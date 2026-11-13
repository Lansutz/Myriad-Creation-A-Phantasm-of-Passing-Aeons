using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using MyriadCreation.Core.Contracts;
using MyriadCreation.UI.Common;

namespace MyriadCreation.UI.Panels
{
/// <summary>
/// 家族树面板（P1.6）：本文件独立负责这块 UI；只读角色列表/详情契约，
/// 由 UIManager 注册到 <see cref="UiPanelRegistry"/>。
/// </summary>
public sealed class FamilyTreePanelView : MonoBehaviour, IUserInterfacePanel
{
	private const float PanelWidth = 500f;
	private const float PanelHeight = 560f;

	private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.11f, 0.96f);
	private static readonly Color HeaderColor = new Color(0.55f, 0.48f, 0.25f, 1f);
	private static readonly Color ButtonColor = new Color(0.18f, 0.20f, 0.26f, 0.95f);

	private ICharacterQuery _characters;
	private ICharacterDetailQuery _details;
	private int _viewRealmId = -1;
	private readonly List<int> _ids = new List<int>();
	private int _index;

	private bool _built;
	private GameObject _root;
	private TextMeshProUGUI _titleText;
	private TextMeshProUGUI _pageText;
	private TextMeshProUGUI _bodyText;

	public string PanelId => "family_tree";

	public bool IsOpen => _root != null && _root.activeSelf;

	public int CurrentCharacterId => CurrentId();

	public string BodyText => _bodyText != null ? _bodyText.text : string.Empty;

	public void SetWorldReadModel(IWorldReadModel world)
	{
		SetQueries(world, world);
	}

	public void SetQueries(ICharacterQuery characters, ICharacterDetailQuery details)
	{
		_characters = characters;
		_details = details;
		if (IsOpen)
		{
			RebuildList();
			Refresh();
		}
	}

	public void SetViewRealm(int realmId)
	{
		_viewRealmId = realmId;
		if (IsOpen)
		{
			RebuildList();
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

		_root = UiPanelBuilder.CreateRoot(host, "FamilyTreePanel", new Vector2(PanelWidth, PanelHeight),
			new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), PanelColor);

		_titleText = UiPanelBuilder.CreateText(_root.transform, "Title", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
		RectTransform titleRect = _titleText.rectTransform;
		titleRect.anchorMin = new Vector2(0f, 1f);
		titleRect.anchorMax = new Vector2(1f, 1f);
		titleRect.pivot = new Vector2(0.5f, 1f);
		titleRect.offsetMin = new Vector2(16f, -50f);
		titleRect.offsetMax = new Vector2(-60f, -10f);
		_titleText.color = HeaderColor;

		_pageText = UiPanelBuilder.CreateText(_root.transform, "Page", 15f, FontStyles.Normal, TextAlignmentOptions.Right);
		RectTransform pageRect = _pageText.rectTransform;
		pageRect.anchorMin = new Vector2(1f, 1f);
		pageRect.anchorMax = new Vector2(1f, 1f);
		pageRect.pivot = new Vector2(1f, 1f);
		pageRect.sizeDelta = new Vector2(120f, 24f);
		pageRect.anchoredPosition = new Vector2(-56f, -14f);

		UiPanelBuilder.CreateButton(_root.transform, "×", new Vector2(32f, 32f), new Vector2(1f, 1f), new Vector2(-12f, -10f), Hide, ButtonColor);
		UiPanelBuilder.CreateButton(_root.transform, "<", new Vector2(40f, 32f), new Vector2(0f, 0f), new Vector2(16f, 12f), Prev, ButtonColor);
		UiPanelBuilder.CreateButton(_root.transform, ">", new Vector2(40f, 32f), new Vector2(0f, 0f), new Vector2(64f, 12f), Next, ButtonColor);

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
		RebuildList();
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

	public void Next()
	{
		if (_ids.Count == 0)
		{
			return;
		}
		_index = (_index + 1) % _ids.Count;
		Refresh();
	}

	public void Prev()
	{
		if (_ids.Count == 0)
		{
			return;
		}
		_index = (_index - 1 + _ids.Count) % _ids.Count;
		Refresh();
	}

	private void RebuildList()
	{
		_ids.Clear();
		if (_characters != null)
		{
			IReadOnlyList<CharacterRowDto> rows = _characters.GetCharactersInRealm(_viewRealmId);
			if (rows != null)
			{
				for (int i = 0; i < rows.Count; i++)
				{
					if (rows[i] != null)
					{
						_ids.Add(rows[i].characterId);
					}
				}
			}
		}
		_ids.Sort();
		_index = 0;
	}

	private void Refresh()
	{
		int id = CurrentId();
		CharacterDetailDto dto = id >= 0 && _details != null ? _details.GetCharacterDetail(id) : null;
		if (dto == null)
		{
			UiPanelBuilder.SetText(_titleText, "家族");
			UiPanelBuilder.SetText(_pageText, string.Empty);
			UiPanelBuilder.SetText(_bodyText, "-");
			return;
		}
		UiPanelBuilder.SetText(_titleText, dto.name + " 的家族");
		UiPanelBuilder.SetText(_pageText, (_index + 1) + " / " + _ids.Count);
		UiPanelBuilder.SetText(_bodyText, Format(dto));
	}

	private int CurrentId()
	{
		if (_ids.Count == 0)
		{
			return -1;
		}
		_index = Mathf.Clamp(_index, 0, _ids.Count - 1);
		return _ids[_index];
	}

	private static string Format(CharacterDetailDto dto)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("王朝: ").Append(Text(dto.dynastyName));
		sb.Append("\n家族: ").Append(Text(dto.familyName));
		sb.Append("\n本人: ").Append(dto.name).Append("（").Append(dto.age).Append("岁，")
			.Append(dto.isAlive ? "在世" : "已故").Append("）");
		if (dto.isRuler)
		{
			sb.Append(" · 统治者");
		}
		else if (dto.isHeir)
		{
			sb.Append(" · 继承人");
		}

		sb.Append("\n\n父: ").Append(Describe(dto.father));
		sb.Append("\n母: ").Append(Describe(dto.mother));
		sb.Append("\n配偶: ").Append(Describe(dto.spouse));

		sb.Append("\n\n兄弟姐妹（").Append(dto.siblings?.Count ?? 0).Append("）：");
		AppendMembers(sb, dto.siblings);
		sb.Append("\n子女（").Append(dto.children?.Count ?? 0).Append("）：");
		AppendMembers(sb, dto.children);
		return sb.ToString();
	}

	private static void AppendMembers(StringBuilder sb, IReadOnlyList<FamilyMemberDto> members)
	{
		if (members == null || members.Count == 0)
		{
			sb.Append("\n  -");
			return;
		}
		for (int i = 0; i < members.Count; i++)
		{
			sb.Append("\n  · ").Append(Describe(members[i]));
		}
	}

	private static string Describe(FamilyMemberDto member)
	{
		if (member == null || string.IsNullOrEmpty(member.name))
		{
			return "-";
		}
		string status = member.isAlive ? "在世" : "已故";
		return member.name + "（" + member.age + "岁，" + status + "）";
	}

	private static string Text(string value)
	{
		return string.IsNullOrEmpty(value) ? "-" : value;
	}
}
}
