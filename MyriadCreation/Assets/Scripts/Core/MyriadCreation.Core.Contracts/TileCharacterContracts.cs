using System.Collections.Generic;

namespace MyriadCreation.Core.Contracts
{
/// <summary>地块资源条目（UI 地块详情用，仅 Core 类型）。</summary>
public sealed class TileResourceDto
{
	public int goodsId;

	public string goodsName = "";

	public float abundance;

	public bool discovered;

	public bool developed;
}

/// <summary>地块只读详情（UI 4e；所有展示所需数据由 host 转成 Core 类型）。</summary>
public sealed class TileInfoDto
{
	public int tileIndex = -1;

	public int x;

	public int y;

	public bool exists;

	public string terrainName = "";

	public float elevation01;

	public float slopeDegree;

	public bool isLand;

	/// <summary>Core.Enums.GameEnums.BiomeType int 值。</summary>
	public int biomeId;

	public string biomeName = "";

	public float fertility;

	/// <summary>Core.Enums.GameEnums.ClimateZone int 值。</summary>
	public int climateZoneId;

	public string climateZoneName = "";

	public float annualTemp;

	public float annualPrecipMm;

	public int ownerRealmId = -1;

	public string ownerRealmName = "";

	public int provinceId = -1;

	public string provinceName = "";

	public int settlementAnchorId = -1;

	public string settlementName = "";

	public float population;

	public float development;

	public float stability;

	public IReadOnlyList<TileResourceDto> resources = new List<TileResourceDto>();
}

/// <summary>地块详情查询契约（UI 4e）。</summary>
public interface ITileInfoQuery
{
	/// <summary>地块详情；非法索引/空世界返回 null。</summary>
	TileInfoDto GetTileInfo(int tileIndex);
}

/// <summary>家族成员最小行（UI 家族树/角色详情用）。</summary>
public sealed class FamilyMemberDto
{
	public int characterId = -1;

	public string name = "";

	public int age;

	public bool isAlive;

	public string relation = "";
}

/// <summary>角色详情（UI 4e；属性/家族/官职均以 Core 类型表达）。</summary>
public sealed class CharacterDetailDto
{
	public int characterId;

	public string name = "";

	public int age;

	public bool isMale;

	public bool isAlive;

	public int realmId = -1;

	public string realmName = "";

	public int roleId;

	public string roleName = "";

	public int officeTitleId = -1;

	public string officeName = "";

	public int cultureId = -1;

	public string cultureName = "";

	public int faithId = -1;

	public string faithName = "";

	/// <summary>性格摘要（来自 CharacterData.GetPersonalityDescription；无数据为空串）。</summary>
	public string personality = "";

	/// <summary>特质名称列表（无则空列表，永不为 null）。</summary>
	public IReadOnlyList<string> traits = new List<string>();

	/// <summary>家族/王朝名（优先 FamilyNode.familyName；无家族为空串）。</summary>
	public string dynastyName = "";

	/// <summary>姓氏/家族名（优先 FamilyNode.familyName，回退 lastName）。</summary>
	public string familyName = "";

	/// <summary>家族 id；-1 表示无家族。</summary>
	public int familyId = -1;

	/// <summary>当前角色是否为统治者。</summary>
	public bool isRuler;

	/// <summary>当前角色是否为继承人。</summary>
	public bool isHeir;

	public float prowess;

	public float social;

	public float military;

	public float management;

	public float conspiracy;

	public float scholarship;

	public float prestige;

	public float health;

	public float stress;

	public float piety;

	public FamilyMemberDto father;

	public FamilyMemberDto mother;

	public FamilyMemberDto spouse;

	/// <summary>兄弟姐妹（无则空列表，永不为 null）。</summary>
	public IReadOnlyList<FamilyMemberDto> siblings = new List<FamilyMemberDto>();

	public IReadOnlyList<FamilyMemberDto> children = new List<FamilyMemberDto>();
}

/// <summary>角色详情查询契约（UI 4e）。</summary>
public interface ICharacterDetailQuery
{
	/// <summary>角色详情；未知 id/空世界返回 null。</summary>
	CharacterDetailDto GetCharacterDetail(int characterId);
}
}
