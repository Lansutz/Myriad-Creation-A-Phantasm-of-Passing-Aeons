using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CivilizationEvolution.UI.Common
{
    /// <summary>
    /// 进度条控制器 - 凹入式石槽 + 内部填充实体色层
    /// 支持语义色切换、平滑进度过渡、完成脉冲动画
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIProgressBarController : MonoBehaviour
    {
        public enum ProgressType
        {
            Generic,     // 通用/经济 #9A8058
            Innovation,  // 革新 #66877A
            Culture,     // 文化 #98685A
            Religion,    // 宗教 #78677C
            Population,  // 人口 #B1A68E
            Military,    // 军事 #965A52
            Diplomacy,   // 外交 #687B83
            Danger,      // 危险/衰退 #87594F
            Positive     // 正向发展 #71876F
        }

        [System.Serializable]
        public class ProgressColorSet
        {
            public Color fillColor;
            public Color edgeHighlight;
        }

        [Header("引用")]
        [SerializeField] private Image trackImage;
        [SerializeField] private Image fillImage;

        [Header("进度")]
        [SerializeField] [Range(0f, 1f)] private float currentProgress = 0f;
        [SerializeField] private float transitionDuration = 0.2f; // 200ms平滑过渡
        [SerializeField] private bool useSmoothTransition = true;

        [Header("语义色")]
        [SerializeField] private ProgressType progressType = ProgressType.Generic;

        [Header("完成动画")]
        [SerializeField] private float completedPulseDuration = 0.35f;
        [SerializeField] private float completedPulseIntensity = 0.15f;

        // 语义色板（低饱和、偏灰、略带土色）
        private static readonly ProgressColorSet[] ColorSets = new ProgressColorSet[]
        {
            new ProgressColorSet // Generic/Economy #9A8058
            {
                fillColor = new Color(0.604f, 0.502f, 0.345f, 1f),
                edgeHighlight = new Color(0.667f, 0.576f, 0.420f, 1f) // #AA936B
            },
            new ProgressColorSet // Innovation #66877A
            {
                fillColor = new Color(0.400f, 0.529f, 0.478f, 1f),
                edgeHighlight = new Color(0.467f, 0.596f, 0.545f, 1f)
            },
            new ProgressColorSet // Culture #98685A
            {
                fillColor = new Color(0.596f, 0.408f, 0.353f, 1f),
                edgeHighlight = new Color(0.659f, 0.471f, 0.412f, 1f)
            },
            new ProgressColorSet // Religion #78677C
            {
                fillColor = new Color(0.471f, 0.404f, 0.486f, 1f),
                edgeHighlight = new Color(0.533f, 0.467f, 0.549f, 1f)
            },
            new ProgressColorSet // Population #B1A68E
            {
                fillColor = new Color(0.694f, 0.651f, 0.557f, 1f),
                edgeHighlight = new Color(0.757f, 0.714f, 0.620f, 1f)
            },
            new ProgressColorSet // Military #965A52
            {
                fillColor = new Color(0.588f, 0.353f, 0.322f, 1f),
                edgeHighlight = new Color(0.651f, 0.416f, 0.384f, 1f)
            },
            new ProgressColorSet // Diplomacy #687B83
            {
                fillColor = new Color(0.408f, 0.482f, 0.514f, 1f),
                edgeHighlight = new Color(0.471f, 0.545f, 0.576f, 1f)
            },
            new ProgressColorSet // Danger #87594F
            {
                fillColor = new Color(0.529f, 0.349f, 0.310f, 1f),
                edgeHighlight = new Color(0.592f, 0.412f, 0.373f, 1f)
            },
            new ProgressColorSet // Positive #71876F
            {
                fillColor = new Color(0.443f, 0.529f, 0.435f, 1f),
                edgeHighlight = new Color(0.506f, 0.592f, 0.498f, 1f)
            }
        };

        // 材质属性ID
        private static readonly int FillColorID = Shader.PropertyToID("_FillColor");
        private static readonly int EdgeHighlightColorID = Shader.PropertyToID("_EdgeHighlightColor");
        private static readonly int ProgressID = Shader.PropertyToID("_Progress");
        private static readonly int CompletedID = Shader.PropertyToID("_Completed");
        private static readonly int CompletedPulseID = Shader.PropertyToID("_CompletedPulse");
        private static readonly int PulsePhaseID = Shader.PropertyToID("_PulsePhase");

        private Material _fillMaterial;
        private float _targetProgress;
        private Coroutine _transitionCoroutine;
        private Coroutine _completedCoroutine;
        private bool _isCompleted;

        public float Progress => currentProgress;
        public ProgressType Type => progressType;

        protected void Awake()
        {
            if (fillImage != null && fillImage.material != null)
            {
                _fillMaterial = new Material(fillImage.material);
                fillImage.material = _fillMaterial;
            }

            ApplyType(progressType);
            SetProgressImmediate(currentProgress);
        }

        /// <summary>
        /// 设置进度（平滑过渡）
        /// </summary>
        public void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            _targetProgress = value;

            if (_isCompleted && value < 1f)
            {
                _isCompleted = false;
                SetCompletedState(0f);
            }

            if (useSmoothTransition && gameObject.activeInHierarchy)
            {
                if (_transitionCoroutine != null)
                    StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = StartCoroutine(SmoothTransition(value));
            }
            else
            {
                SetProgressImmediate(value);
            }

            // 检测完成
            if (value >= 1f && !_isCompleted)
            {
                _isCompleted = true;
                TriggerCompletedPulse();
            }
        }

        /// <summary>
        /// 立即设置进度（无过渡）
        /// </summary>
        public void SetProgressImmediate(float value)
        {
            currentProgress = Mathf.Clamp01(value);
            _targetProgress = currentProgress;
            if (_fillMaterial != null)
            {
                _fillMaterial.SetFloat(ProgressID, currentProgress);
            }
            if (fillImage != null)
            {
                fillImage.fillAmount = currentProgress;
            }
        }

        /// <summary>
        /// 设置语义类型（切换颜色）
        /// </summary>
        public void SetType(ProgressType type)
        {
            if (progressType == type) return;
            progressType = type;
            ApplyType(type);
        }

        private void ApplyType(ProgressType type)
        {
            if (_fillMaterial == null) return;
            var set = ColorSets[(int)type];
            _fillMaterial.SetColor(FillColorID, set.fillColor);
            _fillMaterial.SetColor(EdgeHighlightColorID, set.edgeHighlight);
        }

        private IEnumerator SmoothTransition(float target)
        {
            float from = currentProgress;
            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, transitionDuration * Mathf.Abs(target - from));

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t); // SmoothStep
                currentProgress = Mathf.Lerp(from, target, t);

                if (_fillMaterial != null)
                    _fillMaterial.SetFloat(ProgressID, currentProgress);
                if (fillImage != null)
                    fillImage.fillAmount = currentProgress;

                yield return null;
            }

            currentProgress = target;
            if (_fillMaterial != null)
                _fillMaterial.SetFloat(ProgressID, currentProgress);
            if (fillImage != null)
                fillImage.fillAmount = currentProgress;
        }

        private void TriggerCompletedPulse()
        {
            if (_completedCoroutine != null)
                StopCoroutine(_completedCoroutine);
            _completedCoroutine = StartCoroutine(CompletedPulseRoutine());
        }

        private IEnumerator CompletedPulseRoutine()
        {
            SetCompletedState(1f);
            float elapsed = 0f;

            while (elapsed < completedPulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / completedPulseDuration;
                float pulse = Mathf.Sin(t * Mathf.PI) * completedPulseIntensity;
                if (_fillMaterial != null)
                {
                    _fillMaterial.SetFloat(CompletedPulseID, pulse);
                    _fillMaterial.SetFloat(PulsePhaseID, Random.Range(0f, 6.283f));
                }
                yield return null;
            }

            SetCompletedState(0f);
        }

        private void SetCompletedState(float value)
        {
            if (_fillMaterial != null)
            {
                _fillMaterial.SetFloat(CompletedID, value);
                if (value <= 0f)
                    _fillMaterial.SetFloat(CompletedPulseID, 0f);
            }
        }

        protected void OnDestroy()
        {
            if (_fillMaterial != null)
            {
                Destroy(_fillMaterial);
                _fillMaterial = null;
            }
        }
    }
}
