using System;
using System.Collections.Generic;
using UnityEngine;

namespace Myriad.MapEditor
{
    public enum BrushKind { Raise, Lower, Smooth, Cliff }

    /// <summary>
    /// Authoritative map-editing model. Source elevation is edited first; all other cell data is cached and rebuilt.
    /// The grid uses four edge neighbours. Diagonal cells never share ocean connectivity.
    /// </summary>
    public sealed class MapEditorModel
    {
        private readonly MapCell[] cells;
        private readonly HashSet<int> dirtyCells = new HashSet<int>();
        private readonly HashSet<int> boundaryChanged = new HashSet<int>();
        private readonly Stack<MapOperation> undoStack = new Stack<MapOperation>();
        private readonly Stack<MapOperation> redoStack = new Stack<MapOperation>();
        private int recalculationLockDepth;
        private bool forceGlobalRecalculation;
        private int nextSeaConnectId = 1;

        public int Width { get; }
        public int Height { get; }
        public float SeaLevel { get; private set; }
        public float NearSeaWidth { get; private set; }
        public float MaxOceanDepth { get; private set; }
        public IReadOnlyList<MapCell> Cells => cells;
        public bool IsRecalculationLocked => recalculationLockDepth > 0;

        public MapEditorModel(int width, int height, float seaLevel = 0f, float nearSeaWidth = 3f, float maxOceanDepth = 1f)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (nearSeaWidth < 0f) throw new ArgumentOutOfRangeException(nameof(nearSeaWidth));
            if (maxOceanDepth <= 0f) throw new ArgumentOutOfRangeException(nameof(maxOceanDepth));

            Width = width;
            Height = height;
            SeaLevel = seaLevel;
            NearSeaWidth = nearSeaWidth;
            MaxOceanDepth = maxOceanDepth;
            cells = new MapCell[width * height];
            for (var i = 0; i < cells.Length; i++) cells[i] = new MapCell();
            ForceGlobalRecalculation();
            RecalculateDirty();
        }

        public MapCell GetCell(int x, int y) => cells[ToIndex(x, y)];

        /// <summary>Sets one source height as a reversible editor operation.</summary>
        public void SetElevationAt(int x, int y, float elevation)
        {
            var index = ToIndex(x, y);
            ExecuteEdit("SetElevation", changes => SetElevation(index, elevation, changes));
        }

        public void BeginBatch() => recalculationLockDepth++;

        public void EndBatch()
        {
            if (recalculationLockDepth == 0) throw new InvalidOperationException("No active map edit batch.");
            recalculationLockDepth--;
            if (recalculationLockDepth == 0) RecalculateDirty();
        }

        public void ApplyBrush(BrushKind kind, Vector2 center, float radius, float strength, Vector2 cliffDirection)
        {
            if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
            ExecuteEdit(kind.ToString(), changes =>
            {
                var snapshot = new float[cells.Length];
                for (var i = 0; i < cells.Length; i++) snapshot[i] = cells[i].Elevation;
                for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance > radius) continue;
                    var falloff = 1f - distance / radius;
                    var index = ToIndex(x, y);
                    var value = snapshot[index];
                    if (kind == BrushKind.Raise) value += strength * falloff;
                    else if (kind == BrushKind.Lower) value -= strength * falloff;
                    else if (kind == BrushKind.Smooth) value = Mathf.Lerp(value, NeighbourAverage(snapshot, x, y), Mathf.Clamp01(strength * falloff));
                    else
                    {
                        var normal = cliffDirection.sqrMagnitude < 0.0001f ? Vector2.right : cliffDirection.normalized;
                        var side = Vector2.Dot(new Vector2(x, y) - center, normal);
                        value += Mathf.Sign(side == 0f ? 1f : side) * strength * falloff;
                    }
                    SetElevation(index, value, changes);
                }
            });
        }

        public void GenerateCraterIslands(Vector2 center, float radius, int islandCount, int seed, float rimHeight, float craterDepth, float noiseAmplitude)
        {
            GenerateIslands("CraterIslands", center, radius, islandCount, seed, (distance01, random) =>
            {
                var ring = Mathf.Exp(-Mathf.Pow((distance01 - 0.62f) / 0.18f, 2f)) * rimHeight;
                var bowl = -Mathf.Exp(-Mathf.Pow(distance01 / 0.48f, 2f)) * craterDepth;
                return ring + bowl + ((float)random.NextDouble() * 2f - 1f) * noiseAmplitude;
            });
        }

        public void GenerateVolcanicIslands(Vector2 center, float radius, int volcanoCount, int seed, float coneHeight, float calderaDepth, float roughness)
        {
            GenerateIslands("VolcanicIslands", center, radius, volcanoCount, seed, (distance01, random) =>
            {
                var cone = (1f - distance01) * coneHeight;
                var caldera = -Mathf.Exp(-Mathf.Pow(distance01 / 0.2f, 2f)) * calderaDepth;
                return cone + caldera + ((float)random.NextDouble() * 2f - 1f) * roughness;
            });
        }

        public void SetOceanParameters(float seaLevel, float nearSeaWidth, float maxOceanDepth)
        {
            if (nearSeaWidth < 0f || maxOceanDepth <= 0f) throw new ArgumentOutOfRangeException();
            if (Mathf.Approximately(SeaLevel, seaLevel) && Mathf.Approximately(NearSeaWidth, nearSeaWidth) && Mathf.Approximately(MaxOceanDepth, maxOceanDepth)) return;
            SeaLevel = seaLevel;
            NearSeaWidth = nearSeaWidth;
            MaxOceanDepth = maxOceanDepth;
            ForceGlobalRecalculation();
            if (!IsRecalculationLocked) RecalculateDirty();
        }

        public void Undo()
        {
            if (undoStack.Count == 0) return;
            var operation = undoStack.Pop();
            ApplyOperation(operation, false);
            redoStack.Push(operation);
        }

        public void Redo()
        {
            if (redoStack.Count == 0) return;
            var operation = redoStack.Pop();
            ApplyOperation(operation, true);
            undoStack.Push(operation);
        }

        /// <summary>Repairs all cached fields after external data changes; it is not an undoable edit.</summary>
        public void RecalculateAll()
        {
            ForceGlobalRecalculation();
            if (!IsRecalculationLocked) RecalculateDirty();
        }

        private void GenerateIslands(string tool, Vector2 center, float radius, int count, int seed, Func<float, System.Random, float> profile)
        {
            if (radius <= 0f || count <= 0) throw new ArgumentOutOfRangeException();
            ExecuteEdit(tool, changes =>
            {
                var random = new System.Random(seed);
                for (var island = 0; island < count; island++)
                {
                    var offset = new Vector2(((float)random.NextDouble() * 2f - 1f), ((float)random.NextDouble() * 2f - 1f)) * radius * 0.45f;
                    var islandCenter = center + offset;
                    var islandRadius = radius * (0.35f + (float)random.NextDouble() * 0.35f);
                    for (var y = 0; y < Height; y++)
                    for (var x = 0; x < Width; x++)
                    {
                        var d = Vector2.Distance(new Vector2(x, y), islandCenter);
                        if (d > islandRadius) continue;
                        var index = ToIndex(x, y);
                        SetElevation(index, cells[index].Elevation + profile(d / islandRadius, random), changes);
                    }
                }
            });
        }

        private void ExecuteEdit(string tool, Action<List<HeightChange>> edit)
        {
            var ownedLock = !IsRecalculationLocked;
            if (ownedLock) BeginBatch();
            var changes = new List<HeightChange>();
            var beforeSeaLevel = SeaLevel;
            try
            {
                edit(changes);
                if (changes.Count > 0)
                {
                    undoStack.Push(new MapOperation(tool, changes, beforeSeaLevel, SeaLevel));
                    redoStack.Clear();
                }
            }
            finally
            {
                if (ownedLock) EndBatch();
            }
        }

        private void SetElevation(int index, float value, List<HeightChange> changes)
        {
            var cell = cells[index];
            if (Mathf.Approximately(cell.Elevation, value)) return;
            // A generator may affect one cell several times. Preserve its first value and only advance its final value,
            // so undo restores the pre-operation state rather than an intermediate layer.
            var existingChange = -1;
            for (var i = 0; i < changes.Count; i++)
            {
                if (changes[i].Index != index) continue;
                existingChange = i;
                break;
            }
            if (existingChange >= 0)
            {
                var existing = changes[existingChange];
                existing.After = value;
                changes[existingChange] = existing;
            }
            else changes.Add(new HeightChange(index, cell.Elevation, value));
            var wasLand = cell.Elevation > SeaLevel;
            cell.Elevation = value;
            dirtyCells.Add(index);
            AddNeighboursToDirty(index);
            if (wasLand != value > SeaLevel) boundaryChanged.Add(index);
        }

        private void ApplyOperation(MapOperation operation, bool forward)
        {
            BeginBatch();
            try
            {
                SeaLevel = forward ? operation.AfterSeaLevel : operation.BeforeSeaLevel;
                foreach (var change in operation.HeightChanges)
                {
                    var cell = cells[change.Index];
                    var wasLand = cell.Elevation > SeaLevel;
                    cell.Elevation = forward ? change.After : change.Before;
                    dirtyCells.Add(change.Index);
                    AddNeighboursToDirty(change.Index);
                    if (wasLand != cell.Elevation > SeaLevel) boundaryChanged.Add(change.Index);
                }
            }
            finally { EndBatch(); }
        }

        private void RecalculateDirty()
        {
            if (dirtyCells.Count == 0 && !forceGlobalRecalculation) return;
            // Connectivity and water tiers precede slope, climate, biome and travel cost.
            RecalculateOcean();
            if (!forceGlobalRecalculation && boundaryChanged.Count == 0) UpdateOceanDepths();
            foreach (var index in dirtyCells) RecalculateSlope(index);
            foreach (var index in dirtyCells) RecalculateClimate(index);
            foreach (var index in dirtyCells) RecalculateBiome(index);
            foreach (var index in dirtyCells) RecalculateTravelCost(index);
            dirtyCells.Clear();
            boundaryChanged.Clear();
            forceGlobalRecalculation = false;
        }

        private void RecalculateOcean()
        {
            // A boundary change can merge or split components. Scanning components is deterministic and correct;
            // callers only invoke this method for a boundary change or a required global parameter refresh.
            if (!forceGlobalRecalculation && boundaryChanged.Count == 0) return;
            var visited = new bool[cells.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                cell.IsLand = cell.Elevation > SeaLevel;
                cell.IsCoast = false;
                cell.OceanTier = OceanTier.None;
                cell.OceanDepth01 = cell.IsLand ? 0f : Mathf.Clamp01((SeaLevel - cell.Elevation) / MaxOceanDepth);
                cell.SeaConnectId = null;
            }
            for (var start = 0; start < cells.Length; start++)
            {
                if (visited[start] || cells[start].IsLand) continue;
                var component = FloodSea(start, visited);
                var id = nextSeaConnectId++;
                foreach (var index in component) cells[index].SeaConnectId = id;
                ClassifyOceanTier(component);
            }
            for (var i = 0; i < cells.Length; i++)
                if (cells[i].IsLand)
                    foreach (var neighbour in Neighbours(i))
                        if (!cells[neighbour].IsLand) { cells[i].IsCoast = true; break; }
        }

        // Deepening/shoaling an existing sea cell changes depth but cannot alter connectivity or tier membership.
        private void UpdateOceanDepths()
        {
            foreach (var index in dirtyCells)
            {
                var cell = cells[index];
                if (!cell.IsLand) cell.OceanDepth01 = Mathf.Clamp01((SeaLevel - cell.Elevation) / MaxOceanDepth);
            }
        }

        private List<int> FloodSea(int start, bool[] visited)
        {
            var result = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(start); visited[start] = true;
            while (queue.Count > 0)
            {
                var index = queue.Dequeue(); result.Add(index);
                foreach (var neighbour in Neighbours(index))
                    if (!visited[neighbour] && !cells[neighbour].IsLand) { visited[neighbour] = true; queue.Enqueue(neighbour); }
            }
            return result;
        }

        private void ClassifyOceanTier(List<int> component)
        {
            var distance = new Dictionary<int, int>();
            var queue = new Queue<int>();
            foreach (var index in component)
            {
                foreach (var neighbour in Neighbours(index))
                    if (cells[neighbour].IsLand) { cells[index].OceanTier = OceanTier.Coast; distance[index] = 0; queue.Enqueue(index); break; }
            }
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                foreach (var neighbour in Neighbours(index))
                    if (!cells[neighbour].IsLand && !distance.ContainsKey(neighbour)) { distance[neighbour] = distance[index] + 1; queue.Enqueue(neighbour); }
            }
            foreach (var index in component)
                if (cells[index].OceanTier != OceanTier.Coast)
                    cells[index].OceanTier = distance.ContainsKey(index) && distance[index] <= NearSeaWidth ? OceanTier.NearSea : OceanTier.DeepSea;
        }

        private void RecalculateSlope(int index)
        {
            var maxDelta = 0f;
            foreach (var neighbour in Neighbours(index)) maxDelta = Mathf.Max(maxDelta, Mathf.Abs(cells[index].Elevation - cells[neighbour].Elevation));
            cells[index].Slope01 = Mathf.Clamp01(maxDelta);
        }

        private void RecalculateClimate(int index)
        {
            var cell = cells[index];
            cell.Temperature01 = Mathf.Clamp01(0.7f - cell.Elevation * 0.1f);
            cell.Moisture01 = cell.IsLand ? (cell.IsCoast ? 0.75f : 0.45f) : 1f;
        }

        private void RecalculateBiome(int index)
        {
            var cell = cells[index];
            cell.Biome = !cell.IsLand ? "Ocean" : cell.Slope01 > 0.7f ? "Cliff" : cell.Moisture01 > 0.6f ? "Temperate" : "Dry";
        }

        private void RecalculateTravelCost(int index)
        {
            var cell = cells[index];
            cell.TravelCost = !cell.IsLand ? (cell.OceanTier == OceanTier.Coast ? 2f : 3f) : 1f + cell.Slope01 * 4f;
        }

        private float NeighbourAverage(float[] snapshot, int x, int y)
        {
            var total = snapshot[ToIndex(x, y)]; var count = 1;
            foreach (var index in Neighbours(ToIndex(x, y))) { total += snapshot[index]; count++; }
            return total / count;
        }

        private void ForceGlobalRecalculation()
        {
            forceGlobalRecalculation = true;
            for (var i = 0; i < cells.Length; i++) dirtyCells.Add(i);
        }

        private void AddNeighboursToDirty(int index) { foreach (var neighbour in Neighbours(index)) dirtyCells.Add(neighbour); }
        private int ToIndex(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new ArgumentOutOfRangeException();
            return y * Width + x;
        }
        private IEnumerable<int> Neighbours(int index)
        {
            var x = index % Width; var y = index / Width;
            if (x > 0) yield return index - 1;
            if (x + 1 < Width) yield return index + 1;
            if (y > 0) yield return index - Width;
            if (y + 1 < Height) yield return index + Width;
        }
    }
}
