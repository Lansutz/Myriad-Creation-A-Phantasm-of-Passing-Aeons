using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;

namespace CivilizationEvolution.Politics
{
    /// <summary>政权数据</summary>
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
        public Dictionary<int, int> officeHolders = new Dictionary<int, int>();
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
        public Dictionary<int, GameEnums.MovementControlLevel> tileMovementControlOverrides = new Dictionary<int, GameEnums.MovementControlLevel>();

        /// <summary>已授予军事通行权的政权ID列表（严格管制下这些政权的军队可通过）</summary>
        public HashSet<int> militaryAccessGranted = new HashSet<int>();

        // 税收系统（每个政权独立）
        public TaxSystem taxSystem = new TaxSystem();

        // 货币系统
        public CurrencySystem currencySystem = new CurrencySystem();

        // 阶层好感度
        public Dictionary<GameEnums.SocialClass, float> classRelations = new Dictionary<GameEnums.SocialClass, float>();

        // 法理领土
        public HashSet<int> coreTiles = new HashSet<int>();
        public HashSet<int> claimedTiles = new HashSet<int>();

        // 附庸关系
        public int suzerainId = -1; // 宗主国ID，-1表示独立
        public List<int> vassalIds = new List<int>();

        // 政治体制成分（用户定稿七维：三权力层级×交接/分配 + 央地结构）
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

    /// <summary>
    /// 政治管理器
    /// 处理法理、占领、政体、阶层治理
    /// </summary>
    public class PoliticalManager
    {
        private readonly TileData[] _tiles;
        private readonly Dictionary<int, RealmData> _realms;

        public PoliticalManager(TileData[] tiles, Dictionary<int, RealmData> realms)
        {
            _tiles = tiles;
            _realms = realms;
        }

        /// <summary>占领地块</summary>
        public bool OccupyTile(int tileIndex, int realmId)
        {
            if (tileIndex < 0 || tileIndex >= _tiles.Length) return false;
            if (!_realms.ContainsKey(realmId)) return false;

            _tiles[tileIndex].occupyingRealmId = realmId;

            // 占领后稳定值下降
            _tiles[tileIndex].stability = Mathf.Max(0f, _tiles[tileIndex].stability - 20f);
            _tiles[tileIndex].order = Mathf.Max(0f, _tiles[tileIndex].order - 15f);

            return true;
        }

        /// <summary>割让地块（法理转移）</summary>
        public bool CedeTile(int tileIndex, int fromRealmId, int toRealmId)
        {
            if (tileIndex < 0 || tileIndex >= _tiles.Length) return false;
            if (_tiles[tileIndex].ownerRealmId != fromRealmId) return false;

            _tiles[tileIndex].ownerRealmId = toRealmId;
            _tiles[tileIndex].occupyingRealmId = -1;

            if (_realms.TryGetValue(fromRealmId, out var fromRealm))
                fromRealm.coreTiles.Remove(tileIndex);
            if (_realms.TryGetValue(toRealmId, out var toRealm))
                toRealm.claimedTiles.Add(tileIndex);

            return true;
        }

        /// <summary>计算地块控制度</summary>
        public float CalculateControlDegree(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= _tiles.Length) return 0f;
            ref TileData tile = ref _tiles[tileIndex];

            // 基础控制度由稳定值和秩序决定
            float control = (tile.stability + tile.order) / 200f;

            // 占领地控制度降低
            if (tile.occupyingRealmId != -1 && tile.occupyingRealmId != tile.ownerRealmId)
                control *= 0.5f;

            // 有驻军时控制度提升（简化：假设buildingLevels[3]是城防）
            control += tile.buildingLevels[3] * 0.05f;

            return Mathf.Clamp01(control);
        }

        /// <summary>
        /// 政体改革已统一到七维成分模型——见 GovernmentReform.Reform（按 PolityDimension 改 composition，
        /// 需支撑革新已持有，触发稳定性下降与编年史）。旧的单标签 GovernmentType 枚举与本方法已废弃。
        /// </summary>

        /// <summary>每日政治Tick</summary>
        public void DailyTick()
        {
            foreach (var realm in _realms.Values)
            {
                // 稳定值自然恢复
                realm.stability = Mathf.Lerp(realm.stability, 50f, 0.001f);

                // 阶层好感不再机械回归中性值——由 SocietyManager 按各阶层需求满足度驱动：
                // ClassNeedsSystem 评估多维需求 → ApplyClassRelations 平滑趋近满足度（见 GameWorld 政治Tick）。

                // 叛乱风险检测
                if (realm.CalculateRebellionRisk() > 70f && Random.value < 0.01f)
                {
                    // 触发叛乱事件
                    Debug.Log($"[Politics] {realm.realmName} 爆发叛乱！");
                }
            }
        }
    }
}
