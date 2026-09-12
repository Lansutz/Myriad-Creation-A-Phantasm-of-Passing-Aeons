using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CivilizationEvolution.UI.Common
{
    /// <summary>
    /// 标签页控制器 - 嵌入式磨砂石材 + Active时底部青铜强调线
    /// 四状态: Inactive/Hover/Active/Disabled
    /// Tab是信息结构导航层，不使用语义色底色，保持中性灰褐
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class UITabController : Toggle
    {
        [System.Serializable]
        public class TabStateColors
        {
            public Color baseColor = new Color(0.357f, 0.341f, 0.306f, 1f); // #5B574E
            public Color borderColor = new Color(0.502f, 0.459f, 0.373f, 1f);
            public Color borderDark = new Color(0.318f, 0.294f, 0.251f, 1f);
            [Range(0f, 1f)] public float accentLine = 0f;
            [Range(0f, 1f)] public float edgeHighlight = 0.05f;
            [Range(0f, 1f)] public float innerShadow = 0.08f;
            public Color textColor = new Color(0.682f, 0.655f, 0.592f, 1f); // #AEA797
        }

        [Header("四状态配色")]
        [SerializeField] private TabStateColors inactiveState = new TabStateColors
        {
            baseColor = new Color(0.357f, 0.341f, 0.306f, 1f),    // #5B574E
            borderColor = new Color(0.451f, 0.412f, 0.337f, 1f),  // #736956
            borderDark = new Color(0.286f, 0.275f, 0.235f, 1f),   // #49463C
            accentLine = 0f,
            edgeHighlight = 0.03f,
            innerShadow = 0.06f,
            textColor = new Color(0.682f, 0.655f, 0.592f, 1f)     // #AEA797
        };

        [SerializeField] private TabStateColors hoverState = new TabStateColors
        {
            baseColor = new Color(0.408f, 0.380f, 0.333f, 1f),    // #686155
            borderColor = new Color(0.510f, 0.463f, 0.369f, 1f),  // #82765E
            borderDark = new Color(0.318f, 0.294f, 0.251f, 1f),   // #514B40
            accentLine = 0f,
            edgeHighlight = 0.08f,
            innerShadow = 0.07f,
            textColor = new Color(0.784f, 0.749f, 0.667f, 1f)     // #C8BFAA
        };

        [SerializeField] private TabStateColors activeState = new TabStateColors
        {
            baseColor = new Color(0.443f, 0.412f, 0.357f, 1f),    // #71695B
            borderColor = new Color(0.533f, 0.467f, 0.369f, 1f),  // #88775E
            borderDark = new Color(0.345f, 0.318f, 0.271f, 1f),   // #585145
            accentLine = 1f,
            edgeHighlight = 0.06f,
            innerShadow = 0.10f,
            textColor = new Color(0.839f, 0.800f, 0.722f, 1f)     // #D6CCB8
        };

        [SerializeField] private TabStateColors disabledState = new TabStateColors
        {
            baseColor = new Color(0.318f, 0.306f, 0.282f, 1f),    // #514E48
            borderColor = new Color(0.400f, 0.376f, 0.322f, 1f),  // #666052
            borderDark = new Color(0.251f, 0.243f, 0.224f, 1f),   // #403E39
            accentLine = 0f,
            edgeHighlight = 0.02f,
            innerShadow = 0.05f,
            textColor = new Color(0.467f, 0.447f, 0.408f, 1f)     // #777268
        };

        [Header("过渡参数")]
        [SerializeField] private float transitionDuration = 0.12f;
        [SerializeField] private Text tabText;

        // 材质属性ID
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int BorderColorID = Shader.PropertyToID("_BorderColor");
        private static readonly int BorderDarkID = Shader.PropertyToID("_BorderDark");
        private static readonly int AccentLineID = Shader.PropertyToID("_AccentLine");
        private static readonly int EdgeHighlightID = Shader.PropertyToID("_EdgeHighlightStrength");
        private static readonly int EdgeShadowID = Shader.PropertyToID("_EdgeShadowStrength");

        private Material _tabMaterial;
        private RectTransform _rectTransform;
        private Coroutine _transitionCoroutine;
        private TabStateColors _currentState;

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();

            var graphic = GetComponent<Graphic>();
            if (graphic != null && graphic.material != null)
            {
                _tabMaterial = new Material(graphic.material);
                graphic.material = _tabMaterial;
            }

            _currentState = isOn ? activeState : inactiveState;
            ApplyState(_currentState, 1f);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            TabStateColors target;
            if (state == SelectionState.Disabled)
            {
                target = disabledState;
            }
            else if (isOn)
            {
                target = (state == SelectionState.Pressed || state == SelectionState.Highlighted)
                    ? activeState
                    : activeState;
            }
            else
            {
                target = state switch
                {
                    SelectionState.Highlighted => hoverState,
                    SelectionState.Pressed => hoverState,
                    _ => inactiveState
                };
            }

            if (instant)
            {
                ApplyState(target, 1f);
                _currentState = target;
            }
            else
            {
                if (_transitionCoroutine != null)
                    StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = StartCoroutine(SmoothTransition(target));
            }
        }

        /// <summary>
        /// 设置选中状态（外部调用）
        /// </summary>
        public void SetActive(bool active)
        {
            isOn = active;
        }

        private IEnumerator SmoothTransition(TabStateColors target)
        {
            TabStateColors from = new TabStateColors
            {
                baseColor = _currentState.baseColor,
                borderColor = _currentState.borderColor,
                borderDark = _currentState.borderDark,
                accentLine = _currentState.accentLine,
                edgeHighlight = _currentState.edgeHighlight,
                innerShadow = _currentState.innerShadow,
                textColor = _currentState.textColor
            };

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                t = t * t * (3f - 2f * t);
                ApplyState(from, target, t);
                yield return null;
            }

            ApplyState(target, 1f);
            _currentState = target;
        }

        private void ApplyState(TabStateColors state, float t)
        {
            ApplyState(_currentState, state, t);
        }

        private void ApplyState(TabStateColors from, TabStateColors to, float t)
        {
            if (_tabMaterial == null) return;

            Color baseCol = Color.Lerp(from.baseColor, to.baseColor, t);
            Color borderCol = Color.Lerp(from.borderColor, to.borderColor, t);
            Color borderDark = Color.Lerp(from.borderDark, to.borderDark, t);
            float accent = Mathf.Lerp(from.accentLine, to.accentLine, t);
            float edgeHi = Mathf.Lerp(from.edgeHighlight, to.edgeHighlight, t);
            float innerSh = Mathf.Lerp(from.innerShadow, to.innerShadow, t);
            Color textCol = Color.Lerp(from.textColor, to.textColor, t);

            _tabMaterial.SetColor(BaseColorID, baseCol);
            _tabMaterial.SetColor(BorderColorID, borderCol);
            _tabMaterial.SetColor(BorderDarkID, borderDark);
            _tabMaterial.SetFloat(AccentLineID, accent);
            _tabMaterial.SetFloat(EdgeHighlightID, edgeHi);
            _tabMaterial.SetFloat(EdgeShadowID, innerSh);

            if (tabText != null)
            {
                tabText.color = textCol;
            }
        }

        protected override void OnDestroy()
        {
            if (_tabMaterial != null)
            {
                Destroy(_tabMaterial);
                _tabMaterial = null;
            }
            base.OnDestroy();
        }
    }
}
