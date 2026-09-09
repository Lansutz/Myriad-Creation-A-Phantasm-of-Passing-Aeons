using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CivilizationEvolution.Core;
using CivilizationEvolution.Render;
using CivilizationEvolution.Race;
using CivilizationEvolution.Character;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.UI
{
 /// UIManager.Feedback —— 用户反馈（事件日志/Toast通知/确认对话框）（partial class，与 UIManager.cs 共享字段与组件引用）    public partial class UIManager : MonoBehaviour
    {

 /// <summary>添加事件日志（按类型着色）</summary>        public void AddEventLog(string message, EventLogKind kind = EventLogKind.Info)
        {
            string timestamp = world != null ? $"[{world.currentYear}年{world.currentDay}天] " : "";
            Color color = kind switch
            {
                EventLogKind.System => UITheme.LogSystem,
                EventLogKind.War => UITheme.LogWar,
                EventLogKind.Economy => UITheme.LogEconomy,
                EventLogKind.Warning => UITheme.LogWarning,
                _ => UITheme.LogInfo
            };
            string hex = ColorUtility.ToHtmlStringRGB(color);
            _eventLog.Add($"<color=#{ColorUtility.ToHtmlStringRGB(UITheme.TextDim)}>{timestamp}</color><color=#{hex}>{message}</color>");

            if (_eventLog.Count > MaxLogEntries)
                _eventLog.RemoveAt(0);

            if (eventLogText != null)
            {
                eventLogText.text = string.Join("\n", _eventLog);
                if (eventLogScroll != null)
                    eventLogScroll.verticalNormalizedPosition = 0f;
            }
        }


 /// <summary>显示顶部 Toast 提示（支持排队，自动渐隐）</summary>        public void ShowToast(string message, float duration = 3f)
        {
            if (string.IsNullOrEmpty(message)) return;
            _toastQueue.Enqueue(message);
            if (_toastRoutine == null)
                _toastRoutine = StartCoroutine(ToastLoop());
        }


        private IEnumerator ToastLoop()
        {
            while (_toastQueue.Count > 0)
            {
                yield return ShowToastCoroutine(_toastQueue.Dequeue(), 3f);
            }
            _toastRoutine = null;
        }


        private IEnumerator ShowToastCoroutine(string message, float duration)
        {
            var go = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -60f);
            rt.sizeDelta = new Vector2(400f, 46f);

            var img = go.AddComponent<Image>();
            img.color = UITheme.ToastBg;
            img.sprite = UITheme.RoundedPanelSprite;
            img.type = Image.Type.Sliced;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
            txt.font = TMPFontUtility.GetChineseFont();
            txt.text = message;
            txt.fontSize = 18;
            txt.color = UITheme.TextMain;
            txt.alignment = TextAlignmentOptions.Center;
            txt.overflowMode = TextOverflowModes.Overflow;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;

            var cg = go.GetComponent<CanvasGroup>();

            float t = 0f;
            const float fadeIn = 0.25f;
            const float fadeOut = 0.4f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(duration);

            t = fadeOut;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / fadeOut);
                yield return null;
            }
            Destroy(go);
        }


 /// <summary>显示确认对话框（简化：记录日志）</summary>        public void ShowConfirmation(string title, string message, Action onConfirm, Action onCancel = null)
        {
 // 简化：直接确认            AddEventLog($"{title}: {message}", EventLogKind.Warning);
            onConfirm?.Invoke();
        }

    }
}
