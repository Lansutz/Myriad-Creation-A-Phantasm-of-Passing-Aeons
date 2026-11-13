using System;
using System.Collections.Generic;
using System.Text;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.UI.Presentation
{
public static partial class ClassPanelText
{
	private const string C_HEADER = "<color=#E8D5B7>";

	private const string C_GOOD = "<color=#7CB342>";

	private const string C_WARN = "<color=#FFB300>";

	private const string C_BAD = "<color=#E53935>";

	private const string C_DIM = "<color=#888888>";

	private const string C_VALUE = "<color=#FFFFFF>";

	private const string C_END = "</color>";

	private const int BAR_WIDTH = 10;

	public static string GetDimensionName(ClassNeedDimension d)
	{
		return d switch
		{
			ClassNeedDimension.Subsistence => "生存保障", 
			ClassNeedDimension.Security => "人身安全", 
			ClassNeedDimension.TaxBurden => "税负合理", 
			ClassNeedDimension.PoliticalAccess => "政治通道", 
			ClassNeedDimension.EconomicOpportunity => "经济机会", 
			ClassNeedDimension.InstitutionalRecognition => "制度承认", 
			ClassNeedDimension.Legitimacy => "合法秩序", 
			ClassNeedDimension.Privilege => "特权保障", 
			_ => d.ToString(), 
		};
	}

	private static string ScoreColor(float score)
	{
		if (score < 45f)
		{
			return "<color=#E53935>";
		}
		if (score < 65f)
		{
			return "<color=#FFB300>";
		}
		return "<color=#7CB342>";
	}

	private static string Bar(float score)
	{
		int num = Math.Clamp((int)Math.Round(score / 100f * 10f), 0, 10);
		string text = new string('█', num) + new string('░', 10 - num);
		return ScoreColor(score) + text + "</color>";
	}

	// ===== 4b4 契约重载：只依赖 ISocietyQuery/SocietySnapshotDto =====

	public static string Build(IWorldReadModel view, int realmId)
	{
		return Build((ISocietyQuery)view, realmId);
	}

	public static string Build(ISocietyQuery query, int realmId)
	{
		return Build(query?.GetSociety(realmId));
	}

	public static string Build(SocietySnapshotDto society)
	{
		if (society == null || society.classes == null || society.classes.Count == 0)
		{
			return "<color=#888888>（无社会画像数据）</color>\n";
		}

		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<color=#E8D5B7>── 社会概览 ──</color>");
		stringBuilder.AppendLine(
			$"整体动荡 {ScoreColor(society.unrestScore)}{society.unrestScore:F0}</color>"
			+ $" | 总人口 <color=#FFFFFF>{society.totalPopulation:N0}</color>"
			+ $" | 主导阶层 <color=#FFFFFF>{society.dominantClassName}</color>"
			+ $" | 最动荡 {ScoreColor(70f)}{society.mostRestlessClassName}</color>");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("<color=#E8D5B7>── 阶层详情 ──</color>");

		List<SocialClassSnapshotDto> list = new List<SocialClassSnapshotDto>(society.classes);
		list.Sort((a, b) => b.populationShare.CompareTo(a.populationShare));
		foreach (SocialClassSnapshotDto item in list)
		{
			if (IsVisibleClass(item))
			{
				stringBuilder.Append(BuildClassDetail(item));
			}
		}
		return stringBuilder.ToString();
	}

	private static bool IsVisibleClass(SocialClassSnapshotDto item)
	{
		if (item == null)
		{
			return false;
		}
		return item.population != 0f || item.populationShare != 0f
			|| item.satisfaction != 0f || item.loyalty != 0f || item.unrest != 0f
			|| !string.IsNullOrEmpty(item.chiefGrievanceReason)
			|| (item.needScores != null && item.needScores.Count > 0);
	}

	private static string BuildClassDetail(SocialClassSnapshotDto item)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = string.IsNullOrEmpty(item.className) ? item.classId.ToString() : item.className;
		stringBuilder.AppendLine("<color=#E8D5B7>▶ " + text + "</color>"
			+ $"  人口 <color=#FFFFFF>{item.population:N0}</color>（<color=#FFFFFF>{item.populationShare * 100f:F1}%</color>）");
		stringBuilder.AppendLine(
			$"  满足 {ScoreColor(item.satisfaction)}{item.satisfaction:F0}</color>"
			+ $" | 忠诚 {ScoreColor(item.loyalty)}{item.loyalty:F0}</color>"
			+ $" | 组织 <color=#FFFFFF>{item.organization:F2}</color>"
			+ $" | 影响 <color=#FFFFFF>{item.influence:F0}</color>"
			+ $" | 动荡 {ScoreColor(100f - item.unrest)}{item.unrest:F0}</color>"
			+ $" | 支持 {ScoreColor(item.support)}{item.support:F0}</color>");
		if (!string.IsNullOrEmpty(item.chiefGrievanceReason) || item.chiefGrievanceId != (int)ClassNeedDimension.Subsistence)
		{
			string grievance = item.chiefGrievanceId != (int)ClassNeedDimension.Subsistence
				? GetDimensionName((ClassNeedDimension)item.chiefGrievanceId) : "未知";
			string reason = string.IsNullOrEmpty(item.chiefGrievanceReason) ? "" : " — " + item.chiefGrievanceReason;
			stringBuilder.AppendLine("  <color=#E53935>主要不满：" + grievance + reason + "</color>");
		}
		if (item.needScores != null && item.needScores.Count > 0)
		{
			stringBuilder.AppendLine("  <color=#888888>── 需求维度 ──</color>");
			List<ClassNeedScoreDto> list = new List<ClassNeedScoreDto>(item.needScores);
			list.Sort((a, b) => a.dimensionId.CompareTo(b.dimensionId));
			for (int i = 0; i < list.Count; i += 2)
			{
				string left = NeedBar(list[i]);
				string right = i + 1 < list.Count ? NeedBar(list[i + 1]) : "";
				stringBuilder.AppendLine("  " + left + "    " + right);
			}
		}
		stringBuilder.AppendLine();
		return stringBuilder.ToString();
	}

	private static string NeedBar(ClassNeedScoreDto score)
	{
		string dimensionName = string.IsNullOrEmpty(score.dimensionName)
			? GetDimensionName((ClassNeedDimension)score.dimensionId) : score.dimensionName;
		if (score.score <= 0f)
		{
			return $"<color=#888888>{dimensionName,-6} {new string('░', 10)}  --</color>";
		}
		return $"{dimensionName,-4} {Bar(score.score)} {ScoreColor(score.score)}{score.score:F0}</color>";
	}
}
}
