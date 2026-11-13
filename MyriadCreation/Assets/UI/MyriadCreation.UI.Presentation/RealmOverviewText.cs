using System.Collections.Generic;
using System.Text;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.UI.Presentation
{
public static partial class RealmOverviewText
{
	/// <summary>
	/// B8-4b2：IWorldReadModel 重载（只读 DTO，不引用 GameWorld/RealmData）。
	/// 与旧重载在"政权基础字段 + 官职 + 行政区划"范围内保持一致；法律/宗教/社会细节仍由旧重载负责（待 4b 后续契约）。
	/// </summary>
	public static string Build(IWorldReadModel view, int realmId, IReadOnlyDictionary<int, string> officeDisplay = null, bool isPlayerRealm = false, int? selectedDivisionId = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (view == null)
		{
			stringBuilder.AppendLine("=== 政权总览 ===");
			stringBuilder.AppendLine("（未选中政权——点击地图上的政权查看）");
			return stringBuilder.ToString();
		}
		RealmSnapshotDto realm = view.GetRealm(realmId);
		if (realm == null)
		{
			stringBuilder.AppendLine("=== 政权总览 ===");
			stringBuilder.AppendLine("（未选中政权——点击地图上的政权查看）");
			return stringBuilder.ToString();
		}

		string text = (isPlayerRealm ? "（本家）" : "");
		stringBuilder.AppendLine("=== " + realm.realmName + text + " ===");
		stringBuilder.AppendLine($"人口：{realm.population:N0} 人");
		stringBuilder.AppendLine($"国库：{realm.treasury:F0} | 稳定：{realm.stability:F0} | 集权：{realm.centralization:F2}");
		if (officeDisplay != null && officeDisplay.Count > 0)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("── 官职体系 ──");
			foreach (KeyValuePair<int, string> item in officeDisplay)
			{
				stringBuilder.AppendLine(item.Value);
			}
		}
		IReadOnlyList<AdminDivisionNodeDto> divisions = view.GetAdminDivisions(realmId);
		if (divisions != null && divisions.Count > 1)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("── 行政区划 ──");
			AppendTree(stringBuilder, divisions, realmId, -1, selectedDivisionId);
		}
		return stringBuilder.ToString();
	}

	private static void AppendTree(StringBuilder sb, IReadOnlyList<AdminDivisionNodeDto> all, int realmId, int parentId, int? selected)
	{
		foreach (AdminDivisionNodeDto item in all)
		{
			if (item.realmId == realmId && item.parentId == parentId)
			{
				string text = ((selected.HasValue && selected.Value == item.divisionId) ? "▶ " : "  ");
				string text2 = (string.IsNullOrEmpty(item.titleId) ? "" : ("（" + item.titleId + "）"));
				sb.AppendLine(text + new string('　', item.level - 1) + item.name + text2);
				AppendTree(sb, all, realmId, item.divisionId, selected);
			}
		}
	}
}
}
