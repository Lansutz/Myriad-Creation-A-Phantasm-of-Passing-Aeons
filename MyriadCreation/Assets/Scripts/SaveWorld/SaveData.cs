


using System.Collections.Generic;
using System;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Warfare;


using MyriadCreation.Simulation.Politics;
using MyriadCreation.Core.Data;
using MyriadCreation.Save.Contracts;
using MyriadCreation.World;
using MyriadCreation.World.Generation;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.LawHost;
using MyriadCreation.Content.Definitions;
namespace MyriadCreation.Infrastructure.Save
{
    [Serializable]
    public class SaveData
    {
        public int version;
        public string saveTime;

 // 世界配置
        public int mapWidth;
        public int mapHeight;
        public int currentYear;
        public int currentDay;
        public int currentSeason;
        public int randomSeed; // 世界生成 seed（SaveVersion 16 起；旧档缺省 0 → 读档回退默认 42）
        public int playerRealmId = -1; // 玩家当前政权；旧档缺省 -1 = 未指定

        // P10.2：城市兴衰观测计数（旧档缺省 0）
        public int cityFoundedCount;
        public int cityCollapsedCount;
        public int riverAvulsionCount;
        public string lastCityDeclineCause = string.Empty;

 // 核心数据（TileData/CultureData/GoodsDef/TradeRoute 无字典，可直接序列化）
        public TileData[] tiles;
        public List<RaceDTO> races;
        public List<CultureSaveData> cultures;
        public List<RealmDTO> realms;
        public List<TradeCenterDTO> tradeCenters;
        public List<GoodsDef> goodsDefs;
        public List<DynamicGoodsSaveData> dynamicGoods;
        public List<EntityRelation> relations;
        public List<Contract> contracts;
        public string configJson; // WorldConfig(ScriptableObject)的JSON快照
        public WorldSetupOptions setupOptions; // 初始内容/自然生成选项（旧档缺失走默认）

 // 战争状态（WarState 无字典字段，可直接序列化——读档恢复战争闭环）
        public List<WarState> wars;

 // ===== 角色/家族/官职/地图单位（SaveVersion 11 起；旧档这些字段为 null，读档走默认兜底）=====
        public List<CharacterSaveData> characters;
        public List<FamilySaveData> families;
        public List<RealmOfficeSaveData> realmOffices;
        public List<MapActorSaveData> mapActors;
        public List<MapUnitSaveData> mapUnits;
        public List<KnownCharacterSaveData> knownCharacters;

 // ===== 省份/聚落/营地（SaveVersion 12 起；旧档字段为 null，读档后由地图生成结果决定）=====
        public List<ProvinceSaveData> provinces;
        public List<HabitationSiteSaveData> habitationSites;

 // ===== 军队实体（SaveVersion 13 起；地图单位视图由军队重新派生，不入档）=====
        public List<ArmySaveData> armies;

        // ===== 远征实体（P4.1；旧档为 null，读档保持空表）=====
        public List<ExpeditionSaveData> expeditions;

 // ===== 革新/科技树运行时状态（SaveVersion 14 起；革新定义本身来自内容包）=====
        public InnovationTreeSaveData innovations;

 // ===== 信仰运行态（SaveVersion 15 起；faithId/faithName/virtues/sins 等内容定义由 ReligionCatalog 重建，旧档字段为 null）=====
        public List<FaithSaveData> faiths;
        public List<MyriadCreation.Save.Contracts.DenominationSaveData> denominations;

        // ===== 编年史与头衔实例（P0.1；旧档为 null，读档保持空表/空注册表）=====
        public ChronicleSaveData chronicle;
        public List<TitleInstanceSaveData> titleInstances;

        // ===== 外交关系/从属（P0.1；旧档为 null，读档保留初始化关系）=====
        public List<DiplomaticRelation> diplomaticRelations;
        public List<Subordination> diplomaticSubordinations;

        // ===== 计划运行态（P0.1；旧档为 null，读档保持空计划表）=====
        public List<PlanSaveData> plans;

        // ===== 地图知识（S19；旧档为 null，读档保持全未知）=====
        public List<RealmKnowledgeSaveData> realmKnowledge;

        // ===== AI 长期决策状态（P0.1；旧档为 null，保留 EnsureAIControllers 的默认人格）=====
        public List<AIControllerSaveData> aiControllers;

        // ===== 角色级革新掌握/练习（P0.1；旧档为 null，读档从零掌握开始）=====
        public InnovationKnowledgeSaveData innovationKnowledge;

        // ===== 司法运行态（P0.1；旧档为 null，读档按需重建默认司法系统）=====
        public List<JudicialSystemSaveData> judicialSystems;

 // ===== 角色纽带（SaveVersion 15 起；CharacterManager 私有持有，旧档字段为 null）=====
        public List<CharacterBondSaveData> bonds;

        // ===== 海军实体（SaveVersion 17 起；旧档字段为 null）=====
        public List<NavySaveData> navies;
    }

    [System.Serializable]
    public class TitleHoldingSaveData
    {
        public int characterId = -1;
        public int institutionId = -1;
        public int role;
        public float share = 1f;
        public int sinceDay = -1;
        public int endDay = -1;
        public float legitimacy = 50f;
    }

    [System.Serializable]
    public class TitleInstanceSaveData
    {
        public string titleId;
        public string definitionId;
        public TitleHoldingSaveData[] holders;
    }

    [System.Serializable]
    public class ChronicleEntrySaveData
    {
        public int entryId;
        public int tick;
        public int year;
        public string eventType;
        public string description;
        public bool major;
        public int[] participants;
    }
}
