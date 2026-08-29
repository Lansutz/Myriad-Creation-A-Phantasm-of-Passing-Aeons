using UnityEngine;

namespace CivilizationEvolution.UI
{
    /// <summary>
    /// UI 主题：统一色板 + 程序化圆角 Sprite + 按钮 ColorTint
    /// 供场景构建（编辑器工具）与运行时 UI（Toast 等）共用，保证视觉一致。
    /// 纯代码生成，无外部图片资源。
    /// </summary>
    public static class UITheme
    {
        // ===== 色板 =====
        public static readonly Color PanelBg = new Color(0.07f, 0.09f, 0.15f, 0.88f);      // 面板底色（半透明）
        public static readonly Color PanelSolid = new Color(0.09f, 0.12f, 0.19f, 0.97f);   // 视口/实体底
        public static readonly Color ButtonNormal = new Color(0.20f, 0.30f, 0.50f, 1f);
        public static readonly Color ButtonHover = new Color(0.30f, 0.43f, 0.68f, 1f);
        public static readonly Color ButtonPressed = new Color(0.13f, 0.20f, 0.36f, 1f);
        public static readonly Color ButtonDisabled = new Color(0.24f, 0.26f, 0.30f, 0.6f);
        public static readonly Color Accent = new Color(0.45f, 0.65f, 0.95f, 1f);          // 强调色（标题/选中）
        public static readonly Color TextMain = new Color(0.92f, 0.95f, 1f, 1f);
        public static readonly Color TextDim = new Color(0.65f, 0.70f, 0.80f, 1f);
        public static readonly Color ToastBg = new Color(0.08f, 0.10f, 0.16f, 0.92f);

        // ===== 事件日志分类色 =====
        public static readonly Color LogSystem = new Color(0.72f, 0.78f, 0.88f, 1f);   // 系统
        public static readonly Color LogInfo = new Color(0.92f, 0.95f, 1f, 1f);        // 常规
        public static readonly Color LogWar = new Color(0.95f, 0.45f, 0.40f, 1f);      // 战争红
        public static readonly Color LogEconomy = new Color(0.45f, 0.85f, 0.55f, 1f);  // 经济绿
        public static readonly Color LogWarning = new Color(0.98f, 0.80f, 0.35f, 1f);  // 警示黄

        // ===== 程序化圆角 Sprite =====
        private const int RoundedSize = 24;
        private const int RoundedRadius = 8;
        private const int RoundedBorder = 8;

        private static Sprite _panelSprite;
        private static Sprite _buttonSprite;

        /// <summary>面板/容器用圆角九宫格 Sprite（白底，配合 Image.color 着色）</summary>
        public static Sprite RoundedPanelSprite
        {
            get { if (_panelSprite == null) _panelSprite = GenerateRoundedSprite(); return _panelSprite; }
        }

        /// <summary>按钮/Dropdown 用圆角九宫格 Sprite</summary>
        public static Sprite RoundedButtonSprite
        {
            get { if (_buttonSprite == null) _buttonSprite = GenerateRoundedSprite(); return _buttonSprite; }
        }

        /// <summary>生成白底圆角九宫格 Sprite（带 1px 抗锯齿）</summary>
        private static Sprite GenerateRoundedSprite()
        {
            var tex = new Texture2D(RoundedSize, RoundedSize, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var px = new Color[RoundedSize * RoundedSize];
            float r = RoundedRadius;
            for (int y = 0; y < RoundedSize; y++)
            {
                for (int x = 0; x < RoundedSize; x++)
                {
                    float lx = x + 0.5f;
                    float ly = y + 0.5f;
                    // 到内部方形区域的距离（四角之外的部分参与圆弧判定）
                    float dx = lx < r ? r - lx : (lx > RoundedSize - r ? lx - (RoundedSize - r) : 0f);
                    float dy = ly < r ? r - ly : (ly > RoundedSize - r ? ly - (RoundedSize - r) : 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = dist <= r ? 1f : Mathf.Clamp01(r - dist + 0.5f);
                    px[y * RoundedSize + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            var texRect = new Rect(0, 0, RoundedSize, RoundedSize);
            var border = new Vector4(RoundedBorder, RoundedBorder, RoundedBorder, RoundedBorder);
            return Sprite.Create(tex, texRect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>为 Selectable 应用统一的 ColorTint 过渡（悬停/按下/选中/禁用）</summary>
        public static void ApplyButtonTint(UnityEngine.UI.Selectable selectable)
        {
            selectable.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var c = selectable.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(0.85f, 0.92f, 1f, 1f);
            c.pressedColor = new Color(0.55f, 0.70f, 0.95f, 1f);
            c.selectedColor = new Color(0.75f, 0.85f, 1f, 1f);
            c.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            c.colorMultiplier = 1f;
            selectable.colors = c;
        }
    }
}