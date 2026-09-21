using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CivilizationEvolution.UI.Common
{
    /// <summary>
    /// 按钮状态控制器 - 暖灰褐磨砂石材按钮
    /// 四状态(Normal/Hover/Pressed/Disabled)平滑过渡 + 按压缩放效果
    /// 材质参数由本脚本驱动，不使用Unity默认ColorTint
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIButtonController : Selectable
    {
        [System.Serializable]
        public class ButtonStateColors
        {
            public Color baseColor = new Color(0.416f, 0.392f, 0.345f, 1f); // #6A6458
            public Color borderColor = new Color(0.502f, 0.459f, 0.373f, 1f); // #80755F
            public Color borderDark = new Color(0.318f, 0.294f, 0.251f, 1f); // #514B40
            [Range(0f, 1f)] public float pressDepth = 0f;
            [Range(0f, 1f)] public float scale = 1f;
        }

        [Header("四状态配色")]
        [SerializeField] private ButtonStateColors normalState = new ButtonStateColors
        {
            baseColor = new Color(0.416f, 0.392f, 0.345f, 1f),    // #6A6458
            borderColor = new Color(0.502f, 0.459f, 0.373f, 1f),  // #80755F
            borderDark = new Color(0.318f, 0.294f, 0.251f, 1f),   // #514B40
            pressDepth = 0f,
            scale = 1f
        };

        [SerializeField] private ButtonStateColors hoverState = new ButtonStateColors
        {
            baseColor = new Color(0.459f, 0.427f, 0.373f, 1f),    // #756D5F
            borderColor = new Color(0.557f, 0.502f, 0.396f, 1f),  // #8E8065
            borderDark = new Color(0.345f, 0.318f, 0.271f, 1f),   // #585145
            pressDepth = 0f,
            scale = 1.015f
        };

        [SerializeField] private ButtonStateColors pressedState = new ButtonStateColors
        {
            baseColor = new Color(0.345f, 0.325f, 0.290f, 1f),    // #58534A
            borderColor = new Color(0.424f, 0.388f, 0.322f, 1f),  // #6C6352
            borderDark = new Color(0.271f, 0.251f, 0.216f, 1f),   // #454037
            pressDepth = 1f,
            scale = 0.985f
        };

        [SerializeField] private ButtonStateColors disabledState = new ButtonStateColors
        {
            baseColor = new Color(0.318f, 0.306f, 0.278f, 1f),    // #514E47
            borderColor = new Color(0.384f, 0.365f, 0.322f, 1f),  // #625D52
            borderDark = new Color(0.243f, 0.235f, 0.208f, 1f),   // #3E3C35
            pressDepth = 0f,
            scale = 1f
        };

        [Header("过渡参数")]
        [SerializeField] private float transitionDuration = 0.08f; // 80ms
        [SerializeField] private bool useScaleAnimation = true;

        // 材质属性ID（缓存）
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int BorderColorID = Shader.PropertyToID("_BorderColor");
        private static readonly int BorderDarkID = Shader.PropertyToID("_BorderDark");
        private static readonly int PressDepthID = Shader.PropertyToID("_PressDepth");

        private Material _buttonMaterial;
        private RectTransform _rectTransform;
        private Coroutine _transitionCoroutine;
        private ButtonStateColors _currentState;
        private Vector3 _baseScale = Vector3.one;

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
            _baseScale = _rectTransform.localScale;

            // 创建材质实例，避免修改原始材质
            var graphic = GetComponent<Graphic>();
            if (graphic != null && graphic.material != null)
            {
                _buttonMaterial = new Material(graphic.material);
                graphic.material = _buttonMaterial;
            }

            _currentState = normalState;
            ApplyState(_currentState, 1f);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            ButtonStateColors target = state switch
            {
                SelectionState.Normal => normalState,
                SelectionState.Highlighted => hoverState,
                SelectionState.Pressed => pressedState,
                SelectionState.Selected => hoverState,
                SelectionState.Disabled => disabledState,
                _ => normalState
            };

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

        private IEnumerator SmoothTransition(ButtonStateColors target)
        {
            ButtonStateColors from = new ButtonStateColors
            {
                baseColor = _currentState.baseColor,
                borderColor = _currentState.borderColor,
                borderDark = _currentState.borderDark,
                pressDepth = _currentState.pressDepth,
                scale = _currentState.scale
            };

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                // 平滑缓动
                t = t * t * (3f - 2f * t);
                ApplyState(from, target, t);
                yield return null;
            }

            ApplyState(target, 1f);
            _currentState = target;
        }

        private void ApplyState(ButtonStateColors state, float t)
        {
            ApplyState(_currentState, state, t);
        }

        private void ApplyState(ButtonStateColors from, ButtonStateColors to, float t)
        {
            if (_buttonMaterial == null) return;

            Color baseCol = Color.Lerp(from.baseColor, to.baseColor, t);
            Color borderCol = Color.Lerp(from.borderColor, to.borderColor, t);
            Color borderDark = Color.Lerp(from.borderDark, to.borderDark, t);
            float pressDepth = Mathf.Lerp(from.pressDepth, to.pressDepth, t);
            float scale = Mathf.Lerp(from.scale, to.scale, t);

            _buttonMaterial.SetColor(BaseColorID, baseCol);
            _buttonMaterial.SetColor(BorderColorID, borderCol);
            _buttonMaterial.SetColor(BorderDarkID, borderDark);
            _buttonMaterial.SetFloat(PressDepthID, pressDepth);

            if (useScaleAnimation && _rectTransform != null)
            {
                _rectTransform.localScale = _baseScale * scale;
            }
        }

        protected override void OnDestroy()
        {
            if (_buttonMaterial != null)
            {
                Destroy(_buttonMaterial);
                _buttonMaterial = null;
            }
            base.OnDestroy();
        }
    }
}
