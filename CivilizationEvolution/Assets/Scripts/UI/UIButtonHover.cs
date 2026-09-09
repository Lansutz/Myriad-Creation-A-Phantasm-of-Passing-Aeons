using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CivilizationEvolution.UI
{
 /// 按钮悬停动效组件：鼠标移入放大+高亮，移出平滑恢复，按下轻微收缩。 /// 挂在任意 Button 上即可，无需额外配置。 /// 用 Update 插值实现平滑过渡，不依赖 DOTween。    [RequireComponent(typeof(RectTransform))]
    public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("缩放")]
        [Tooltip("悬停时的缩放倍数")]
        public float hoverScale = 1.06f;
        [Tooltip("按下时的缩放倍数")]
        public float pressScale = 0.96f;
        [Tooltip("缩放平滑速度（越大越快）")]
        public float smoothSpeed = 12f;

        [Header("高亮")]
        [Tooltip("悬停时背景色亮度倍率")]
        public float hoverBrightness = 1.25f;
        [Tooltip("高亮平滑速度")]
        public float colorSmoothSpeed = 10f;

        private RectTransform _rt;
        private Image _image;
        private Vector3 _baseScale = Vector3.one;
        private Color _baseColor = Color.white;
        private float _currentScale = 1f;
        private float _targetScale = 1f;
        private float _currentBrightness = 1f;
        private float _targetBrightness = 1f;
        private bool _isHovering;
        private bool _isPressed;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _baseScale = _rt.localScale;
            if (_image != null) _baseColor = _image.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            UpdateTarget();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            _isPressed = false;
            UpdateTarget();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            UpdateTarget();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            UpdateTarget();
        }

        private void UpdateTarget()
        {
            if (_isPressed)
            {
                _targetScale = pressScale;
                _targetBrightness = hoverBrightness * 0.85f;
            }
            else if (_isHovering)
            {
                _targetScale = hoverScale;
                _targetBrightness = hoverBrightness;
            }
            else
            {
                _targetScale = 1f;
                _targetBrightness = 1f;
            }
        }

        void Update()
        {
 // 缩放插值            _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.unscaledDeltaTime * smoothSpeed);
            _rt.localScale = _baseScale * _currentScale;

 // 颜色亮度插值            if (_image != null)
            {
                _currentBrightness = Mathf.Lerp(_currentBrightness, _targetBrightness, Time.unscaledDeltaTime * colorSmoothSpeed);
                _image.color = _baseColor * _currentBrightness;
            }
        }

        void OnDisable()
        {
 // 禁用时重置，避免停在放大状态            _isHovering = false;
            _isPressed = false;
            _targetScale = 1f;
            _targetBrightness = 1f;
            if (_rt != null) _rt.localScale = _baseScale;
            if (_image != null) _image.color = _baseColor;
        }
    }
}
