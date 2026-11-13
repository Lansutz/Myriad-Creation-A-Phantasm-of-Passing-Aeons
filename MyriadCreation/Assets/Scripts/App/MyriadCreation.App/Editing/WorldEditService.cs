using System;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Rendering.Editing;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Editing;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.App.Editing
{
    /// <summary>
    /// <see cref="IMapEditCommandService"/>（Rendering.Editing 契约）的 App 层实现：
    /// 所有写操作在 App 程序集内完成，Rendering 只负责输入与调用本服务。
    /// 世界来源：优先构造时注入的 provider（GameManager 的 world 字段，读档自动切换），
    /// 测试/无 provider 时退回 <see cref="BindWorld"/> 显式绑定。
    /// 注：B8 后接口在 Rendering 程序集（不引 Simulation），接口不再包含 BindWorld(GameWorld)；
    /// BindWorld 作为 App 内部可选绑定保留。
    /// </summary>
    public sealed class WorldEditService : IMapEditCommandService
    {
        private readonly Func<GameWorld> _worldProvider;
        private GameWorld _boundWorld;
        private MapEditHistory _history;
        private GameWorld _historyWorld;

        public WorldEditService(Func<GameWorld> worldProvider = null)
        {
            _worldProvider = worldProvider;
        }

        public void BindWorld(GameWorld world)
        {
            _boundWorld = world;
            _history = null;
            _historyWorld = null;
        }

        public bool ElevateTile(int tileIndex, float delta)
        {
            var world = ResolveWorld();
            if (!TryGetTile(world, tileIndex, out var tile) || !tile.exists) return false;
            SetElevation(world, tileIndex, tile.elevation01 + delta);
            Commit(world);
            return true;
        }

        public bool LowerTile(int tileIndex, float amount)
        {
            return ElevateTile(tileIndex, -Math.Abs(amount));
        }

        public bool SmoothTile(int tileIndex, int brushRadius)
        {
            var world = ResolveWorld();
            if (world == null || world.mapWidth <= 0 || world.mapHeight <= 0) return false;
            if (!TryGetTile(world, tileIndex, out var center) || !center.exists) return false;

            int radius = Math.Max(0, Math.Min(brushRadius, 128));
            int cx = tileIndex % world.mapWidth;
            int cy = tileIndex / world.mapWidth;
            float sum = 0f;
            int count = 0;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || x >= world.mapWidth || y < 0 || y >= world.mapHeight) continue;
                    int index = y * world.mapWidth + x;
                    if (index < 0 || index >= world.tiles.Length || !world.tiles[index].exists) continue;
                    sum += world.tiles[index].elevation01;
                    count++;
                }
            }
            if (count == 0) return false;

            // 用平均值平滑（不改动生物群系/资源等其他字段）。
            float avg = sum / count;
            world.PaintTerrain(tileIndex, Clamp(avg, -1f, 1f));
            world.tiles[tileIndex].isLand = avg > 0f;
            Commit(world);
            return true;
        }

        public bool SetTerrain(int tileIndex, float elevation01)
        {
            var world = ResolveWorld();
            if (!TryGetTile(world, tileIndex, out var tile) || !tile.exists) return false;
            SetElevation(world, tileIndex, elevation01);
            Commit(world);
            return true;
        }

        public bool SetBiome(int tileIndex, GameEnums.BiomeType biome)
        {
            var world = ResolveWorld();
            if (!TryGetTile(world, tileIndex, out var tile) || !tile.exists) return false;
            world.tiles[tileIndex].biome = biome;
            Commit(world);
            return true;
        }

        public bool AddTile(int tileIndex)
        {
            var world = ResolveWorld();
            if (world == null || world.tiles == null || !world.CreateTile(tileIndex)) return false;
            Commit(world);
            return true;
        }

        public bool RemoveTile(int tileIndex)
        {
            var world = ResolveWorld();
            if (world == null || world.tiles == null || !world.RemoveTile(tileIndex)) return false;
            Commit(world);
            return true;
        }

        public bool MoveSelection(MapSelection selection, int dx, int dy)
        {
            var world = ResolveWorld();
            if (world == null || selection == null || selection.Count == 0) return false;
            if (selection.width * selection.height > world.tiles.Length) return false;
            GetHistory(world).Execute(world.tiles, new MoveSelectionCommand(selection, dx, dy));
            Commit(world);
            return true;
        }

        public bool RaiseSelection(MapSelection selection, float delta)
        {
            var world = ResolveWorld();
            if (world == null || selection == null || selection.Count == 0) return false;
            if (selection.width * selection.height > world.tiles.Length) return false;
            GetHistory(world).Execute(world.tiles, new RaiseSelectionCommand(selection, delta));
            Commit(world);
            return true;
        }

        public bool Undo()
        {
            var world = ResolveWorld();
            if (world == null || _history == null || !ReferenceEquals(_historyWorld, world)) return false;
            bool ok = _history.Undo(world.tiles);
            if (ok) Commit(world);
            return ok;
        }

        public bool Redo()
        {
            var world = ResolveWorld();
            if (world == null || _history == null || !ReferenceEquals(_historyWorld, world)) return false;
            bool ok = _history.Redo(world.tiles);
            if (ok) Commit(world);
            return ok;
        }

        public void Recalculate()
        {
            var world = ResolveWorld();
            if (world == null) return;
            Commit(world);
        }

        private GameWorld ResolveWorld()
        {
            var fromProvider = _worldProvider?.Invoke();
            return fromProvider != null ? fromProvider : _boundWorld;
        }

        private MapEditHistory GetHistory(GameWorld world)
        {
            if (_history == null || !ReferenceEquals(_historyWorld, world))
            {
                _history = new MapEditHistory();
                _historyWorld = world;
            }
            return _history;
        }

        private static bool TryGetTile(GameWorld world, int tileIndex, out TileData tile)
        {
            tile = default;
            if (world == null || world.tiles == null || tileIndex < 0 || tileIndex >= world.tiles.Length)
                return false;
            tile = world.tiles[tileIndex];
            return true;
        }

        private static void SetElevation(GameWorld world, int tileIndex, float elevation01)
        {
            float clamped = Clamp(elevation01, -1f, 1f);
            world.PaintTerrain(tileIndex, clamped);
            world.tiles[tileIndex].isLand = clamped > 0f;
        }

        /// <summary>等价旧 RuntimeMapEditingController.MarkDirty：所有地块 terrain dirty + 世界重算。</summary>
        private static void Commit(GameWorld world)
        {
            if (world == null || world.tiles == null) return;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                world.tiles[i].isTerrainDirty = true;
            }
            world.RecalculateDirty();
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
