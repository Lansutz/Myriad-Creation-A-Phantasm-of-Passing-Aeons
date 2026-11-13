using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.UI.Presentation
{
public static partial class SettlementPeopleText
{
	public static int ResolveObserverRealmId(int playerRealmId, int viewRealmId)
	{
		if (playerRealmId < 0)
		{
			return viewRealmId;
		}
		return playerRealmId;
	}

	/// <summary>4b4：ISettlementPeopleQuery.KnownAt 的契约入口。</summary>
	public static IReadOnlyList<CharacterRowDto> KnownAt(IWorldReadModel view, int anchorId, int observerRealmId)
	{
		return view != null ? view.GetKnownCharactersAtAnchor(anchorId, observerRealmId) : null;
	}

	/// <summary>4b4：ISettlementPeopleQuery.PublicFigures 的契约入口。</summary>
	public static IReadOnlyList<CharacterRowDto> PublicFiguresAt(IWorldReadModel view, int anchorId)
	{
		return view != null ? view.GetPublicFiguresAtAnchor(anchorId) : null;
	}

	/// <summary>4b4：CharacterRowDto 版本（不依赖 CharacterData/CharacterLocationService）。</summary>
	public static string Describe(IWorldReadModel view, CharacterRowDto character)
	{
		if (character == null)
		{
			return "";
		}
		string text = character.name + "｜" + RoleLabel((CharacterRole)character.role);
		if ((character.flags & CharacterRowDto.FlagRuler) != 0)
		{
			text += "·领袖";
		}
		if ((character.flags & CharacterRowDto.FlagPublicFigure) != 0)
		{
			text += "·公共人物";
		}
		return text;
	}

	public static string RoleLabel(CharacterRole role)
	{
		return role switch
		{
			CharacterRole.Noble => "贵族", 
			CharacterRole.Clergy => "神职", 
			CharacterRole.Merchant => "商人", 
			CharacterRole.Military => "军人", 
			CharacterRole.Scholar => "学者", 
			CharacterRole.Ruler => "统治者", 
			CharacterRole.Heir => "继承人", 
			CharacterRole.Spouse => "配偶", 
			CharacterRole.Courtier => "廷臣", 
			CharacterRole.Courier => "使者", 
			_ => "平民", 
		};
	}

	/// <summary>
	/// B8-4b2：IWorldReadModel 重载——按地块查聚落锚点（只查聚落；营地需走旧 GameWorld 重载兜底）。
	/// </summary>
	public static bool TryFindSettlementAnchorAtTile(IWorldReadModel view, int tileIndex, out int anchorId, out string siteName)
	{
		anchorId = -1;
		siteName = "";
		if (view == null || tileIndex < 0)
		{
			return false;
		}
		IReadOnlyList<SettlementSnapshotDto> near = view.GetSettlementsNearTile(tileIndex, 0);
		if (near == null)
		{
			return false;
		}
		foreach (SettlementSnapshotDto settlement in near)
		{
			if (settlement != null && settlement.tileIndex == tileIndex && settlement.anchorId >= 0)
			{
				anchorId = settlement.anchorId;
				siteName = settlement.settlementName ?? "";
				return true;
			}
		}
		return false;
	}
}
}
