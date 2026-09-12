using UnityEngine;
using UnityEngine.UI;

namespace CivilizationEvolution.UI.Common
{
    /// <summary>
    /// 输入框控制器 - 内凹暗槽编辑区域
    /// 管理Focus/Error/Valid状态的边框颜色切换
    /// 附加在UnityEngine.UI.InputField的Background Image上
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIInputFieldController : MonoBehaviour
    {
        public enum InputState
        {
            Normal,
            Hover,
            Focus,
            Error,
            Valid,
            Disabled
        }

        [Header("引用")]
        [SerializeField] private InputField inputField;
        [SerializeField] private Image backgroundImage;

        [Header("状态颜色")]
        [SerializeField] private Color normalBorder = new Color(0.427f, 0.400f, 0.345f, 1f); // #6D6658
        [SerializeField] private Color hoverBorder = new Color(0.463f, 0.420f, 0.341f, 1f); // #766B57
        [SerializeField] private Color focusBorder = new Color(0.584f, 0.510f, 0.392f, 1f); // #958264
        [SerializeField] private Color errorBorder = new Color(0.502f, 0.357f, 0.318f, 1f); // #805B51
        [SerializeField] private Color validBorder = new Color(0.408f, 0.478f, 0.408f, 1f); // #687A68
        [SerializeField] private Color disabledBorder = new Color(0.369f, 0.353f, 0.318f, 1f); // #5E5A51

        [Header("状态背景")]
        [SerializeField] private Color normalBg = new Color(0.337f, 0.322f, 0.286f, 0.98f); // #565249
        [SerializeField] private Color hoverBg = new Color(0.353f, 0.333f, 0.298f, 0.98f); // #5A554C
        [SerializeField] private Color focusBg = new Color(0.373f, 0.353f, 0.314f, 0.98f); // #5F5A50
        [SerializeField] private Color errorBg = new Color(0.353f, 0.318f, 0.294f, 0.98f); // #5A514B
        [SerializeField] private Color disabledBg = new Color(0.302f, 0.290f, 0.263f, 0.98f); // #4D4A43

        [Header("动画")]
        [SerializeField] private float transitionDuration = 0.12f;

        // 材质属性ID
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int BorderColorID = Shader.PropertyToID("_BorderColor");
        private static readonly int FocusBlendID = Shader.PropertyToID("_FocusBlend");

        private Material _inputMaterial;
        private InputState _currentState = InputState.Normal;
        private Coroutine _transitionCoroutine;
        private Color _currentBorder;
        private Color _currentBg;

        public InputState CurrentState => _currentState;
        public InputField InputField => inputField;

        protected void Awake()
        {
            if (backgroundImage == null)
                backgroundImage = GetComponent<Image>();

            if (backgroundImage != null && backgroundImage.material != null)
            {
                _inputMaterial = new Material(backgroundImage.material);
                backgroundImage.material = _inputMaterial;
            }

            _currentBorder = normalBorder;
            _currentBg = normalBg;
        }

        private bool _lastFocused;

        protected void OnEnable()
        {
            if (inputField != null)
                _lastFocused = inputField.isFocused;
        }

        protected void Update()
        {
            if (inputField == null) return;

            bool isFocused = inputField.isFocused;
            if (isFocused != _lastFocused)
            {
                _lastFocused = isFocused;
                if (isFocused)
                    OnFocusGained();
                else
                    OnFocusLost();
            }
        }

        /// <summary>
        /// 设置状态
        /// </summary>
        public void SetState(InputState state)
        {
            if (_currentState == state) return;
            _currentState = state;

            Color targetBorder = state switch
            {
                InputState.Hover => hoverBorder,
                InputState.Focus => focusBorder,
                InputState.Error => errorBorder,
                InputState.Valid => validBorder,
                InputState.Disabled => disabledBorder,
                _ => normalBorder
            };

            Color targetBg = state switch
            {
                InputState.Hover => hoverBg,
                InputState.Focus => focusBg,
                InputState.Error => errorBg,
                InputState.Disabled => disabledBg,
                _ => normalBg
            };

            if (_transitionCoroutine != null)
                StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(TransitionRoutine(targetBorder, targetBg));
        }

        /// <summary>
        /// 设置错误状态（带错误信息）
        /// </summary>
        public void SetError(string errorMessage = null)
        {
            SetState(InputState.Error);
        }

        /// <summary>
        /// 设置验证通过状态
        /// </summary>
        public void SetValid()
        {
            SetState(InputState.Valid);
        }

        /// <summary>
        /// 重置为正常状态
        /// </summary>
        public void ResetState()
        {
            SetState(InputState.Normal);
        }

        private void OnFocusGained()
        {
            if (_currentState != InputState.Error && _currentState != InputState.Valid)
                SetState(InputState.Focus);
        }

        private void OnFocusLost()
        {
            if (_currentState == InputState.Focus)
                SetState(InputState.Normal);
        }

        private System.Collections.IEnumerator TransitionRoutine(Color targetBorder, Color targetBg)
        {
            float elapsed = 0f;
            Color startBorder = _currentBorder;
            Color startBg = _currentBg;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                t = t * t * (3f - 2f * t);

                _currentBorder = Color.Lerp(startBorder, targetBorder, t);
                _currentBg = Color.Lerp(startBg, targetBg, t);
                ApplyMaterial();
                yield return null;
            }

            _currentBorder = targetBorder;
            _currentBg = targetBg;
            ApplyMaterial();
        }

        private void ApplyMaterial()
        {
            if (_inputMaterial == null) return;

            _inputMaterial.SetColor(BaseColorID, _currentBg);
            _inputMaterial.SetColor(BorderColorID, _currentBorder);
            _inputMaterial.SetFloat(FocusBlendID,
                _currentState == InputState.Focus ? 1f : 0f);
        }

        protected void OnDestroy()
        {
            if (_inputMaterial != null)
            {
                Destroy(_inputMaterial);
                _inputMaterial = null;
            }
        }
    }
}
