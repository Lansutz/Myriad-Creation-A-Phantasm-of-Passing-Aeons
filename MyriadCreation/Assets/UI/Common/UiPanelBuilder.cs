using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MyriadCreation.UI.Common
{
/// <summary>
/// 面板构建的共用骨架（P1.6）：只做 Rect/Text/Button 的重复劳动，
/// 具体面板的内容与交互仍由各自那个文件负责。
/// </summary>
public static class UiPanelBuilder
{
	public static GameObject CreateRoot(Transform parent, string name, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Color color)
	{
		var go = new GameObject(name, typeof(RectTransform), typeof(Image));
		go.transform.SetParent(parent, false);
		RectTransform rect = (RectTransform)go.transform;
		rect.anchorMin = anchor;
		rect.anchorMax = anchor;
		rect.pivot = pivot;
		rect.sizeDelta = size;
		rect.anchoredPosition = anchoredPosition;
		go.GetComponent<Image>().color = color;
		return go;
	}

	public static TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, FontStyles style, TextAlignmentOptions alignment)
	{
		var go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		var text = go.AddComponent<TextMeshProUGUI>();
		text.fontSize = fontSize;
		text.fontStyle = style;
		text.alignment = alignment;
		text.color = Color.white;
		text.raycastTarget = false;
		return text;
	}

	public static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 anchor, Vector2 position, UnityAction onClick, Color color)
	{
		var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
		go.transform.SetParent(parent, false);
		RectTransform rect = (RectTransform)go.transform;
		rect.anchorMin = anchor;
		rect.anchorMax = anchor;
		rect.pivot = anchor;
		rect.sizeDelta = size;
		rect.anchoredPosition = position;
		go.GetComponent<Image>().color = color;

		var button = go.GetComponent<Button>();
		button.onClick.AddListener(onClick);

		TextMeshProUGUI text = CreateText(go.transform, "Label", 18f, FontStyles.Bold, TextAlignmentOptions.Center);
		RectTransform textRect = text.rectTransform;
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = Vector2.zero;
		textRect.offsetMax = Vector2.zero;
		text.text = label;
		return button;
	}

	public static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = offsetMin;
		rect.offsetMax = offsetMax;
	}

	public static void SetText(TMP_Text target, string value)
	{
		if (target != null)
		{
			target.text = value;
		}
	}
}
}
