using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.UI.Common;

namespace MyriadCreation.UI.Panels
{
/// <summary>
/// 革新面板（P1.4 UI）：本文件独立负责这块 UI；只读 IInnovationQuery，
/// 由 UIManager 注册到 <see cref="UiPanelRegistry"/>。只展示，不改研究状态。
/// </summary>
public sealed class InnovationPanelView : MonoBehaviour, IUserInterfacePanel
{
	private const float PanelWidth = 560f;
	private const float PanelHeight = 600f;

	private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.11f, 0.96f);
	private static readonly Color HeaderColor = new Color(0.55f, 0.48f, 0.25f, 1f);
	private static readonly Color ButtonColor = new Color(0.18f, 0.20f, 0.26f, 0.95f);

	private IInnovationQuery _innovations;
	private int _viewRealmId = -1;

	private bool _built;
	private GameObject _root;
	private TextMeshProUGUI _titleText;
	private TextMeshProUGUI _bodyText;

	public string PanelId => "innovation";

	public bool IsOpen => _root != null && _root.activeSelf;

	public string BodyText => _bodyText != null ? _bodyText.text : string.Empty;

	public void SetWorldReadModel(IWorldReadModel world)
	{
		SetInnovationQuery(world);
	}

	public void SetInnovationQuery(IInnovationQuery query)
	{
		_innovations = query;
		if (IsOpen)
		{
			Refresh();
		}
	}

	public void SetViewRealm(int realmId)
	{
		_viewRealmId = realmId;
		if (IsOpen)
		{
			Refresh();
		}
	}

	public void Build(Transform parent)
	{
		if (_built)
		{
			return;
		}
		_built = true;
		Transform host = parent != null ? parent : transform;

		_root = UiPanelBuilder.CreateRoot(host, "InnovationPanel", new Vector2(PanelWidth, PanelHeight),
			new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelColor);

		_titleText = UiPanelBuilder.CreateText(_root.transform, "Title", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
		RectTransform titleRect = _titleText.rectTransform;
		titleRect.anchorMin = new Vector2(0f, 1f);
		titleRect.anchorMax = new Vector2(1f, 1f);
		titleRect.pivot = new Vector2(0.5f, 1f);
		titleRect.offsetMin = new Vector2(16f, -50f);
		titleRect.offsetMax = new Vector2(-60f, -10f);
		_titleText.color = HeaderColor;
		_titleText.text = "革新";

		UiPanelBuilder.CreateButton(_root.transform, "×", new Vector2(32f, 32f), new Vector2(1f, 1f), new Vector2(-12f, -10f), Hide, ButtonColor);
		UiPanelBuilder.CreateButton(_root.transform, "刷新", new Vector2(64f, 32f), new Vector2(0f, 0f), new Vector2(16f, 12f), Refresh, ButtonColor);

		_bodyText = UiPanelBuilder.CreateText(_root.transform, "Body", 16f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
		UiPanelBuilder.Stretch(_bodyText.rectTransform, new Vector2(16f, 56f), new Vector2(-16f, -56f));

		_root.SetActive(false);
	}

	public void Show()
	{
		if (!_built)
		{
			Build(transform);
		}
		_root.SetActive(true);
		Refresh();
	}

	public void Hide()
	{
		if (_root != null)
		{
			_root.SetActive(false);
		}
	}

	/// <summary>重新读只读快照；面板不持有革新状态。</summary>
	public void Refresh()
	{
		if (_bodyText == null)
		{
			return;
		}
		if (_innovations == null)
		{
			_bodyText.text = "-";
			return;
		}

		StringBuilder sb = new StringBuilder();
		InnovationSnapshotDto snapshot = _innovations.GetInnovationSnapshot(_viewRealmId);
		sb.Append("已研究: ").Append(snapshot?.researchedCount ?? 0).Append(" 项");
		AppendResearched(sb, snapshot);

		IReadOnlyList<InnovationAvailabilityDto> available = _innovations.GetAvailableInnovations(_viewRealmId);
		sb.Append("\n\n可研究: ").Append(available?.Count ?? 0).Append(" 项");
		if (available == null || available.Count == 0)
		{
			sb.Append("\n  -");
		}
		else
		{
			for (int i = 0; i < available.Count; i++)
			{
				InnovationAvailabilityDto item = available[i];
				if (item == null)
				{
					continue;
				}
				sb.Append("\n  · ").Append(string.IsNullOrEmpty(item.name) ? "#" + item.innovationId : item.name);
				if (item.prerequisiteIds != null && item.prerequisiteIds.Count > 0)
				{
					sb.Append("（前置: ");
					for (int p = 0; p < item.prerequisiteIds.Count; p++)
					{
						if (p > 0)
						{
							sb.Append("、");
						}
						sb.Append(_innovations.GetInnovationName(item.prerequisiteIds[p]));
					}
					sb.Append("）");
				}
			}
		}
		_bodyText.text = sb.ToString();
	}

	private void AppendResearched(StringBuilder sb, InnovationSnapshotDto snapshot)
	{
		if (snapshot?.researchedInnovationIds == null || snapshot.researchedInnovationIds.Count == 0)
		{
			sb.Append("\n  -");
			return;
		}
		for (int i = 0; i < snapshot.researchedInnovationIds.Count; i++)
		{
			sb.Append("\n  · ").Append(_innovations.GetInnovationName(snapshot.researchedInnovationIds[i]));
		}
	}
}
}
