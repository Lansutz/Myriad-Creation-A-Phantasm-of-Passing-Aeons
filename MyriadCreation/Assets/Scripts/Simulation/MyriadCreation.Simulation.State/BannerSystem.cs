using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.State
{
/// <summary>
/// 政权旗帜类型（P3.4）。旗帜不是普遍存在，先有军旗/家旗，现代意义上的“国旗”出现很晚。
/// </summary>
public enum BannerKind
{
	/// <summary>无旗：没有君主/共同体标识（如空位、无主政体）。</summary>
	None = 0,

	/// <summary>军旗：最早、最普遍；只用于军队识别，不等于国家象征。</summary>
	MilitaryBanner = 1,

	/// <summary>家旗：统治家族有纹章习俗时才存在。</summary>
	HouseholdBanner = 2,

	/// <summary>国族旗：需要民族主义/印刷/公民权等条件，不能默认发给所有政权。</summary>
	StateFlag = 3
}

/// <summary>
/// 旗帜运行（P3.4）：按条件决定“有没有旗、哪一种”，再由 realmId + 名称确定性生成布局/配色。
/// 不生成美术资源；布局与配色是符号组合，渲染层自行解释。
/// 条件（中立表述，不绑定任何现实文明）：
/// - 无君主 → None；
/// - 已获得国族主义类革新且定居政体 → StateFlag；
/// - 统治家族已有纹章（文化开启纹章习俗）→ HouseholdBanner；
/// - 其余有君主的政体 → MilitaryBanner。
/// </summary>
public static class BannerSystem
{
	/// <summary>民族主义革新（内容表 1017）；只在内容存在且已获得时生效。</summary>
	public const int NationalismInnovationId = 1017;

	private static readonly string[] Layouts =
	{
		"plain", "twoBands", "threeBands", "cross", "saltire", "canton", "chevron"
	};

	private static readonly string[] Metals = { "#d4af37", "#e8e8e8" };

	private static readonly string[] Colours =
	{
		"#b23a3a", "#2f5fa8", "#2f7d4f", "#6b3fa0", "#1b1b1b", "#7a4a26", "#2f7a7a", "#8a2f5a"
	};

	public static BannerKind ResolveKind(RealmData realm, CharacterManager characters, InnovationTree innovations)
	{
		if (realm == null || realm.GetSupremeRulerId() < 0) return BannerKind.None;
		if (innovations != null && innovations.HasInnovation(realm.realmId, NationalismInnovationId)
			&& realm.realmForm == MyriadCreation.Simulation.Systems.RealmForm.Sedentary)
		{
			return BannerKind.StateFlag;
		}
		if (HasHouseholdArms(realm, characters)) return BannerKind.HouseholdBanner;
		return BannerKind.MilitaryBanner;
	}

	private static bool HasHouseholdArms(RealmData realm, CharacterManager characters)
	{
		if (characters == null) return false;
		CharacterData ruler = characters.GetCharacter(realm.GetSupremeRulerId());
		if (ruler == null || ruler.familyId < 0) return false;
		FamilyNode family = characters.GetFamily(ruler.familyId);
		return family != null && !string.IsNullOrEmpty(family.coaPattern) && !string.IsNullOrEmpty(family.coaColors);
	}

	/// <summary>确定性生成：同一 realmId/名称/类型 → 同一布局与配色。</summary>
	public static void Generate(int realmId, string realmName, BannerKind kind, out string pattern, out string colors)
	{
		pattern = "";
		colors = "";
		if (kind == BannerKind.None) return;
		unchecked
		{
			int hash = realmId * 73856093 ^ (realmName ?? "").GetHashCode() ^ ((int)kind * 19349663);
			int positive = hash & 0x7fffffff;
			string layout = Layouts[positive % Layouts.Length];
			pattern = kind.ToString() + "|" + layout;
			bool fieldIsMetal = ((positive / 17) & 1) == 0;
			string field = fieldIsMetal
				? Metals[(positive / 3) % Metals.Length]
				: Colours[(positive / 5) % Colours.Length];
			string charge = fieldIsMetal
				? Colours[(positive / 7) % Colours.Length]
				: Metals[(positive / 11) % Metals.Length];
			string accent = Colours[(positive / 29) % Colours.Length];
			colors = field + "|" + charge + "|" + accent;
		}
	}

	/// <summary>补齐/刷新政权旗帜；类型变化时重算，类型不变则保留已有生成结果。</summary>
	public static BannerKind Ensure(RealmData realm, CharacterManager characters, InnovationTree innovations)
	{
		if (realm == null) return BannerKind.None;
		BannerKind kind = ResolveKind(realm, characters, innovations);
		if (kind == BannerKind.None)
		{
			realm.bannerKind = BannerKind.None;
			realm.bannerPattern = "";
			realm.bannerColors = "";
			return kind;
		}
		if (realm.bannerKind != kind || string.IsNullOrEmpty(realm.bannerPattern) || string.IsNullOrEmpty(realm.bannerColors))
		{
			Generate(realm.realmId, realm.realmName, kind, out string pattern, out string colors);
			realm.bannerKind = kind;
			realm.bannerPattern = pattern;
			realm.bannerColors = colors;
		}
		return kind;
	}
}
}
