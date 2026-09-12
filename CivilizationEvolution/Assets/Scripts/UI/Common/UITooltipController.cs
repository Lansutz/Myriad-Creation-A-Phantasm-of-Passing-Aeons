using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CivilizationEvolution.UI.Common
{
    /// <summary>
    /// Tooltip控制器 - 灰米色旧纸信息层
    /// 显示动画(Opacity+Scale)、跟随鼠标、自动避让屏幕边缘、三种类型
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class UITooltipController : MonoBehaviour
    {
        public enum TooltipType
        {
            Standard,   // 普通信息 #B8B09F / 边框#6D6659
            Important,  // 人物/国家/关键事件 边框#81745D
            Critical    // 战争/危险/灾害 边框#805B51
        }

        [System.Serializable]
        public class TooltipTypeStyle
        {
            public Color backgroundColor = new Color(0.722f, 0.690f, 0.624f, 0.98f); // #B8B09F
            public Color borderColor = new Color(0.427f, 0.400f, 0.349f, 1f); // #6D6659
            public Color titleColor = new Color(0.224f, 0.212f, 0.184f, 1f); // #39362F
        }

        [Header("引用")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text contentText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image shadowImage;
        [SerializeField] private RectTransform contentRect;

        [Header("动画")]
        [SerializeField] private float showDuration = 0.1f; // 100ms
        [SerializeField] private float hideDuration = 0.08f;
        [SerializeField] private float showScale = 0.98f;
        [SerializeField] private float hideDelay = 0.15f;

        [Header("跟随")]
        [SerializeField] private Vector2 mouseOffset = new Vector2(16f, -16f);
        [SerializeField] private float screenEdgePadding = 8f;
        [SerializeField] private bool followMouse = true;

        [Header("类型样式")]
        [SerializeField] private TooltipTypeStyle standardStyle = new TooltipTypeStyle
        {
            backgroundColor = new Color(0.722f, 0.690f, 0.624f, 0.98f),
            borderColor = new Color(0.427f, 0.400f, 0.349f, 1f),
            titleColor = new Color(0.224f, 0.212f, 0.184f, 1f)
        };

        [SerializeField] private TooltipTypeStyle importantStyle = new TooltipTypeStyle
        {
            backgroundColor = new Color(0.737f, 0.702f, 0.631f, 0.98f), // #BCB3A1
            borderColor = new Color(0.506f, 0.455f, 0.365f, 1f), // #81745D
            titleColor = new Color(0.208f, 0.196f, 0.173f, 1f) // #35322C
        };

        [SerializeField] private TooltipTypeStyle criticalStyle = new TooltipTypeStyle
        {
            backgroundColor = new Color(0.714f, 0.663f, 0.604f, 0.98f), // #B6A99A
            borderColor = new Color(0.502f, 0.357f, 0.318f, 1f), // #805B51
            titleColor = new Color(0.224f, 0.196f, 0.180f, 1f) // #39322E
        };

        // 材质属性ID
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int BorderColorID = Shader.PropertyToID("_BorderColor");

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Material _tooltipMaterial;
        private Coroutine _showCoroutine;
        private Coroutine _hideCoroutine;
        private bool _isVisible;
        private TooltipType _currentType = TooltipType.Standard;

        public bool IsVisible => _isVisible;
        public TooltipType CurrentType => _currentType;

        protected void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            if (backgroundImage != null && backgroundImage.material != null)
            {
                _tooltipMaterial = new Material(backgroundImage.material);
                backgroundImage.material = _tooltipMaterial;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 显示Tooltip
        /// </summary>
        public void Show(string title, string content, TooltipType type = TooltipType.Standard)
        {
            if (titleText != null) titleText.text = title;
            if (contentText != null) contentText.text = content;

            SetType(type);
            gameObject.SetActive(true);

            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);

            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);
            _showCoroutine = StartCoroutine(ShowRoutine());
        }

        /// <summary>
        /// 隐藏Tooltip
        /// </summary>
        public void Hide()
        {
            if (!_isVisible) return;

            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);

            if (_hideCoroutine != null)
                StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideRoutine());
        }

        /// <summary>
        /// 设置类型（切换边框颜色）
        /// </summary>
        public void SetType(TooltipType type)
        {
            _currentType = type;
            var style = type switch
            {
                TooltipType.Important => importantStyle,
                TooltipType.Critical => criticalStyle,
                _ => standardStyle
            };

            if (_tooltipMaterial != null)
            {
                _tooltipMaterial.SetColor(BaseColorID, style.backgroundColor);
                _tooltipMaterial.SetColor(BorderColorID, style.borderColor);
            }

            if (titleText != null)
                titleText.color = style.titleColor;
        }

        /// <summary>
        /// 更新位置（跟随鼠标）
        /// </summary>
        public void UpdatePosition(Vector2 mousePosition)
        {
            if (!_isVisible || !followMouse) return;

            Vector2 targetPos = mousePosition + mouseOffset;

            // 获取Canvas尺寸
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.rect.size;

            // 计算Tooltip尺寸
            Vector2 tooltipSize = _rectTransform.rect.size;

            // 边缘避让
            if (targetPos.x + tooltipSize.x > canvasSize.x - screenEdgePadding)
                targetPos.x = mousePosition.x - tooltipSize.x - mouseOffset.x;
            if (targetPos.y - tooltipSize.y < -canvasSize.y + screenEdgePadding)
                targetPos.y = mousePosition.y + tooltipSize.y + mouseOffset.y;

            // 最终钳制
            targetPos.x = Mathf.Clamp(targetPos.x, screenEdgePadding, canvasSize.x - tooltipSize.x - screenEdgePadding);
            targetPos.y = Mathf.Clamp(targetPos.y, -canvasSize.y + tooltipSize.y + screenEdgePadding, -screenEdgePadding);

            _rectTransform.anchoredPosition = targetPos;
        }

        private IEnumerator ShowRoutine()
        {
            _isVisible = true;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * showScale;

            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / showDuration);
                t = t * t * (3f - 2f * t);

                _canvasGroup.alpha = t;
                _rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _rectTransform.localScale = Vector3.one;
        }

        private IEnumerator HideRoutine()
        {
            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;
            Vector3 startScale = _rectTransform.localScale;

            while (elapsed < hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / hideDuration);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                _rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * showScale, t);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _isVisible = false;
            gameObject.SetActive(false);
        }

        protected void OnDestroy()
        {
            if (_tooltipMaterial != null)
            {
                Destroy(_tooltipMaterial);
                _tooltipMaterial = null;
            }
        }
    }
}
