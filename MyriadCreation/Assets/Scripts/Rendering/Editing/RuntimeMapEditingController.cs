using UnityEngine;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Rendering.Editing
{
    /// <summary>
    /// 运行时地图编辑控制器：保留选择/射线/画笔/输入，所有编辑执行委托给
    /// <see cref="IMapEditCommandService"/>（App 的 WorldEditService），不再直接写 GameWorld。
    /// 不直接读输入；UI/输入层调用这些方法。核心命令由 App/World.Editing 执行。
    /// B8：服务由 GameManager/Bootstrap 通过 <see cref="SetEditService"/> 显式注入（无 AppServices 静态回退）。
    /// </summary>
    public sealed partial class RuntimeMapEditingController : MonoBehaviour
    {
        private IMapWorldView _view;
        [SerializeField] private MapRenderer mapRenderer;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private int brushRadius = 3;
        [SerializeField] private bool editingActive;
        private int _dragStartTile = -1;
        private bool _dragging;

        private MapSelection _selection;
        private IMapEditCommandService _editService;
        private bool _missingServiceLogged;

        public MapSelection Selection
        {
            get
            {
                EnsureSelection();
                return _selection;
            }
        }

        public int BrushRadius
        {
            get => brushRadius;
            set => brushRadius = Mathf.Clamp(value, 0, 128);
        }

        public bool EditingActive => editingActive;

        /// <summary>当前注入的编辑命令服务（未注入为 null）；App 的 WorldEditService 会跟随 GameManager 的当前世界。</summary>
        public IMapEditCommandService EditService => _editService;

        private void Awake()
        {
            if (mapRenderer == null) mapRenderer = FindAnyObjectByType<MapRenderer>();
            if (mainCamera == null) mainCamera = Camera.main;
            _view = (mapRenderer != null) ? mapRenderer.WorldView : null;
        }

        /// <summary>B7：注入只读地图视图（宽高/查询；编辑命令仍走 IMapEditCommandService）。</summary>
        public void SetWorldView(IMapWorldView view)
        {
            _view = view;
            _selection = null;
            _dragStartTile = -1;
            _dragging = false;
        }

        /// <summary>B8：显式注入地图编辑命令服务（由 GameManager/Bootstrap 注入 App 的 WorldEditService）。</summary>
        public void SetEditService(IMapEditCommandService service)
        {
            _editService = service;
            _missingServiceLogged = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2)) ToggleEditing();
            if (!editingActive || _view == null || mapRenderer == null) return;

            if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                Undo();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Y) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                Redo();
                return;
            }
            if (Input.GetKeyDown(KeyCode.LeftBracket)) BrushRadius--;
            if (Input.GetKeyDown(KeyCode.RightBracket)) BrushRadius++;

            // 方向键 + Shift：整块选区平移
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveSelection(-1, 0);
                else if (Input.GetKeyDown(KeyCode.RightArrow)) MoveSelection(1, 0);
                else if (Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(0, 1);
                else if (Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(0, -1);
            }
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) RaiseSelection(0.05f);
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) RaiseSelection(-0.05f);

            // 鼠标框选
            if (Input.GetMouseButtonDown(0))
            {
                _dragStartTile = mapRenderer.ScreenToTile(Input.mousePosition);
                _dragging = _dragStartTile >= 0;
            }
            if (Input.GetMouseButtonUp(0) && _dragging)
            {
                int end = mapRenderer.ScreenToTile(Input.mousePosition);
                _dragging = false;
                if (_dragStartTile >= 0 && end >= 0)
                {
                    int x0 = _dragStartTile % _view.MapWidth, y0 = _dragStartTile / _view.MapWidth;
                    int x1 = end % _view.MapWidth, y1 = end / _view.MapWidth;
                    SelectRectangle(x0, y0, x1, y1);
                }
            }
        }

        public void ToggleEditing()
        {
            editingActive = !editingActive;
            Debug.Log($"[RuntimeMapEditing] 编辑模式{(editingActive ? "开启" : "关闭")}");
        }

        public void SelectRectangle(int x0, int y0, int x1, int y1)
        {
            EnsureSelection();
            _selection.Clear();
            _selection.SelectRect(x0, y0, x1, y1);
        }

        public void ClearSelection()
        {
            EnsureSelection();
            _selection.Clear();
        }

        public bool MoveSelection(int dx, int dy)
        {
            EnsureSelection();
            if (_selection.Count == 0) return false;
            var service = ResolveService(true);
            return service != null && service.MoveSelection(_selection, dx, dy);
        }

        public bool RaiseSelection(float delta)
        {
            EnsureSelection();
            if (_selection.Count == 0) return false;
            var service = ResolveService(true);
            return service != null && service.RaiseSelection(_selection, delta);
        }

        public bool Undo()
        {
            var service = ResolveService(true);
            return service != null && service.Undo();
        }

        public bool Redo()
        {
            var service = ResolveService(true);
            return service != null && service.Redo();
        }

        // ---- 单地块/画笔工具转发（Rendering 不写模型，只调用 App 服务） ----
        public bool ElevateTile(int tileIndex, float delta)
        {
            var service = ResolveService(true);
            return service != null && service.ElevateTile(tileIndex, delta);
        }

        public bool LowerTile(int tileIndex, float amount)
        {
            var service = ResolveService(true);
            return service != null && service.LowerTile(tileIndex, amount);
        }

        public bool SmoothTile(int tileIndex)
        {
            var service = ResolveService(true);
            return service != null && service.SmoothTile(tileIndex, brushRadius);
        }

        public bool SetTerrain(int tileIndex, float elevation01)
        {
            var service = ResolveService(true);
            return service != null && service.SetTerrain(tileIndex, elevation01);
        }

        public bool SetBiome(int tileIndex, MyriadCreation.Core.Enums.GameEnums.BiomeType biome)
        {
            var service = ResolveService(true);
            return service != null && service.SetBiome(tileIndex, biome);
        }

        public bool AddTile(int tileIndex)
        {
            var service = ResolveService(true);
            return service != null && service.AddTile(tileIndex);
        }

        public bool RemoveTile(int tileIndex)
        {
            var service = ResolveService(true);
            return service != null && service.RemoveTile(tileIndex);
        }

        public bool Recalculate()
        {
            var service = ResolveService(true);
            if (service == null) return false;
            service.Recalculate();
            return true;
        }

        private void EnsureSelection()
        {
            if (_view == null && mapRenderer != null) _view = mapRenderer.WorldView;
            int width = _view != null ? _view.MapWidth : 1;
            int height = _view != null ? _view.MapHeight : 1;
            if (_selection == null || _selection.width != width || _selection.height != height)
                _selection = new MapSelection(width, height);
        }

        /// <summary>只使用显式注入的缓存服务；未注入时返回 null（warning 一次），不做静态回退。</summary>
        private IMapEditCommandService ResolveService(bool warnIfMissing)
        {
            var service = _editService;
            if (service != null)
            {
                return service;
            }

            if (warnIfMissing && !_missingServiceLogged)
            {
                _missingServiceLogged = true;
                Debug.LogWarning("[RuntimeMapEditing] 未注入 IMapEditCommandService，地图编辑命令将被忽略（检查 GameManager/Bootstrap 是否 SetEditService(WorldEditService)）");
            }
            return null;
        }
    }
}
