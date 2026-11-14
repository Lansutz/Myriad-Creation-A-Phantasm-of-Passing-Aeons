using System;
using System.Collections.Generic;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;

namespace MyriadCreation.Infrastructure.Save
{
    // FloatEntry / IntPairEntry / KnownCharacterSaveData 已迁到 MyriadCreation.Save.Contracts
    // （纯字段 DTO，见 Save/EntitySaveData.cs）。

    /// <summary>
    /// 队伍载具/成员/货物存档 DTO（SaveVersion 11 起）。
    /// 成员/货物一律拍平为原始数组（JsonUtility 对"结构体列表"支持不稳），枚举也转 int，
    /// 避免不同 Unity 版本对 struct 列表序列化的差异。
    /// </summary>
    [Serializable]
    public class TransportUnitSaveData
    {
        public TransportUnitType type;
        public UnitSubcategory subcategory = UnitSubcategory.None;
        public string unitName = "";
        public int actorId = -1;
        public int leaderCharacterId = -1;
        public int boats;
        public UnitFunding funding = new UnitFunding();

        // 成员：characterId / UnitRole
        public int[] memberCharacterIds = new int[0];
        public int[] memberRoles = new int[0];

        // 货物：goodsId / amount / unitWeight / fragile
        public int[] cargoGoodsIds = new int[0];
        public float[] cargoAmounts = new float[0];
        public float[] cargoUnitWeights = new float[0];
        public bool[] cargoFragile = new bool[0];

        public int[] animals = new int[0];
        public int[] carts = new int[0];

        public static TransportUnitSaveData FromTransport(TransportUnit t)
        {
            if (t == null) return null;
            var dto = new TransportUnitSaveData
            {
                type = t.type,
                subcategory = t.subcategory,
                unitName = t.unitName ?? "",
                actorId = t.actorId,
                leaderCharacterId = t.leaderCharacterId,
                boats = t.boats,
                funding = t.funding ?? new UnitFunding()
            };

            if (t.members != null && t.members.Count > 0)
            {
                int n = t.members.Count;
                dto.memberCharacterIds = new int[n];
                dto.memberRoles = new int[n];
                for (int i = 0; i < n; i++)
                {
                    dto.memberCharacterIds[i] = t.members[i].characterId;
                    dto.memberRoles[i] = (int)t.members[i].role;
                }
            }

            if (t.cargo != null && t.cargo.Count > 0)
            {
                int n = t.cargo.Count;
                dto.cargoGoodsIds = new int[n];
                dto.cargoAmounts = new float[n];
                dto.cargoUnitWeights = new float[n];
                dto.cargoFragile = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    dto.cargoGoodsIds[i] = t.cargo[i].goodsId;
                    dto.cargoAmounts[i] = t.cargo[i].amount;
                    dto.cargoUnitWeights[i] = t.cargo[i].unitWeight;
                    dto.cargoFragile[i] = t.cargo[i].fragile;
                }
            }

            if (t.animals != null)
            {
                dto.animals = new int[t.animals.Count];
                for (int i = 0; i < t.animals.Count; i++) dto.animals[i] = (int)t.animals[i];
            }
            if (t.carts != null)
            {
                dto.carts = new int[t.carts.Count];
                for (int i = 0; i < t.carts.Count; i++) dto.carts[i] = (int)t.carts[i];
            }
            return dto;
        }

        public TransportUnit ToTransport()
        {
            var t = new TransportUnit
            {
                type = type,
                subcategory = subcategory,
                unitName = unitName ?? "",
                actorId = actorId,
                leaderCharacterId = leaderCharacterId,
                boats = boats,
                funding = funding ?? new UnitFunding()
            };

            if (memberCharacterIds != null)
            {
                int n = memberCharacterIds.Length;
                for (int i = 0; i < n; i++)
                {
                    int role = memberRoles != null && i < memberRoles.Length ? memberRoles[i] : 0;
                    t.members.Add(new MemberSlot
                    {
                        characterId = memberCharacterIds[i],
                        role = (UnitRole)role
                    });
                }
            }

            if (cargoGoodsIds != null)
            {
                int n = cargoGoodsIds.Length;
                for (int i = 0; i < n; i++)
                {
                    t.cargo.Add(new CargoEntry
                    {
                        goodsId = cargoGoodsIds[i],
                        amount = cargoAmounts != null && i < cargoAmounts.Length ? cargoAmounts[i] : 0f,
                        unitWeight = cargoUnitWeights != null && i < cargoUnitWeights.Length ? cargoUnitWeights[i] : 0f,
                        fragile = cargoFragile != null && i < cargoFragile.Length && cargoFragile[i]
                    });
                }
            }

            if (animals != null)
                foreach (int v in animals) t.animals.Add((PackAnimalKind)v);
            if (carts != null)
                foreach (int v in carts) t.carts.Add((CartKind)v);
            return t;
        }
    }

    /// <summary>
    /// 无主地图单位存档 DTO（SaveVersion 11 起）。
    /// AI 执行器（IMapActorAI）是运行时对象，不入档；读档后按 leaderAICategory/subcategory 重建。
    /// </summary>
    [Serializable]
    public class MapActorSaveData
    {
        public int actorId;
        public MapActorType type;
        public string actorName = "";
        public UnitCategory category;
        public UnitSubcategory subcategory;

        public int currentTile;
        public int targetTile = -1;
        public int currentSettlementAnchorId = -1;
        public int targetSettlementAnchorId = -1;
        public int leaderCharacterId = -1;
        public CharacterAICategory leaderAICategory;
        public float movementProgress;
        public float moveSpeed = 1f;

        public int population;
        public float health = 100f;
        public float morale = 80f;
        public float supplies = 100f;
        public float lowHealthDays;

        public int ownerRealmId = -1;
        public int factionId = -1;
        public bool isHostile;

        public List<FloatEntry> attributes = new List<FloatEntry>();
        public TransportUnitSaveData transport;

        public static MapActorSaveData FromActor(MapActor a)
        {
            if (a == null) return null;
            var dto = new MapActorSaveData
            {
                actorId = a.actorId,
                type = a.type,
                actorName = a.actorName ?? "",
                category = a.category,
                subcategory = a.subcategory,
                currentTile = a.currentTile,
                targetTile = a.targetTile,
                currentSettlementAnchorId = a.currentSettlementAnchorId,
                targetSettlementAnchorId = a.targetSettlementAnchorId,
                leaderCharacterId = a.leaderCharacterId,
                leaderAICategory = a.leaderAICategory,
                movementProgress = a.movementProgress,
                moveSpeed = a.moveSpeed,
                population = a.population,
                health = a.health,
                morale = a.morale,
                supplies = a.supplies,
                lowHealthDays = a.lowHealthDays,
                ownerRealmId = a.ownerRealmId,
                factionId = a.factionId,
                isHostile = a.isHostile,
                transport = TransportUnitSaveData.FromTransport(a.transport)
            };
            if (a.attributes != null)
                foreach (var kv in a.attributes) dto.attributes.Add(new FloatEntry(kv.Key, kv.Value));
            return dto;
        }

        public MapActor ToActor()
        {
            var a = new MapActor
            {
                actorId = actorId,
                type = type,
                actorName = actorName ?? "",
                category = category,
                subcategory = subcategory,
                currentTile = currentTile,
                targetTile = targetTile,
                currentSettlementAnchorId = currentSettlementAnchorId,
                targetSettlementAnchorId = targetSettlementAnchorId,
                leaderCharacterId = leaderCharacterId,
                leaderAICategory = leaderAICategory,
                movementProgress = movementProgress,
                moveSpeed = moveSpeed,
                population = population,
                health = health,
                morale = morale,
                supplies = supplies,
                lowHealthDays = lowHealthDays,
                ownerRealmId = ownerRealmId,
                factionId = factionId,
                isHostile = isHostile,
                attributes = new Dictionary<string, float>(),
                transport = transport != null
                    ? transport.ToTransport()
                    : new TransportUnit { type = MapUnit.MapActorTypeToUnit(type), subcategory = subcategory }
            };
            if (attributes != null)
                foreach (var e in attributes)
                    if (!string.IsNullOrEmpty(e.key)) a.attributes[e.key] = e.value;
            return a;
        }
    }

    /// <summary>
    /// 统一地图单位视图存档 DTO（SaveVersion 11 起）。
    /// MapUnitRegistry 是运行时只读视图，读档后按存档清单恢复 unitId，避免与旧地图引用错位。
    /// </summary>
    [Serializable]
    public class MapUnitSaveData
    {
        public int unitId;
        public UnitSourceKind source;
        public int sourceId;
        public TransportUnitType type;
        public string unitName = "";
        public int leaderCharacterId = -1;
        public int ownerRealmId = -1;
        public int currentTile = -1;
        public int currentSettlementAnchorId = -1;
        public UnitCategory category;
        public UnitSubcategory subcategory;

        public static MapUnitSaveData FromUnit(MapUnit u)
        {
            if (u == null) return null;
            return new MapUnitSaveData
            {
                unitId = u.unitId,
                source = u.source,
                sourceId = u.sourceId,
                type = u.type,
                unitName = u.unitName ?? "",
                leaderCharacterId = u.leaderCharacterId,
                ownerRealmId = u.ownerRealmId,
                currentTile = u.currentTile,
                currentSettlementAnchorId = u.currentSettlementAnchorId,
                category = u.category,
                subcategory = u.subcategory
            };
        }

        public MapUnit ToUnit()
        {
            return new MapUnit
            {
                unitId = unitId,
                source = source,
                sourceId = sourceId,
                type = type,
                unitName = unitName ?? "",
                leaderCharacterId = leaderCharacterId,
                ownerRealmId = ownerRealmId,
                currentTile = currentTile,
                currentSettlementAnchorId = currentSettlementAnchorId,
                category = category,
                subcategory = subcategory,
                transport = new TransportUnit { type = type, subcategory = subcategory }
            };
        }
    }

    /// <summary>
    /// 家族存档 DTO（SaveVersion 11 起）。
    /// FamilyNode 未标记 [Serializable] 且含递归 branches / 字典传统，只保存平铺主干字段。
    /// 已知不入档：branches（分支家族树）、familyTraditions。读档后分支关系平铺为主家族。
    /// </summary>
    [Serializable]
    public class FamilySaveData
    {
        public int familyId;
        public string familyName = "";
        public int founderCharacterId;
        public int foundingYear;
        public List<int> memberIds = new List<int>();
        public float familyPrestige;
        public float familyWealth;
        public int holderRealmId = -1;
        public int homelandCountryId = -1;
        public List<string> generationMarks = new List<string>();
        public string coaPattern = "";
        public string coaColors = "";

        public static FamilySaveData FromFamily(FamilyNode f)
        {
            if (f == null) return null;
            var dto = new FamilySaveData
            {
                familyId = f.familyId,
                familyName = f.familyName ?? "",
                founderCharacterId = f.founderCharacterId,
                foundingYear = f.foundingYear,
                familyPrestige = f.familyPrestige,
                familyWealth = f.familyWealth,
                holderRealmId = f.holderRealmId,
                homelandCountryId = f.homelandCountryId,
                coaPattern = f.coaPattern ?? "",
                coaColors = f.coaColors ?? ""
            };
            if (f.memberIds != null) dto.memberIds.AddRange(f.memberIds);
            if (f.generationMarks != null) dto.generationMarks.AddRange(f.generationMarks);
            return dto;
        }

        public FamilyNode ToFamily()
        {
            var f = new FamilyNode
            {
                familyId = familyId,
                familyName = familyName ?? "",
                founderCharacterId = founderCharacterId,
                foundingYear = foundingYear,
                familyPrestige = familyPrestige,
                familyWealth = familyWealth,
                holderRealmId = holderRealmId,
                homelandCountryId = homelandCountryId,
                coaPattern = coaPattern ?? "",
                coaColors = coaColors ?? ""
            };
            if (memberIds != null) f.memberIds.AddRange(memberIds);
            if (generationMarks != null) f.generationMarks.AddRange(generationMarks);
            return f;
        }
    }

    /// <summary>
    /// 政权官职/首领存档 DTO（SaveVersion 11 起）。
    /// RealmData.officeHolders 是 [NonSerialized] 字典，RealmDTO 不包含；若不单独落盘，
    /// 读档后 Governor 等官职 holder 会永久空缺（长历史"官员蒸发"）。
    /// </summary>
    [Serializable]
    public class RealmOfficeSaveData
    {
        public int realmId;
        public int monarchId = -1;
        public int consulId = -1;
        public int stateReligionId = -1;
        public int statePatronSaintId = -1;
        public List<IntPairEntry> officeHolders = new List<IntPairEntry>();
        /// <summary>任命日（P3.1）；旧档缺省为空 = 终身任职，不误判任期到期。</summary>
        public List<IntPairEntry> officeAppointedDays = new List<IntPairEntry>();
        /// <summary>政权旗帜（P3.4）；0/空 = 无旗。</summary>
        public int bannerKind;
        public string bannerPattern = "";
        public string bannerColors = "";

        public static RealmOfficeSaveData FromRealm(RealmData r)
        {
            if (r == null) return null;
            var dto = new RealmOfficeSaveData
            {
                realmId = r.realmId,
                monarchId = r.monarchId,
                consulId = r.consulId,
                stateReligionId = r.stateReligionId,
                statePatronSaintId = r.statePatronSaintId
            };
            if (r.officeHolders != null)
                foreach (var kv in r.officeHolders) dto.officeHolders.Add(new IntPairEntry(kv.Key, kv.Value));
            if (r.officeAppointedDays != null)
                foreach (var kv in r.officeAppointedDays) dto.officeAppointedDays.Add(new IntPairEntry(kv.Key, kv.Value));
            dto.bannerKind = (int)r.bannerKind;
            dto.bannerPattern = r.bannerPattern ?? "";
            dto.bannerColors = r.bannerColors ?? "";
            return dto;
        }

        /// <summary>写回已存在的 RealmData（不新建政权，避免覆盖别的存档段）。</summary>
        public void ApplyTo(RealmData r)
        {
            if (r == null) return;
            r.monarchId = monarchId;
            r.consulId = consulId;
            r.stateReligionId = stateReligionId;
            r.statePatronSaintId = statePatronSaintId;
            if (r.officeHolders == null) r.officeHolders = new Dictionary<int, int>();
            r.officeHolders.Clear();
            if (officeHolders != null)
                foreach (var e in officeHolders) r.officeHolders[e.key] = e.value;
            if (r.officeAppointedDays == null) r.officeAppointedDays = new Dictionary<int, int>();
            r.officeAppointedDays.Clear();
            if (officeAppointedDays != null)
                foreach (var e in officeAppointedDays) r.officeAppointedDays[e.key] = e.value;
            r.bannerKind = (BannerKind)bannerKind;
            r.bannerPattern = bannerPattern ?? "";
            r.bannerColors = bannerColors ?? "";
        }
    }

    // KnownCharacterSaveData 已迁到 MyriadCreation.Save.Contracts（见 Save/EntitySaveData.cs）。
}
