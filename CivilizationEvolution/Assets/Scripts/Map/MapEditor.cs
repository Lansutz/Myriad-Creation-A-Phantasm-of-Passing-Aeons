using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Render;

namespace CivilizationEvolution.Map
{
    /// <summary>
    /// 地图编辑器（重做版）
    /// 支持自由画笔绘制、增删地块、地形编辑、左右连通
    /// 不再必须沿格子作画，画笔可为圆形/方形/自定义半径
    /// </summary>
    public class MapEditor : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private GameWorld world;
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private Camera mainCamera;

        [Header("画笔设置")]
        [SerializeField] private BrushMode brushMode = BrushMode.RaiseTerrain;
        [SerializeField] private BrushShape brushShape = BrushShape.Circle;
        [Range(0, 20)] [SerializeField] private int brushRadius = 3;
        [Range(0.01f, 0.5f)] [SerializeField] private float brushStrength = 0.1f;
        [SerializeField] private bool continuousPaint = true; // 拖拽时连续绘制

        [Header("编辑器状态")]
        [SerializeField] private bool isEditorActive = false;
        private int hoveredTile = -1;
        private readonly List<int> currentBrushTiles = new List<int>();

        /// <summary>编辑器是否激活（只读）</summary>
        public bool IsEditorActive => isEditorActive;
        /// <summary>当前画笔信息</summary>
        public BrushMode CurrentBrushMode => brushMode;

        // 拖拽状态
        private bool _isPainting = false;
        private Vector3 _lastPaintPos;

        void Update()
        {
            if (!isEditorActive || world == null) return;

            UpdateHoveredTile();
            UpdateBrushPreview();
            HandlePaintInput();
            HandleKeyboardShortcuts();
        }

        /// <summary>更新鼠标悬停地块</summary>
        private void UpdateHoveredTile()
        {
            if (mapRenderer == null) return;
            hoveredTile = mapRenderer.ScreenToTile(Input.mousePosition);
        }

        /// <summary>更新画笔预览（当前画笔影响的地块）</summary>
        private void UpdateBrushPreview()
        {
            currentBrushTiles.Clear();
            if (hoveredTile < 0) return;

            if (brushShape == BrushShape.Circle)
            {
                currentBrushTiles.Clear();
                currentBrushTiles.AddRange(TileGrid.GetCircleBrush(
                    hoveredTile, brushRadius, world.mapWidth, world.mapHeight, world.config.wrapX));
            }
            else
            {
                currentBrushTiles.Clear();
                currentBrushTiles.AddRange(TileGrid.GetSquareBrush(
                    hoveredTile, brushRadius, world.mapWidth, world.mapHeight, world.config.wrapX));
            }
        }

        /// <summary>处理绘制输入</summary>
        private void HandlePaintInput()
        {
            // 左键按下开始绘制
            if (Input.GetMouseButtonDown(0))
            {
                _isPainting = true;
                _lastPaintPos = Input.mousePosition;
                ApplyBrush();
            }

            // 左键松开停止绘制
            if (Input.GetMouseButtonUp(0))
            {
                _isPainting = false;
            }

            // 拖拽连续绘制
            if (_isPainting && continuousPaint && Input.GetMouseButton(0))
            {
                float dist = Vector3.Distance(Input.mousePosition, _lastPaintPos);
                if (dist > 5f) // 移动超过5像素才绘制一次，避免重复
                {
                    ApplyBrush();
                    _lastPaintPos = Input.mousePosition;
                }
            }
        }

        /// <summary>处理键盘快捷键</summary>
        private void HandleKeyboardShortcuts()
        {
            // 括号键调整画笔大小
            if (Input.GetKeyDown(KeyCode.LeftBracket))
                brushRadius = Mathf.Max(0, brushRadius - 1);
            if (Input.GetKeyDown(KeyCode.RightBracket))
                brushRadius = Mathf.Min(20, brushRadius + 1);

            // 数字键切换画笔模式
            if (Input.GetKeyDown(KeyCode.Alpha1)) brushMode = BrushMode.RaiseTerrain;
            if (Input.GetKeyDown(KeyCode.Alpha2)) brushMode = BrushMode.LowerTerrain;
            if (Input.GetKeyDown(KeyCode.Alpha3)) brushMode = BrushMode.SetLand;
            if (Input.GetKeyDown(KeyCode.Alpha4)) brushMode = BrushMode.SetOcean;
            if (Input.GetKeyDown(KeyCode.Alpha5)) brushMode = BrushMode.AddTile;
            if (Input.GetKeyDown(KeyCode.Alpha6)) brushMode = BrushMode.RemoveTile;

            // B键切换画笔形状
            if (Input.GetKeyDown(KeyCode.B))
                brushShape = (brushShape == BrushShape.Circle) ? BrushShape.Square : BrushShape.Circle;

            // E键切换编辑器激活
            if (Input.GetKeyDown(KeyCode.E))
                ToggleEditor();
        }

        /// <summary>应用画笔到当前悬停位置</summary>
        public void ApplyBrush()
        {
            if (hoveredTile < 0 || currentBrushTiles.Count == 0) return;

            foreach (int tileIndex in currentBrushTiles)
            {
                if (tileIndex < 0 || tileIndex >= world.tiles.Length) continue;

                switch (brushMode)
                {
                    case BrushMode.RaiseTerrain:
                        RaiseTerrain(tileIndex);
                        break;
                    case BrushMode.LowerTerrain:
                        LowerTerrain(tileIndex);
                        break;
                    case BrushMode.SetLand:
                        SetLand(tileIndex);
                        break;
                    case BrushMode.SetOcean:
                        SetOcean(tileIndex);
                        break;
                    case BrushMode.AddTile:
                        world.CreateTile(tileIndex);
                        break;
                    case BrushMode.RemoveTile:
                        world.RemoveTile(tileIndex);
                        break;
                    case BrushMode.SmoothTerrain:
                        SmoothTerrain(tileIndex);
                        break;
                }
            }

            // 标记脏区，触发增量重算
            world.RecalculateDirty();
        }

        /// <summary>抬高地形</summary>
        private void RaiseTerrain(int tileIndex)
        {
            if (!world.tiles[tileIndex].exists) return;
            ref TileData tile = ref world.tiles[tileIndex];
            tile.elevation01 = Mathf.Clamp(tile.elevation01 + brushStrength, -1f, 1f);
            tile.slopeDegree = Mathf.Abs(tile.elevation01) * 30f;
            world.PaintTerrain(tileIndex, tile.elevation01);
        }

        /// <summary>降低地形</summary>
        private void LowerTerrain(int tileIndex)
        {
            if (!world.tiles[tileIndex].exists) return;
            ref TileData tile = ref world.tiles[tileIndex];
            tile.elevation01 = Mathf.Clamp(tile.elevation01 - brushStrength, -1f, 1f);
            tile.slopeDegree = Mathf.Abs(tile.elevation01) * 30f;
            world.PaintTerrain(tileIndex, tile.elevation01);
        }

        /// <summary>设置为陆地（直接修改isLand，跳过海平面判定）</summary>
        private void SetLand(int tileIndex)
        {
            if (!world.tiles[tileIndex].exists) return;
            ref TileData tile = ref world.tiles[tileIndex];
            tile.isLand = true;
            tile.oceanTier = GameEnums.OceanTier.None;
            tile.seaConnectId = -1;
            if (tile.elevation01 < world.config.seaLevel)
                tile.elevation01 = world.config.seaLevel + 0.05f;
            world.PaintTerrain(tileIndex, tile.elevation01);
        }

        /// <summary>设置为海洋</summary>
        private void SetOcean(int tileIndex)
        {
            if (!world.tiles[tileIndex].exists) return;
            ref TileData tile = ref world.tiles[tileIndex];
            tile.isLand = false;
            tile.isCoast = false;
            if (tile.elevation01 >= world.config.seaLevel)
                tile.elevation01 = world.config.seaLevel - 0.05f;
            world.PaintTerrain(tileIndex, tile.elevation01);
        }

        /// <summary>平滑地形（与邻接取平均）</summary>
        private void SmoothTerrain(int tileIndex)
        {
            if (!world.tiles[tileIndex].exists) return;

            var neighbours = world.GetNeighbours(tileIndex);
            float sum = world.tiles[tileIndex].elevation01;
            int count = 1;

            foreach (int n in neighbours)
            {
                if (n >= 0 && n < world.tiles.Length && world.tiles[n].exists)
                {
                    sum += world.tiles[n].elevation01;
                    count++;
                }
            }

            float avg = sum / count;
            ref TileData tile = ref world.tiles[tileIndex];
            tile.elevation01 = Mathf.Lerp(tile.elevation01, avg, brushStrength * 2f);
            tile.slopeDegree = Mathf.Abs(tile.elevation01) * 30f;
            world.PaintTerrain(tileIndex, tile.elevation01);
        }

        // ===== 公共接口 =====

        /// <summary>切换编辑器激活状态</summary>
        public void ToggleEditor()
        {
            isEditorActive = !isEditorActive;
            Debug.Log($"[MapEditor] 编辑器{(isEditorActive ? "激活" : "关闭")}");
        }

        /// <summary>设置画笔模式</summary>
        public void SetBrushMode(BrushMode mode)
        {
            brushMode = mode;
            Debug.Log($"[MapEditor] 画笔模式: {mode}");
        }

        /// <summary>设置画笔形状</summary>
        public void SetBrushShape(BrushShape shape)
        {
            brushShape = shape;
        }

        /// <summary>设置画笔半径</summary>
        public void SetBrushRadius(int radius)
        {
            brushRadius = Mathf.Clamp(radius, 0, 20);
        }

        /// <summary>切换左右连通</summary>
        public void ToggleWrapX()
        {
            world.config.wrapX = !world.config.wrapX;
            world.UpdateConfig(c => c.wrapX = world.config.wrapX);
            Debug.Log($"[MapEditor] 左右连通: {(world.config.wrapX ? "开启" : "关闭")}");
        }

        /// <summary>填充整个地图为存在地块</summary>
        public void FillAllTiles()
        {
            for (int i = 0; i < world.tiles.Length; i++)
            {
                world.CreateTile(i);
            }
            world.RecalculateDirty();
            Debug.Log("[MapEditor] 已填充所有地块");
        }

        /// <summary>清除所有地块（全部设为虚空）</summary>
        public void ClearAllTiles()
        {
            for (int i = 0; i < world.tiles.Length; i++)
            {
                world.RemoveTile(i);
            }
            world.RecalculateDirty();
            Debug.Log("[MapEditor] 已清除所有地块");
        }

        /// <summary>获取当前画笔信息（用于UI显示）</summary>
        public string GetBrushInfo()
        {
            return $"模式: {brushMode} | 形状: {brushShape} | 半径: {brushRadius} | 强度: {brushStrength:F2}";
        }
    }

    /// <summary>画笔模式</summary>
    public enum BrushMode
    {
        RaiseTerrain,    // 抬高地形
        LowerTerrain,    // 降低地形
        SmoothTerrain,   // 平滑地形
        SetLand,         // 设置陆地
        SetOcean,        // 设置海洋
        AddTile,         // 添加地块
        RemoveTile       // 删除地块
    }

    /// <summary>画笔形状</summary>
    public enum BrushShape
    {
        Circle,  // 圆形
        Square   // 方形
    }
}
