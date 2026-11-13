using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
/// <summary>
/// 官职表（P3.1）：内容优先（Polity/Offices.json，Mods 可覆盖），缺表时退回内置六职，
/// 保证无内容环境（测试/工具）仍能解析“谁是中央官员”。
/// </summary>
public static class OfficeCatalog
{
	private static Dictionary<int, OfficeDef> _bySlot;
	private static Dictionary<string, OfficeDef> _byId;

	public static bool IsLoaded => _bySlot != null && _bySlot.Count > 0;

	public static IReadOnlyDictionary<int, OfficeDef> All
	{
		get
		{
			EnsureDefaults();
			return _bySlot;
		}
	}

	/// <summary>内容加载入口（ContentDefinitionIndex 调用）。重复加载以最后一次为准，整表替换。</summary>
	public static void LoadFromContent(IEnumerable<OfficeDef> defs)
	{
		if (defs == null) return;
		var bySlot = new Dictionary<int, OfficeDef>();
		var byId = new Dictionary<string, OfficeDef>();
		foreach (OfficeDef def in defs)
		{
			if (def == null || def.slot < 0 || string.IsNullOrEmpty(def.id)) continue;
			bySlot[def.slot] = def;
			byId[def.id] = def;
		}
		_bySlot = bySlot;
		_byId = byId;
	}

	public static void EnsureDefaults()
	{
		if (IsLoaded) return;
		LoadFromContent(DefaultDefs());
	}

	/// <summary>清空（内容重载/测试隔离）；下一次查询自动回到内置六职。</summary>
	public static void ResetContent()
	{
		_bySlot = null;
		_byId = null;
	}

	public static bool TryGet(int slot, out OfficeDef def)
	{
		EnsureDefaults();
		def = null;
		return _bySlot != null && _bySlot.TryGetValue(slot, out def);
	}

	public static bool TryGet(string id, out OfficeDef def)
	{
		EnsureDefaults();
		def = null;
		return !string.IsNullOrEmpty(id) && _byId != null && _byId.TryGetValue(id, out def);
	}

	/// <summary>内置六职：与 <see cref="OfficialOffice"/> 的枚举顺序一致，旧档槽位可直接读。</summary>
	public static IReadOnlyList<OfficeDef> DefaultDefs()
	{
		return new[]
		{
			new OfficeDef
			{
				id = "governor", slot = 0, name = "地方长官", nameKey = "office_Governor", rank = 3,
				modules = new[] { "taxation", "judiciary" }, appointmentOrder = new[] { "ruler" },
				termYears = 5, salary = 40f, dismissalResistance = 20f,
				minTiles = 1, minPopulation = 150f, minCentralization = 0.05f
			},
			new OfficeDef
			{
				id = "district_governor", slot = 1, name = "区长官", nameKey = "office_DistrictGovernor", rank = 4,
				modules = new[] { "taxation" }, appointmentOrder = new[] { "ruler" },
				termYears = 5, salary = 25f, dismissalResistance = 15f,
				minTiles = 4, minPopulation = 300f, minCentralization = 0.1f
			},
			new OfficeDef
			{
				id = "chancellor", slot = 2, name = "宰相", nameKey = "office_Chancellor", rank = 1,
				modules = new[] { "law_proposal", "personnel" }, appointmentOrder = new[] { "ruler", "council" },
				termYears = 0, salary = 60f, dismissalResistance = 35f,
				minTiles = 6, minPopulation = 600f, minCentralization = 0.25f, minCultureStage = 2
			},
			new OfficeDef
			{
				id = "steward", slot = 3, name = "司库", nameKey = "office_Steward", rank = 2,
				modules = new[] { "taxation", "personnel" }, appointmentOrder = new[] { "ruler" },
				termYears = 0, salary = 50f, dismissalResistance = 25f,
				minTiles = 3, minPopulation = 300f, minCentralization = 0.15f
			},
			new OfficeDef
			{
				id = "marshal", slot = 4, name = "统军官", nameKey = "office_Marshal", rank = 2,
				modules = new[] { "military_command" }, appointmentOrder = new[] { "ruler" },
				termYears = 0, salary = 55f, dismissalResistance = 30f,
				minTiles = 3, minPopulation = 300f, minCentralization = 0.15f
			},
			new OfficeDef
			{
				id = "war_commander", slot = 5, name = "元帅", nameKey = "office_WarCommander", rank = 2,
				modules = new[] { "military_command" }, appointmentOrder = new[] { "ruler" },
				termYears = 0, salary = 35f, dismissalResistance = 30f,
				minTiles = 8, minPopulation = 800f, minCentralization = 0.3f
			}
		};
	}
}
}
