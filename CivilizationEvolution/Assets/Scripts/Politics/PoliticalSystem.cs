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
        public GameEnums.GovernmentType governmentType;

        // 财政
        public float treasury = 1000f;
        public float prestige = 50f;
        public float stability = 50f;
        public float centralization = 0.5f; // 集权度 0~1

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

        // 继承法（政治体制成分表·权力交接维度：世袭制的四轴组合，默认长子继承）
        public InheritanceLaw successionLaw = InheritanceLaw.Primogeniture();

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

        /// <summary>政体改革</summary>
        public bool ReformGovernment(int realmId, GameEnums.GovernmentType newType)
        {
            if (!_realms.TryGetValue(realmId, out var realm)) return false;

            // 政体改革触发贵族反抗
            realm.AdjustClassRelation(GameEnums.SocialClass.NobilityClergy, -30f);
            realm.governmentType = newType;
            realm.stability = Mathf.Max(0f, realm.stability - 20f);

            return true;
        }

        /// <summary>每日政治Tick</summary>
        public void DailyTick()
        {
            foreach (var realm in _realms.Values)
            {
                // 稳定值自然恢复
                realm.stability = Mathf.Lerp(realm.stability, 50f, 0.001f);

                // 阶层好感自然恢复
                var keys = new List<GameEnums.SocialClass>(realm.classRelations.Keys);
                foreach (var cls in keys)
                {
                    float current = realm.classRelations[cls];
                    realm.classRelations[cls] = Mathf.Lerp(current, 50f, 0.0005f);
                }

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
