using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.World.Anchor;
using UnityEngine;
using UnityEngine.Serialization;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.World.Settlement
{
[Serializable]
public class SettlementData : HabitationSite, ISerializationCallbackReceiver
{
	[NonSerialized]
	public AnchorData anchor;

	[SerializeField]
	[FormerlySerializedAs("settlementName")]
	private string _legacySettlementName;

	public float development;

	public float wealth;

	public float tradePower;

	public float fortification;

	public int garrison;

	public PopulationComposition composition = new PopulationComposition();

	[NonSerialized]
	public SettlementMarket market;

	public List<SettlementBlock> blocks = new List<SettlementBlock>();

	public bool isCapital;

	public bool isPort;

	public bool isCoastal;

	public bool hasMarket;

	public bool hasTemple;

	public bool hasUniversity;

	/// <summary>
	/// 人口派生快照（D40）：唯一权威是 `TileData.populationBlocks`，由
	/// `SettlementPopulationSystem` 每日按"自身地块 + 受控锚点地块"聚合写入。
	/// 运行期伤亡必须改人口块，不得只改这个快照。
	/// </summary>
	public float derivedPopulation;

	/// <summary>对外的聚落人口读点；隐藏基类标量，避免第二套人口继续被读写。</summary>
	public new float population
	{
		get
		{
			return derivedPopulation;
		}
		set
		{
			derivedPopulation = value;
		}
	}

	[SerializeField]
	[FormerlySerializedAs("settlementCategory")]
	private SettlementCategory _legacySettlementCategory;

	public float constructionProgress;

	public int constructionTier;

	public List<CityDistrict> districts = new List<CityDistrict>();

	public bool districtsGenerated;

	public Dictionary<EconomicSector, float> economicComposition = new Dictionary<EconomicSector, float>();

	public EconomicSector primarySector;

	public bool economicInitialized;

	[SerializeField]
	[FormerlySerializedAs("settlementForm")]
	private SettlementForm _legacySettlementForm;

	[SerializeField]
	[FormerlySerializedAs("settlementLevel")]
	private SettlementLevel _legacySettlementLevel;

	public SettlementFunction primaryFunction;

	public SettlementFunction secondaryFunctions;

	public CityFocus cityFocus;

	public CityForm cityForm;

	public PortTier portTier;

	public BottleneckType bottleneckType;

	public UpgradePath upgradePath;

	public EvolutionStage evolutionStage;

	/// <summary>城防等级 0–15（缓存/旧档迁移用；实际防御以城墙块为准）。</summary>
	public int fortificationLevel;

	public float settlementEvolution;

	public float settlementStability;

	[NonSerialized]
	public SettlementForm? evolutionTarget;

	public int ticksSinceLastTransition;

	public int foundingTick;

	public int parentAnchorId = -1;

	[NonSerialized]
	public float garrisonQuality;

	[NonSerialized]
	public int garrisonQuantity;

	public int ruinLevel;

	public int ruinedDay = -1;

	public SettlementLevel preRuinLevel;

	public SettlementForm preRuinForm;

	public float recoveryProgress;

	public int remnantCultureId = -1;

	public int remnantFaithId = -1;

	public int provinceId
	{
		get
		{
			if (anchor == null)
			{
				return 0;
			}
			return anchor.provinceId;
		}
	}

	public int tileIndex
	{
		get
		{
			if (anchor == null)
			{
				return -1;
			}
			return anchor.tileIndex;
		}
	}

	public float x
	{
		get
		{
			if (anchor == null)
			{
				return 0.5f;
			}
			return anchor.x;
		}
	}

	public float y
	{
		get
		{
			if (anchor == null)
			{
				return 0.5f;
			}
			return anchor.y;
		}
	}

	public int controllerAnchorId
	{
		get
		{
			if (anchor == null)
			{
				return -1;
			}
			return anchor.controllerAnchorId;
		}
	}

	public List<int> controlledAnchorIds
	{
		get
		{
			if (anchor == null)
			{
				return new List<int>();
			}
			return anchor.controlledAnchorIds;
		}
	}

	public Dictionary<int, float> controlProgress
	{
		get
		{
			if (anchor == null)
			{
				return new Dictionary<int, float>();
			}
			return anchor.controlProgress;
		}
	}

	public int influenceRadius
	{
		get
		{
			if (anchor == null)
			{
				return 1;
			}
			return anchor.influenceRadius;
		}
	}

	public bool isControlStable
	{
		get
		{
			if (anchor != null)
			{
				return anchor.isControlStable;
			}
			return false;
		}
	}

	public int bottleneckTileIndex
	{
		get
		{
			if (anchor == null)
			{
				return -1;
			}
			return anchor.bottleneckTileIndex;
		}
	}

	public string settlementName
	{
		get
		{
			return siteName;
		}
		set
		{
			siteName = value;
		}
	}

	public SettlementCategory settlementCategory
	{
		get
		{
			return _legacySettlementCategory;
		}
		set
		{
			_legacySettlementCategory = value;
			siteMode = ToSiteMode(value);
			InitializeModeData(siteMode);
		}
	}

	public SettlementForm settlementForm
	{
		get
		{
			return _legacySettlementForm;
		}
		set
		{
			_legacySettlementForm = value;
			if (siteMode == SiteMode.SedentarySettlement)
			{
				sedentarySettlementData.settlementMorphology = ((value == SettlementForm.Village) ? "village" : "city");
			}
		}
	}

	public SettlementLevel settlementLevel
	{
		get
		{
			return _legacySettlementLevel;
		}
		set
		{
			_legacySettlementLevel = value;
			if (siteMode == SiteMode.SedentarySettlement)
			{
				sedentarySettlementData.developmentStage = (int)value;
			}
		}
	}

	public bool IsRuined => ruinLevel >= 3;

	public bool IsMajorSettlement
	{
		get
		{
			if (settlementForm != SettlementForm.City)
			{
				return settlementCategory == SettlementCategory.Outpost;
			}
			return true;
		}
	}

	public bool IsDamaged
	{
		get
		{
			if (ruinLevel > 0)
			{
				return ruinLevel < 3;
			}
			return false;
		}
	}

	public string SettlementFormName => settlementForm switch
	{
		SettlementForm.Village => "村镇", 
		SettlementForm.City => "城", 
		_ => "未知", 
	};

	public SettlementMarket GetOrCreateMarket()
	{
		if (market == null)
		{
			market = new SettlementMarket(SettlementEconomy.GetWarehouseCapacity(this));
		}
		return market;
	}

	public SettlementBlock AddBlock(SettlementBlockKind kind, SettlementBlock parent = null, int level = 1, SettlementBlockSubtype subtype = SettlementBlockSubtype.None, int buildingDefId = -1, string builderName = null, int builderCharacterId = -1)
	{
		SettlementBlock settlementBlock = new SettlementBlock(kind, level, subtype)
		{
			buildingDefId = buildingDefId,
			builderCharacterId = builderCharacterId
		};
		if (parent != null)
		{
			parent.AddChild(kind, level, subtype, buildingDefId);
			SettlementBlock child = parent.children[parent.children.Count - 1];
			child.builderCharacterId = builderCharacterId;
			// 命名口径：区块名由建造者/当时统治者生成，不写死内容（SettlementBlockNaming）。
			if (string.IsNullOrEmpty(child.displayName))
			{
				child.displayName = SettlementBlockNaming.For(kind, parent.children.Count, builderName);
			}
			return child;
		}
		if (blocks == null)
		{
			blocks = new List<SettlementBlock>();
		}
		blocks.Add(settlementBlock);
		if (string.IsNullOrEmpty(settlementBlock.displayName))
		{
			settlementBlock.displayName = SettlementBlockNaming.For(kind, blocks.Count, builderName);
		}
		return settlementBlock;
	}

	public SettlementBlock GetBlock(SettlementBlockKind kind, bool includeDescendants = true)
	{
		if (blocks == null)
		{
			return null;
		}
		foreach (SettlementBlock block in blocks)
		{
			if (block == null)
			{
				continue;
			}
			if (block.kind == kind)
			{
				return block;
			}
			if (includeDescendants && block.children != null)
			{
				SettlementBlock settlementBlock = FindInChildren(block, kind);
				if (settlementBlock != null)
				{
					return settlementBlock;
				}
			}
		}
		return null;
	}

	private static SettlementBlock FindInChildren(SettlementBlock block, SettlementBlockKind kind)
	{
		if (block.children == null)
		{
			return null;
		}
		foreach (SettlementBlock child in block.children)
		{
			if (child != null)
			{
				if (child.kind == kind)
				{
					return child;
				}
				SettlementBlock settlementBlock = FindInChildren(child, kind);
				if (settlementBlock != null)
				{
					return settlementBlock;
				}
			}
		}
		return null;
	}

	public bool HasBlock(SettlementBlockKind kind)
	{
		return GetBlock(kind) != null;
	}

	public SettlementBlock GetPrimaryWall()
	{
		List<SettlementBlock> list = GetBlocks(SettlementBlockKind.Wall);
		if (list.Count == 0)
		{
			return null;
		}
		return list.Find((SettlementBlock w) => w.wallRole == WallRole.Primary) ?? list[0];
	}

	/// <summary>用现有城墙块回填 fortificationLevel（读档/旧档迁移后调用）。</summary>
	public void RecalculateFortificationLevelFromBlocks()
	{
		List<SettlementBlock> walls = GetBlocks(SettlementBlockKind.Wall);
		int max = FortificationLevels.None;
		foreach (SettlementBlock wall in walls)
		{
			if (wall != null)
			{
				max = Math.Max(max, FortificationLevels.Clamp(wall.level));
			}
		}
		fortificationLevel = max;
	}

	/// <summary>以城墙块为权威计算当前城防等级；缓存字段只作旧档/迁移兜底。</summary>
	public int GetEffectiveFortificationLevel()
	{
		int max = FortificationLevels.None;
		foreach (SettlementBlock wall in GetBlocks(SettlementBlockKind.Wall))
		{
			if (wall != null && wall.enabled)
			{
				max = Math.Max(max, FortificationLevels.Clamp(wall.level));
			}
		}
		return max > 0 ? max : FortificationLevels.Clamp(fortificationLevel);
	}

	/// <summary>拆除/夷平：关闭所有城墙块并清零缓存等级。</summary>
	public void DisableAllWalls()
	{
		foreach (SettlementBlock wall in GetBlocks(SettlementBlockKind.Wall))
		{
			if (wall != null)
			{
				wall.enabled = false;
				wall.level = FortificationLevels.None;
			}
		}
		fortificationLevel = FortificationLevels.None;
	}

	public List<SettlementBlock> GetBlocks(SettlementBlockKind kind)
	{
		List<SettlementBlock> list = new List<SettlementBlock>();
		if (blocks == null)
		{
			return list;
		}
		foreach (SettlementBlock block in blocks)
		{
			if (block != null)
			{
				if (block.kind == kind)
				{
					list.Add(block);
				}
				CollectBlocks(block, kind, list);
			}
		}
		return list;
	}

	private static void CollectBlocks(SettlementBlock block, SettlementBlockKind kind, List<SettlementBlock> result)
	{
		if (block.children == null)
		{
			return;
		}
		foreach (SettlementBlock child in block.children)
		{
			if (child != null)
			{
				if (child.kind == kind)
				{
					result.Add(child);
				}
				CollectBlocks(child, kind, result);
			}
		}
	}

	public SettlementBlock FindBlock(string blockId)
	{
		if (string.IsNullOrEmpty(blockId) || blocks == null)
		{
			return null;
		}
		foreach (SettlementBlock block in blocks)
		{
			SettlementBlock found = FindBlockIn(block, blockId);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}

	private static SettlementBlock FindBlockIn(SettlementBlock block, string blockId)
	{
		if (block == null)
		{
			return null;
		}
		if (string.Equals(block.blockId, blockId, System.StringComparison.Ordinal))
		{
			return block;
		}
		if (block.children == null)
		{
			return null;
		}
		foreach (SettlementBlock child in block.children)
		{
			SettlementBlock found = FindBlockIn(child, blockId);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}

	public SettlementBlock EnsureRootBlock(SettlementBlockKind kind, int level = 1, SettlementBlockSubtype subtype = SettlementBlockSubtype.None)
	{
		SettlementBlock block = GetBlock(kind);
		if (block != null)
		{
			return block;
		}
		return AddBlock(kind, null, level, subtype);
	}

	public void EnsureBlocksFromLegacy()
	{
		if (hasMarket)
		{
			EnsureRootBlock(SettlementBlockKind.Market);
		}
		if (hasTemple)
		{
			EnsureRootBlock(SettlementBlockKind.Temple);
		}
		if (hasUniversity)
		{
			EnsureRootBlock(SettlementBlockKind.University);
		}
		if (isPort)
		{
			EnsureRootBlock(SettlementBlockKind.Port);
		}
		if (fortificationLevel > FortificationLevels.None)
		{
			SettlementBlock wall = EnsureRootBlock(SettlementBlockKind.Wall, fortificationLevel);
			wall.level = FortificationLevels.Clamp(fortificationLevel);
			wall.wallMaterial = FortificationLevels.DefaultMaterialForLevel(wall.level);
			wall.wallMaterialGrade = FortificationLevels.GradeOf(wall.level);
			wall.NormalizeWallData();
			wall.hasSpatialData = true;
			if (wall.wallLayout == WallLayout.None)
			{
				wall.wallLayout = WallLayout.Ring;
			}
			if (wall.wallRole == WallRole.Primary && !GetBlocks(SettlementBlockKind.Wall).Exists((SettlementBlock w) => w != wall && w.wallRole == WallRole.Primary))
			{
				wall.wallRole = WallRole.Primary;
			}
			if (facilities != null)
			{
				foreach (FacilityData facility in facilities)
				{
					if (facility == null)
					{
						continue;
					}
					if (facility.facilityType == FacilityType.Trench)
					{
						wall.wallMoatLevel = Math.Max(1, wall.wallMoatLevel);
						continue;
					}
					SettlementBlockSubtype? subtype = facility.facilityType switch
					{
						FacilityType.Gate => (SettlementBlockSubtype?)SettlementBlockSubtype.Gate, 
						FacilityType.Wall => SettlementBlockSubtype.WallSegment, 
						FacilityType.Palisade => SettlementBlockSubtype.Palisade, 
						FacilityType.Earthwork => SettlementBlockSubtype.Earthwork, 
						_ => null, 
					};
					if (subtype.HasValue && !wall.children.Exists((SettlementBlock c) => c.subtype == subtype.Value))
					{
						wall.AddChild(SettlementBlockKind.Building, 1, subtype.Value);
					}
				}
			}
		}
		if (districts == null)
		{
			return;
		}
		foreach (CityDistrict district in districts)
		{
			if (district != null)
			{
				SettlementBlock settlementBlock = EnsureDistrictBlock(district);
				SettlementBlockKind? settlementBlockKind = DistrictFunctionalKind(district.districtType);
				if (settlementBlockKind.HasValue && settlementBlock.FindChild(settlementBlockKind.Value) == null)
				{
					settlementBlock.AddChild(settlementBlockKind.Value);
				}
			}
		}
	}

	private SettlementBlock EnsureDistrictBlock(CityDistrict district)
	{
		if (blocks != null)
		{
			foreach (SettlementBlock block in blocks)
			{
				if (block != null && block.kind == SettlementBlockKind.District && block.districtType == district.districtType)
				{
					return block;
				}
			}
		}
		SettlementBlock settlementBlock = AddBlock(SettlementBlockKind.District);
		settlementBlock.districtType = district.districtType;
		settlementBlock.hasSpatialData = true;
		settlementBlock.areaRatio = district.areaRatio;
		settlementBlock.populationRatio = district.populationRatio;
		settlementBlock.development = district.development;
		settlementBlock.wealth = district.wealth;
		return settlementBlock;
	}

	private static SettlementBlockKind? DistrictFunctionalKind(CityDistrictType type)
	{
		return type switch
		{
			CityDistrictType.Commercial => (SettlementBlockKind?)SettlementBlockKind.Market, 
			CityDistrictType.Artisanal => SettlementBlockKind.Workshop, 
			CityDistrictType.Port => SettlementBlockKind.Port, 
			CityDistrictType.Religious => SettlementBlockKind.Temple, 
			CityDistrictType.Cultural => SettlementBlockKind.University, 
			CityDistrictType.Military => SettlementBlockKind.Barracks, 
			CityDistrictType.Administrative => SettlementBlockKind.Administration, 
			CityDistrictType.Agricultural => SettlementBlockKind.Field, 
			CityDistrictType.Noble => SettlementBlockKind.Palace, 
			_ => null, 
		};
	}

	public void OnBeforeSerialize()
	{
		_legacySettlementName = siteName;
		if (anchor != null)
		{
			location.x = anchor.x;
			location.y = anchor.y;
		}
		_legacySettlementCategory = ToLegacyCategory(siteMode);
		SettlementFunction result;
		if (siteMode == SiteMode.SedentarySettlement && sedentarySettlementData != null)
		{
			_legacySettlementLevel = (SettlementLevel)sedentarySettlementData.developmentStage;
			_legacySettlementForm = ((!(sedentarySettlementData.settlementMorphology == "village")) ? SettlementForm.City : SettlementForm.Village);
		}
		else if (siteMode == SiteMode.FortifiedOutpost && fortifiedOutpostData != null && Enum.TryParse<SettlementFunction>(fortifiedOutpostData.purpose, out result))
		{
			primaryFunction = result;
		}
	}

	public void OnAfterDeserialize()
	{
		siteName = _legacySettlementName ?? string.Empty;
		siteMode = ToSiteMode(_legacySettlementCategory);
		InitializeModeData(siteMode);
		if (siteMode == SiteMode.SedentarySettlement)
		{
			sedentarySettlementData.settlementMorphology = ((_legacySettlementForm == SettlementForm.Village) ? "village" : "city");
			sedentarySettlementData.developmentStage = (int)_legacySettlementLevel;
		}
		else if (siteMode == SiteMode.FortifiedOutpost)
		{
			fortifiedOutpostData.purpose = primaryFunction.ToString();
		}
	}

	public void SetLegacyCategoryFromSiteMode()
	{
		_legacySettlementCategory = ToLegacyCategory(siteMode);
	}

	private static SiteMode ToSiteMode(SettlementCategory category)
	{
		return category switch
		{
			SettlementCategory.PermanentSettlement => SiteMode.SedentarySettlement, 
			SettlementCategory.Outpost => SiteMode.FortifiedOutpost, 
			SettlementCategory.Camp => SiteMode.TemporaryEncampment, 
			_ => throw new ArgumentOutOfRangeException("category", category, "Unknown legacy settlement category."), 
		};
	}

	private static SettlementCategory ToLegacyCategory(SiteMode mode)
	{
		return mode switch
		{
			SiteMode.SedentarySettlement => SettlementCategory.PermanentSettlement, 
			SiteMode.FortifiedOutpost => SettlementCategory.Outpost, 
			SiteMode.TemporaryEncampment => SettlementCategory.Camp, 
			_ => throw new ArgumentOutOfRangeException("mode", mode, "Unknown site mode."), 
		};
	}
}
}
