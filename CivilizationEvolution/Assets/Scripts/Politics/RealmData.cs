using CivilizationEvolution.Economy;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    [System.Serializable]
    public class RealmData
    {
        public int realmId;
        public string realmName;
 /// <summary>国教（政权选择的教统/传统——-1=无——解锁政权主保圣人）</summary>
        public int stateReligionId = -1;
 /// <summary>官职持有者（OfficialOffice→角色 id——-1=空缺——
 /// Governor 等 6 官职——GameWorld.EnsureOfficeHolders 任命——
 /// OfficeTitleCatalog 提供文化定制称号）</summary>
        [System.NonSerialized]
        public Dictionary<int, int> officeHolders = new Dictionary<int, int>();
 /// <summary>行政区划树（AdminDivisionSystem.Generate——政权=根——
 /// 分封 4 层/郡县容量 2-5——批4——每节点治理头衔/holder）</summary>
        public System.Collections.Generic.List<Culture.AdminDivision> adminDivisions
            = new System.Collections.Generic.List<Culture.AdminDivision>();
 /// <summary>政权主保圣人（CultObject id——须属国教教统内——
 /// 国家庇护/政权标识/加冕礼——国教换主保跟着换）</summary>
        public int statePatronSaintId = -1;
 // 政体：旧单标签 GovernmentType 枚举已废弃，统一由下方 composition 七维成分组合表达；
 // 粗分类（君主制/共和制）由 SupremeSuccessionLevel.IsMonarchy/IsRepublic 推导。

 // 财政
        public float treasury = 1000f;
        public float prestige = 50f;
        public float stability = 50f;
        public float centralization = 0.5f; // 集权度 0~1

 // ===== 通行管制（外交联动）=====
 /// <summary>全国默认通行管制等级</summary>
        public GameEnums.MovementControlLevel movementControl = GameEnums.MovementControlLevel.Loose;

 /// <summary>关键城镇/关隘的单独管制等级覆盖（tileIndex -> 管制等级）</summary>
        [System.NonSerialized]
        public Dictionary<int, GameEnums.MovementControlLevel> tileMovementControlOverrides = new Dictionary<int, GameEnums.MovementControlLevel>();

 /// <summary>已授予军事通行权的政权ID列表（严格管制下这些政权的军队可通过）</summary>
        [System.NonSerialized]
        public HashSet<int> militaryAccessGranted = new HashSet<int>();

 // 税收系统（每个政权独立）
        public TaxSystem taxSystem = new TaxSystem();

 // 货币系统
        public CurrencySystem currencySystem = new CurrencySystem();

 // 阶层好感度
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, float> classRelations = new Dictionary<GameEnums.SocialClass, float>();

 // 法理领土
        [System.NonSerialized]
        public HashSet<int> coreTiles = new HashSet<int>();
        [System.NonSerialized]
        public HashSet<int> claimedTiles = new HashSet<int>();

 // 附庸关系
        public int suzerainId = -1; // 宗主国ID，-1表示独立
        public List<int> vassalIds = new List<int>();

 // 政治体制成分（七维：三权力层级×交接/分配 + 央地结构）
 // 权力交接=世袭时使用 composition.successionLaw（继承法四轴+头衔+领地模式）
        public GovernmentComposition composition = new GovernmentComposition();

 // ===== 继承法双轨（借鉴《地图上发生的事》inheritance_*_from_civilization） =====

 /// <summary>政权主体文化（GetEffectiveSuccessionLaw 的文化默认查询依赖）</summary>
        public int primaryCultureId = -1;

 /// <summary>继承法是否跟随文化默认（true=按文化默认；false=国家自定 composition.successionLaw）</summary>
        public bool successionLawFromCulture = true;

 /// <summary>有效继承法：跟随文化默认时取 CultureData.defaultSuccessionLaw，否则取国家自定</summary>
        public InheritanceLaw GetEffectiveSuccessionLaw()
        {
            if (successionLawFromCulture && primaryCultureId >= 0
                && ContentRegistry.TryGetCulture(primaryCultureId, out var pack))
            {
                return pack.data.defaultSuccessionLaw;
            }
            return composition.successionLaw;
        }

 /// <summary>便捷访问：政权有效继承法</summary>
        public InheritanceLaw SuccessionLaw => GetEffectiveSuccessionLaw();

 // ===== 最高权力运行时（借鉴《地图上发生的事》monarch_id/consul_id 双轨） =====

 /// <summary>君主（A1=世袭/僭主等君主制时生效）</summary>
        public int monarchId = -1;
 /// <summary>执政官（A1=选举/委员会选举等共和制时生效）</summary>
        public int consulId = -1;
 /// <summary>继承人（按有效继承法确定的下一任）</summary>
        public int heirId = -1;
 /// <summary>元老院/议事会席位（B2=议会/元老院/长老会时生效）</summary>
        public int senateSeats = 0;

 /// <summary>当前最高权力者（君主制取君主，共和制取执政官；无则 -1）</summary>
        public int GetSupremeRulerId()
        {
            if (monarchId >= 0) return monarchId;
            return consulId;
        }

        public RealmData()
        {
            classRelations[GameEnums.SocialClass.Royalty] = 70f;
            classRelations[GameEnums.SocialClass.NobilityClergy] = 60f;
            classRelations[GameEnums.SocialClass.MerchantFreeman] = 50f;
            classRelations[GameEnums.SocialClass.Peasant] = 50f;
            classRelations[GameEnums.SocialClass.Slave] = 30f;
        }

 /// <summary>计算政权总兵力上限</summary>
        public float CalculateManpowerLimit(TileData[] tiles)
        {
            float total = 0f;
            foreach (int tileIdx in coreTiles)
            {
                if (tileIdx < 0 || tileIdx >= tiles.Length) continue;
                if (tiles[tileIdx].populationBlocks == null) continue;

                foreach (var pb in tiles[tileIdx].populationBlocks)
                {
                    if (pb.socialClass != GameEnums.SocialClass.Slave)
                        total += pb.count * 0.1f; // 10%可动员
                }
            }
            return total;
        }

 /// <summary>调整阶层好感度</summary>
        public void AdjustClassRelation(GameEnums.SocialClass socialClass, float delta)
        {
            classRelations[socialClass] = Mathf.Clamp(
                classRelations.GetValueOrDefault(socialClass, 50f) + delta, 0f, 100f);
        }

 /// <summary>计算叛乱风险</summary>
        public float CalculateRebellionRisk()
        {
            float avgRelation = 0f;
            int count = 0;
            foreach (var kv in classRelations)
            {
                avgRelation += kv.Value;
                count++;
            }
            avgRelation = count > 0 ? avgRelation / count : 50f;

            float risk = (100f - avgRelation) * 0.5f + (100f - stability) * 0.3f;
            return Mathf.Clamp(risk, 0f, 100f);
        }
    }
}
