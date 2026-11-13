using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using MyriadCreation.Core.Contracts;
using MyriadCreation.UI.Common;

namespace MyriadCreation.UI.Panels
{
/// <summary>
/// 角色面板（P1.6）：本文件独立负责这块 UI 的构建、数据与翻页交互，
/// 由 UIManager 通过 <see cref="UiPanelRegistry"/> 注册；只读 IWorldReadModel，不引用 Simulation。
/// </summary>
public sealed class CharacterPanelView : MonoBehaviour, IUserInterfacePanel
{
	private const float PanelWidth = 520f;
	private const float PanelHeight = 620f;

	private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.11f, 0.96f);
	private static readonly Color HeaderColor = new Color(0.55f, 0.48f, 0.25f, 1f);
	private static readonly Color ButtonColor = new Color(0.18f, 0.20f, 0.26f, 0.95f);

	private ICharacterQuery _characterQuery;
	private ICharacterDetailQuery _detailQuery;
	private int _viewRealmId = -1;
	private readonly List<int> _ids = new List<int>();
	private int _index;

	private bool _built;
	private GameObject _root;
	private TextMeshProUGUI _nameText;
	private TextMeshProUGUI _pageText;
	private TextMeshProUGUI _bodyText;

	public string PanelId => "character";

	public bool IsOpen => _root != null && _root.activeSelf;

	/// <summary>当前显示的角色 id；-1 = 无角色。</summary>
	public int CurrentCharacterId => CurrentId();

	public string BodyText => _bodyText != null ? _bodyText.text : string.Empty;

	public string TitleText => _nameText != null ? _nameText.text : string.Empty;

	public void SetWorldReadModel(IWorldReadModel world)
	{
		SetQueries(world, world);
	}

	/// <summary>窄契约入口：面板只读角色列表与角色详情，测试可只注入这两项。</summary>
	public void SetQueries(ICharacterQuery characters, ICharacterDetailQuery details)
	{
		_characterQuery = characters;
		_detailQuery = details;
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

		_root = new GameObject("CharacterPanel", typeof(RectTransform), typeof(Image));
		_root.transform.SetParent(host, false);
		RectTransform rootRect = (RectTransform)_root.transform;
		rootRect.anchorMin = new Vector2(1f, 0f);
		rootRect.anchorMax = new Vector2(1f, 0f);
		rootRect.pivot = new Vector2(1f, 0f);
		rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
		rootRect.anchoredPosition = new Vector2(-24f, 24f);
		_root.GetComponent<Image>().color = PanelColor;

		// 标题栏：姓名为本面板唯一标题；翻页与关闭都在同一条栏上。
		_nameText = CreateText(_root.transform, "Name", 24f, FontStyles.Bold, TextAlignmentOptions.Left);
		SetRect(_nameText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
			new Vector2(0f, 0f), new Vector2(-120f, 40f), new Vector2(16f, -10f));
		_nameText.color = HeaderColor;

		_pageText = CreateText(_root.transform, "Page", 15f, FontStyles.Normal, TextAlignmentOptions.Right);
		SetRect(_pageText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
			new Vector2(0f, 0f), new Vector2(120f, 24f), new Vector2(-56f, -14f));

		CreateButton(_root.transform, "×", new Vector2(32f, 32f), new Vector2(1f, 1f), new Vector2(-12f, -10f), Hide);
		CreateButton(_root.transform, "<", new Vector2(40f, 32f), new Vector2(0f, 0f), new Vector2(16f, 12f), Prev);
		CreateButton(_root.transform, ">", new Vector2(40f, 32f), new Vector2(0f, 0f), new Vector2(64f, 12f), Next);

		_bodyText = CreateText(_root.transform, "Body", 16f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
		RectTransform bodyRect = _bodyText.rectTransform;
		bodyRect.anchorMin = Vector2.zero;
		bodyRect.anchorMax = Vector2.one;
		bodyRect.offsetMin = new Vector2(16f, 56f);
		bodyRect.offsetMax = new Vector2(-16f, -56f);

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
		if (_characterQuery != null)
		{
			IReadOnlyList<CharacterRowDto> characters = _characterQuery.GetCharactersInRealm(_viewRealmId);
			if (characters != null)
			{
				for (int i = 0; i < characters.Count; i++)
				{
					if (characters[i] != null)
					{
						_ids.Add(characters[i].characterId);
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
		CharacterDetailDto dto = id >= 0 && _detailQuery != null ? _detailQuery.GetCharacterDetail(id) : null;
		if (dto == null)
		{
			SetText(_nameText, "角色");
			SetText(_pageText, string.Empty);
			SetText(_bodyText, "-");
			return;
		}
		SetText(_nameText, dto.name);
		SetText(_pageText, (_index + 1) + " / " + _ids.Count);
		SetText(_bodyText, Format(dto));
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
		string office = string.IsNullOrEmpty(dto.officeName)
			? (dto.officeTitleId >= 0 ? "#" + dto.officeTitleId : "-")
			: dto.officeName;
		sb.Append("年龄 ").Append(dto.age).Append(" · ").Append(dto.isMale ? "男" : "女")
			.Append(" · ").Append(dto.isAlive ? "在世" : "已故");
		sb.Append("\n所属: ").Append(string.IsNullOrEmpty(dto.realmName) ? "-" : dto.realmName);
		sb.Append("\n角色: ").Append(dto.roleName).Append(" | 官职: ").Append(office);
		sb.Append("\n\n武力 ").Append(dto.prowess.ToString("F0"))
			.Append(" | 社交 ").Append(dto.social.ToString("F0"))
			.Append(" | 军事 ").Append(dto.military.ToString("F0"))
			.Append(" | 管理 ").Append(dto.management.ToString("F0"));
		sb.Append("\n谋略 ").Append(dto.conspiracy.ToString("F0"))
			.Append(" | 学术 ").Append(dto.scholarship.ToString("F0"))
			.Append(" | 威望 ").Append(dto.prestige.ToString("F0"))
			.Append(" | 健康 ").Append(dto.health.ToString("F0"));
		sb.Append("\n压力 ").Append(dto.stress.ToString("F0"))
			.Append(" | 虔诚 ").Append(dto.piety.ToString("F0"));

		sb.Append("\n\n性格: ").Append(string.IsNullOrWhiteSpace(dto.personality) ? "-" : dto.personality);
		sb.Append("\n特质: ").Append(dto.traits == null || dto.traits.Count == 0 ? "-" : string.Join("、", dto.traits));
		sb.Append("\n家族: ").Append(string.IsNullOrWhiteSpace(dto.dynastyName) ? "-" : dto.dynastyName)
			.Append(" / ").Append(string.IsNullOrWhiteSpace(dto.familyName) ? "-" : dto.familyName);

		sb.Append("\n\n父: ").Append(FamilyName(dto.father));
		sb.Append("\n母: ").Append(FamilyName(dto.mother));
		sb.Append("\n配偶: ").Append(FamilyName(dto.spouse));
		sb.Append("\n兄弟姐妹: ");
		AppendFamilyList(sb, dto.siblings);
		sb.Append("\n子女: ");
		AppendFamilyList(sb, dto.children);

		sb.Append("\n\n文化: ").Append(string.IsNullOrEmpty(dto.cultureName) ? "-" : dto.cultureName)
			.Append("\n信仰: ").Append(string.IsNullOrEmpty(dto.faithName) ? "-" : dto.faithName);
		return sb.ToString();
	}

	private static string FamilyName(FamilyMemberDto member)
	{
		return member == null || string.IsNullOrEmpty(member.name) ? "-" : member.name + (member.isAlive ? "" : "（已故）");
	}

	private static void AppendFamilyList(StringBuilder sb, IReadOnlyList<FamilyMemberDto> members)
	{
		if (members == null || members.Count == 0)
		{
			sb.Append("-");
			return;
		}
		for (int i = 0; i < members.Count; i++)
		{
			if (i > 0)
			{
				sb.Append("、");
			}
			sb.Append(FamilyName(members[i]));
		}
	}

	private static void SetText(TMP_Text target, string value)
	{
		UiPanelBuilder.SetText(target, value);
	}

	private static TextMeshProUGUI CreateText(Transform parent, string name, float fontSize, FontStyles style, TextAlignmentOptions alignment)
	{
		return UiPanelBuilder.CreateText(parent, name, fontSize, style, alignment);
	}

	private static Button CreateButton(Transform parent, string label, Vector2 size, Vector2 anchor, Vector2 position, UnityAction onClick)
	{
		return UiPanelBuilder.CreateButton(parent, label, size, anchor, position, onClick, ButtonColor);
	}

	private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 offset)
	{
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.pivot = pivot;
		rect.anchoredPosition = anchoredPosition + offset;
		rect.sizeDelta = sizeDelta;
	}
}
}
