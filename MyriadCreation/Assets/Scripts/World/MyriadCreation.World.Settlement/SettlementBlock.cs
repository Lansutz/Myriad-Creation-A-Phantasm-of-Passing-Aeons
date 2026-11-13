using System;
using System.Collections.Generic;

namespace MyriadCreation.World.Settlement
{
[Serializable]
public class SettlementBlock
{
	public string blockId = Guid.NewGuid().ToString("D");

	public string displayName = string.Empty;

	/// <summary>
	/// 建造者（人物 id）。命名规则见 `SettlementBlockNaming`：区块名由建造者/当时统治者生成，
	/// 不写死内容。存 id 而不是只存名字，是为了人物改名/身故后仍能重建归属。
	/// </summary>
	public int builderCharacterId = -1;

	public SettlementBlockKind kind;

	public SettlementBlockSubtype subtype;

	public bool enabled = true;

	public int level = 1;

	public float condition = 1f;

	public bool hasSpatialData;

	public float areaRatio;

	public float populationRatio;

	public float development;

	public float wealth;

	public float length;

	public int wallLayerCount = 1;

	public WallLayout wallLayout;

	public int wallMoatLevel;

	public WallRole wallRole;

	/// <summary>城墙材质；非城墙块忽略。</summary>
	public WallMaterial wallMaterial = WallMaterial.Earth;

	/// <summary>同一种材质的工艺档位 1..3；非城墙块忽略。</summary>
	public int wallMaterialGrade = 1;

	/// <summary>横向段（沿墙长方向切分）；非城墙块忽略。</summary>
	public List<WallSegment> wallSegments = new List<WallSegment>();

	public CityDistrictType districtType;

	public int buildingDefId = -1;

	public List<SettlementBlock> children = new List<SettlementBlock>();

	public SettlementBlock()
	{
	}

	public SettlementBlock(SettlementBlockKind kind, int level = 1, SettlementBlockSubtype subtype = SettlementBlockSubtype.None)
	{
		this.kind = kind;
		this.level = level;
		this.subtype = subtype;
	}

	public SettlementBlock AddChild(SettlementBlockKind childKind, int childLevel = 1, SettlementBlockSubtype childSubtype = SettlementBlockSubtype.None, int childBuildingDefId = -1)
	{
		SettlementBlock settlementBlock = new SettlementBlock(childKind, childLevel, childSubtype)
		{
			buildingDefId = childBuildingDefId
		};
		if (children == null)
		{
			children = new List<SettlementBlock>();
		}
		children.Add(settlementBlock);
		return settlementBlock;
	}

	public SettlementBlock FindChild(SettlementBlockKind childKind)
	{
		if (children == null)
		{
			return null;
		}
		foreach (SettlementBlock child in children)
		{
			if (child != null && child.kind == childKind)
			{
				return child;
			}
		}
		return null;
	}

	/// <summary>城墙字段自愈：层数 1..3、工艺 1..3、段列表非空引用。</summary>
	public void NormalizeWallData()
	{
		if (kind != SettlementBlockKind.Wall)
		{
			return;
		}
		wallLayerCount = System.Math.Min(3, System.Math.Max(1, wallLayerCount));
		wallMaterialGrade = FortificationLevels.ClampMaterialGrade(wallMaterialGrade);
		wallSegments ??= new List<WallSegment>();
	}
}
}
