
using System.Text;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.UI.Presentation
{
public static partial class ReligionPanelText
{

	/// <summary>4b4：IFaithQuery 契约入口（仅契约 DTO 字段；教统细节仍由 Legacy 旧重载负责）。</summary>
	public static string Build(IWorldReadModel view, int realmId, int statePatronSaintId)
	{
		return Build((IFaithQuery)view, realmId, statePatronSaintId);
	}

	public static string Build(IFaithQuery query, int realmId, int statePatronSaintId)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (query == null)
		{
			stringBuilder.AppendLine("=== 宗教 ===");
			stringBuilder.AppendLine("（信仰查询未注入）");
			return stringBuilder.ToString();
		}
		int faithId = query.GetStateReligionId(realmId);
		if (faithId < 0)
		{
			stringBuilder.AppendLine("=== 宗教 ===");
			stringBuilder.AppendLine("当前政权未确立国教（可在外交/政体界面选择）");
			return stringBuilder.ToString();
		}

		FaithSnapshotDto snapshot;
		bool hasSnapshot = query.TryGetFaithSnapshot(faithId, out snapshot);
		string faithName = hasSnapshot && !string.IsNullOrEmpty(snapshot.faithName)
			? snapshot.faithName : query.GetFaithName(faithId);
		float fervor = hasSnapshot ? snapshot.fervor : query.GetFervor(faithId);
		int holySites = hasSnapshot ? snapshot.holySiteCount : 0;
		if (!hasSnapshot)
		{
			var tiles = query.GetHolySiteTiles(faithId);
			holySites = tiles != null ? tiles.Count : 0;
		}

		stringBuilder.AppendLine("=== " + faithName + " ===");
		stringBuilder.AppendLine($"信仰热忱：{fervor:F0}/100");
		stringBuilder.AppendLine($"圣地：{holySites} 处");
		if (statePatronSaintId > 0)
		{
			stringBuilder.AppendLine("政权主保圣人：#" + statePatronSaintId);
		}
		return stringBuilder.ToString();
	}

	private static string PillarName(string pillar)
	{
		return pillar switch
		{
			"doctrine" => "教义", 
			"ethics" => "伦理教法", 
			"ritual" => "仪式", 
			"experience" => "体验", 
			"institution" => "组织", 
			"myth" => "神话", 
			"material" => "物质", 
			_ => pillar, 
		};
	}
}
}
