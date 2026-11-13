using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Simulation.State;
using MyriadCreation.World.Biome;
using UnityEngine;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Content.Definitions;
using Random = System.Random;

using MyriadCreation.Content.Modding;
namespace MyriadCreation.Simulation.ContentHost
{
public static class ContentRegistry
{
	[Serializable]
	public class NamePoolData
	{
		public List<string> maleNames = new List<string>();

		public List<string> femaleNames = new List<string>();

		public List<string> lastNames = new List<string>();

		public List<string> cityNames = new List<string>();
	}

	public class CultureContentPack
	{
		public CultureData data;

		public NamePoolData names = new NamePoolData();

		public string packagePath = "";
	}

	public static Dictionary<int, CultureContentPack> Cultures { get; private set; } = new Dictionary<int, CultureContentPack>();

	public static Dictionary<int, RaceData> Races { get; private set; } = new Dictionary<int, RaceData>();

	public static Dictionary<string, EthosDef> Ethos { get; private set; } = new Dictionary<string, EthosDef>();

	public static Dictionary<string, HeritageDef> Heritages { get; private set; } = new Dictionary<string, HeritageDef>();

	public static Dictionary<string, TraditionDef> Traditions { get; private set; } = new Dictionary<string, TraditionDef>();

	public static Dictionary<string, LanguageDef> Languages { get; private set; } = new Dictionary<string, LanguageDef>();

	public static Dictionary<string, EthnicGroupDef> EthnicGroups { get; private set; } = new Dictionary<string, EthnicGroupDef>();

	public static Dictionary<string, FamilyTraditionDef> FamilyTraditions { get; private set; } = new Dictionary<string, FamilyTraditionDef>();

	public static Dictionary<string, TitleDef> Titles { get; private set; } = new Dictionary<string, TitleDef>();

	public static Dictionary<string, CharacterTemplateDef> CharacterTemplates { get; private set; } = new Dictionary<string, CharacterTemplateDef>();

	public static Dictionary<string, TalentDefectDef> TalentDefects { get; private set; } = new Dictionary<string, TalentDefectDef>();

	public static Dictionary<string, MentalDisorderDef> MentalDisorders { get; private set; } = new Dictionary<string, MentalDisorderDef>();

	public static Dictionary<int, InnovationDef> Innovations { get; private set; } = new Dictionary<int, InnovationDef>();

	public static Dictionary<int, ReligionDef> Religions { get; private set; } = new Dictionary<int, ReligionDef>();

	public static Dictionary<int, EquipmentDef> Equipment { get; private set; } = new Dictionary<int, EquipmentDef>();

	public static Dictionary<string, DoctrineOptionDef> Doctrines { get; private set; } = new Dictionary<string, DoctrineOptionDef>();

	public static Dictionary<int, BiomeDef> Biomes => BiomeRegistry.Overrides;

	public static bool IsInitialized { get; private set; }

	private static ContentDefinitionIndex _definitions;

	static ContentRegistry() { Initialize(); }

	public static void Initialize()
	{
		if (!IsInitialized)
		{
			Cultures = new Dictionary<int, CultureContentPack>();
			Races = new Dictionary<int, RaceData>();
			BiomeRegistry.Overrides.Clear();
			string streamingAssetsPath = Application.streamingAssetsPath;
			_definitions = new ContentDefinitionIndex();
			if (!Directory.Exists(streamingAssetsPath))
			{
				Debug.LogWarning((object)("[ContentRegistry] 未找到 StreamingAssets 目录：" + streamingAssetsPath));
			}
			else
			{
				// P2：运行时映射（Culture/Race/Biome）留 host；纯定义 JSON 解析委托 ContentDefinitionIndex。
				ContentRuntime.LoadRuntime(streamingAssetsPath, Cultures, Races, Biomes);
				_definitions.Initialize(streamingAssetsPath, new ContentDefinitionIndex.InnovationFallbackGroups(
					DiscoveryGroups.Merchant, DiscoveryGroups.Scholar, DiscoveryGroups.Artisan));
			}
			Ethos = _definitions.Ethos;
			Heritages = _definitions.Heritages;
			Traditions = _definitions.Traditions;
			Languages = _definitions.Languages;
			EthnicGroups = _definitions.EthnicGroups;
			FamilyTraditions = _definitions.FamilyTraditions;
			CharacterTemplates = _definitions.CharacterTemplates;
			TalentDefects = _definitions.TalentDefects;
			MentalDisorders = _definitions.MentalDisorders;
			Innovations = _definitions.Innovations;
			Religions = _definitions.Religions;
			Doctrines = _definitions.Doctrines;
			Titles = _definitions.Titles;
			MyriadCreation.Simulation.Systems.TitleManager.LoadFromContent();
			Equipment = _definitions.Equipment;
			LanguageFamilyCatalog.Initialize(streamingAssetsPath);
			PropertyDefinitionCatalog.Initialize(streamingAssetsPath);
			GoodsPropertyCatalog.Initialize(streamingAssetsPath);
			RecipeCatalog.Initialize(streamingAssetsPath);
			IsInitialized = true;
		}
	}

	public static void Reset()
	{
		IsInitialized = false;
		Cultures.Clear();
		Races.Clear();
		Ethos.Clear();
		Heritages.Clear();
		Traditions.Clear();
		Languages.Clear();
		EthnicGroups.Clear();
		FamilyTraditions.Clear();
		CharacterTemplates.Clear();
		TalentDefects.Clear();
		MentalDisorders.Clear();
		Innovations.Clear();
		Religions.Clear();
		Doctrines.Clear();
		Titles.Clear();
		Equipment.Clear();
		_definitions?.Reset();
		_definitions = null;
		Biomes.Clear();
		LanguageFamilyCatalog.Reset();
		PropertyDefinitionCatalog.Reset();
		GoodsPropertyCatalog.Reset();
		RecipeCatalog.Reset();
		// Reset 之后立即重建默认内容，避免测试/调用方在 Reset 与 Initialize 之间看到空表。
		Initialize();
	}

	public static bool TryGetCulture(int id, out CultureContentPack pack)
	{
		return Cultures.TryGetValue(id, out pack);
	}

	public static bool TryGetRace(int id, out RaceData race)
	{
		return Races.TryGetValue(id, out race);
	}

	public static bool TryGetEthos(string id, out EthosDef def)
	{
		return Ethos.TryGetValue(id, out def);
	}

	public static bool TryGetHeritage(string id, out HeritageDef def)
	{
		return Heritages.TryGetValue(id, out def);
	}

	public static bool TryGetTradition(string id, out TraditionDef def)
	{
		return Traditions.TryGetValue(id, out def);
	}

	public static bool TryGetLanguage(string id, out LanguageDef def)
	{
		return Languages.TryGetValue(id, out def);
	}

	public static bool TryGetEthnicGroup(string id, out EthnicGroupDef def)
	{
		return EthnicGroups.TryGetValue(id, out def);
	}

	public static bool TryGetFamilyTradition(string id, out FamilyTraditionDef def)
	{
		return FamilyTraditions.TryGetValue(id, out def);
	}

	public static bool TryGetCharacterTemplate(string id, out CharacterTemplateDef def)
	{
		return CharacterTemplates.TryGetValue(id, out def);
	}

	public static bool TryGetTalentDefect(string id, out TalentDefectDef def)
	{
		return TalentDefects.TryGetValue(id, out def);
	}

	public static bool TryGetMentalDisorder(string id, out MentalDisorderDef def)
	{
		return MentalDisorders.TryGetValue(id, out def);
	}

	public static bool TryGetInnovation(int id, out InnovationDef def)
	{
		return Innovations.TryGetValue(id, out def);
	}

	public static bool TryGetReligion(int id, out ReligionDef def)
	{
		return Religions.TryGetValue(id, out def);
	}

	public static bool TryGetDoctrine(string id, out DoctrineOptionDef def)
	{
		return Doctrines.TryGetValue(id, out def);
	}

	public static bool TryGetTitle(string id, out TitleDef def)
	{
		return Titles.TryGetValue(id, out def);
	}

	public static bool TryGetEquipment(int id, out EquipmentDef def)
	{
		return Equipment.TryGetValue(id, out def);
	}

	public static bool TryGetBiome(int id, out BiomeDef def)
	{
		return Biomes.TryGetValue(id, out def);
	}

	public static string GetRandomName(CultureContentPack pack, int type, Random rng = null)
	{
		// 确定性优先：未传 rng 时固定种子（原 new Random() 用时间种子 ⇒ 同 seed 不同结果）。
		// TODO(M1)：调用方传世界 seed 派生的 rng。
		rng = rng ?? new Random(0);
		List<string> list = null;
		if (pack != null && pack.data != null && !string.IsNullOrEmpty(pack.data.languageId) && TryGetLanguage(pack.data.languageId, out var def))
		{
			list = type switch
			{
				1 => def.femaleNames, 
				2 => def.familyNames, 
				3 => def.cityNames, 
				_ => def.maleNames, 
			};
		}
		if (list == null || list.Count == 0)
		{
			if (pack == null)
			{
				return "无名";
			}
			list = type switch
			{
				1 => pack.names.femaleNames, 
				2 => pack.names.lastNames, 
				3 => pack.names.cityNames, 
				_ => pack.names.maleNames, 
			};
			if (list.Count == 0 && type != 2)
			{
				list = pack.names.lastNames;
			}
			if (list.Count == 0)
			{
				return pack.data.cultureName;
			}
		}
		return list[rng.Next(list.Count)];
	}
}
}
