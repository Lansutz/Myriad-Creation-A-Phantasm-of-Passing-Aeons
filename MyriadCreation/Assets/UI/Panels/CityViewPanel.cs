using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Rendering;

namespace MyriadCreation.UI.Panels
{
/// <summary>
/// 城市俯视图面板（P4.2）：一个 UI 一个文件；由 UIManager 注册与开关。
/// 只读 ISettlementQuery 的区块快照，布局与纹理按需生成，不写世界状态。
/// </summary>
public sealed class CityViewPanel : MonoBehaviour, Common.IUserInterfacePanel
{
	public const string Id = "city_view";

	private GameObject _canvasObj;
	private GameObject _root;
	private RawImage _image;
	private TMP_Text _title;
	private TMP_Text _empty;
	private ISettlementQuery _query;
	private int _anchorId = -1;

	public string PanelId => Id;

	public bool IsOpen => _root != null && _root.activeSelf;

	public string TitleText => _title != null ? _title.text : "";

	public string EmptyText => _empty != null ? _empty.text : "";

	public void SetQuery(ISettlementQuery query)
	{
		_query = query;
	}

	public void Build(Transform parent)
	{
		if (_root != null) return;
		Transform host = parent != null ? parent : transform;
		_canvasObj = new GameObject("CityViewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		_canvasObj.transform.SetParent(host, false);
		var canvas = _canvasObj.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 610;
		var scaler = _canvasObj.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		var overlay = new GameObject("CityViewOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		overlay.transform.SetParent(_canvasObj.transform, false);
		var ort = overlay.GetComponent<RectTransform>();
		ort.anchorMin = Vector2.zero;
		ort.anchorMax = Vector2.one;
		ort.offsetMin = Vector2.zero;
		ort.offsetMax = Vector2.zero;
		overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
		_root = overlay;
		_root.SetActive(false);

		var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
		panel.transform.SetParent(overlay.transform, false);
		var prt = panel.GetComponent<RectTransform>();
		prt.anchorMin = new Vector2(0.5f, 0.5f);
		prt.anchorMax = new Vector2(0.5f, 0.5f);
		prt.sizeDelta = new Vector2(760f, 820f);
		panel.GetComponent<Image>().color = new Color(0.09f, 0.10f, 0.13f, 0.98f);

		_title = CreateText(panel.transform, "Title", "城市视图", 26, new Color(0.88f, 0.75f, 0.45f), TextAlignmentOptions.Center);
		var trt = _title.rectTransform;
		trt.anchorMin = new Vector2(0f, 1f);
		trt.anchorMax = new Vector2(1f, 1f);
		trt.pivot = new Vector2(0.5f, 1f);
		trt.anchoredPosition = new Vector2(0f, -20f);
		trt.sizeDelta = new Vector2(-40f, 40f);

		var imageObj = new GameObject("Layout", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
		imageObj.transform.SetParent(panel.transform, false);
		var irt = imageObj.GetComponent<RectTransform>();
		irt.anchorMin = new Vector2(0.5f, 0.5f);
		irt.anchorMax = new Vector2(0.5f, 0.5f);
		irt.sizeDelta = new Vector2(660f, 660f);
		irt.anchoredPosition = new Vector2(0f, 10f);
		_image = imageObj.GetComponent<RawImage>();

		_empty = CreateText(panel.transform, "Empty", "", 20, new Color(0.7f, 0.72f, 0.78f), TextAlignmentOptions.Center);
		var ert = _empty.rectTransform;
		ert.anchorMin = new Vector2(0.5f, 0.5f);
		ert.anchorMax = new Vector2(0.5f, 0.5f);
		ert.sizeDelta = new Vector2(600f, 120f);

		var closeObj = new GameObject("Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
		closeObj.transform.SetParent(panel.transform, false);
		var crt = closeObj.GetComponent<RectTransform>();
		crt.anchorMin = new Vector2(0.5f, 0f);
		crt.anchorMax = new Vector2(0.5f, 0f);
		crt.pivot = new Vector2(0.5f, 0f);
		crt.anchoredPosition = new Vector2(0f, 18f);
		crt.sizeDelta = new Vector2(170f, 44f);
		closeObj.GetComponent<Image>().color = new Color(0.20f, 0.23f, 0.29f, 1f);
		closeObj.GetComponent<Button>().onClick.AddListener(Hide);
		var closeText = CreateText(closeObj.transform, "Text", "关闭", 20, Color.white, TextAlignmentOptions.Center);
		closeText.rectTransform.anchorMin = Vector2.zero;
		closeText.rectTransform.anchorMax = Vector2.one;
		closeText.rectTransform.offsetMin = Vector2.zero;
		closeText.rectTransform.offsetMax = Vector2.zero;
	}

	public void ShowFor(int anchorId)
	{
		if (_root == null) Build(null);
		_anchorId = anchorId;
		Show();
	}

	public void Show()
	{
		if (_root == null) Build(null);
		_root.SetActive(true);
		Refresh();
	}

	public void Hide()
	{
		if (_root != null) _root.SetActive(false);
	}

	private void Refresh()
	{
		if (_query == null || _anchorId < 0)
		{
			SetEmpty("无聚落数据");
			return;
		}
		SettlementSnapshotDto settlement = _query.GetSettlement(_anchorId);
		IReadOnlyList<SettlementBlockRowDto> rows = _query.GetSettlementBlocks(_anchorId);
		if (settlement == null || rows == null || rows.Count == 0)
		{
			if (_title != null) _title.text = settlement != null ? settlement.settlementName : "城市视图";
			SetEmpty("该聚落暂无区块布局");
			return;
		}
		if (_title != null) _title.text = settlement.settlementName + " · 城市视图";
		var specs = new List<CityBlockSpec>(rows.Count);
		foreach (SettlementBlockRowDto row in rows)
		{
			specs.Add(new CityBlockSpec
			{
				blockId = row.blockId,
				areaRatio = row.areaRatio,
				districtType = row.districtType,
				isWall = row.isWall,
				enabled = row.enabled
			});
		}
		CityLayoutPlan plan = CityLayoutGenerator.Build(specs);
		Texture2D texture = CityLayoutRenderer.RenderCached("settlement_" + _anchorId + "_" + plan.lots.Count, plan);
		if (_image != null)
		{
			_image.texture = texture;
			_image.color = Color.white;
		}
		SetEmpty("");
	}

	private void SetEmpty(string text)
	{
		if (_empty != null) _empty.text = text ?? "";
		if (_image != null) _image.enabled = string.IsNullOrEmpty(text);
	}

	private static TMP_Text CreateText(Transform parent, string name, string text, int size, Color color, TextAlignmentOptions align)
	{
		var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
		go.transform.SetParent(parent, false);
		var t = go.GetComponent<TextMeshProUGUI>();
		t.text = text;
		t.fontSize = size;
		t.color = color;
		t.alignment = align;
		return t;
	}
}
}
