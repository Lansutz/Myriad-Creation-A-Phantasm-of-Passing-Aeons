using System.Collections.Generic;
using System;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;

namespace CivilizationEvolution.Politics
{
 /// <summary>政权数据</summary>

 /// 政治管理器 /// 处理法理、占领、政体、阶层治理    public class PoliticalManager
    {
        private readonly TileData[] _tiles;
        private readonly Dictionary<int, RealmData> _realms;

        public PoliticalManager(TileData[] tiles, Dictionary<int, RealmData> realms)
        {
            _tiles = tiles;
            _realms = realms;
        }

 /// <summary>占领地块</summary>        public bool OccupyTile(int tileIndex, int realmId)
        {
            if (tileIndex < 0 || tileIndex >= _tiles.Length) return false;
            if (!_realms.ContainsKey(realmId)) return false;

            _tiles[tileIndex].occupyingRealmId = realmId;

 // 占领后稳定值下降            _tiles[tileIndex].stability = Mathf.Max(0f, _tiles[tileIndex].stability - 20f);
            _tiles[tileIndex].order = Mathf.Max(0f, _tiles[tileIndex].order - 15f);

            return true;
        }

 /// <summary>割让地块（法理转移）</summary>        public bool CedeTile(int tileIndex, int fromRealmId, int toRealmId)
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

 /// <summary>计算地块控制度</summary>        public float CalculateControlDegree(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= _tiles.Length) return 0f;
            ref TileData tile = ref _tiles[tileIndex];

 // 基础控制度由稳定值和秩序决定            float control = (tile.stability + tile.order) / 200f;

 // 占领地控制度降低            if (tile.occupyingRealmId != -1 && tile.occupyingRealmId != tile.ownerRealmId)
                control *= 0.5f;

 // 有驻军时控制度提升（简化：假设buildingLevels[3]是城防）            control += tile.buildingLevels[3] * 0.05f;

            return Mathf.Clamp01(control);
        }

 /// 政体改革已统一到七维成分模型——见 GovernmentReform.Reform（按 PolityDimension 改 composition， /// 需支撑革新已持有，触发稳定性下降与编年史）。旧的单标签 GovernmentType 枚举与本方法已废弃。
 /// <summary>每日政治Tick</summary>        public void DailyTick()
        {
            foreach (var realm in _realms.Values)
            {
 // 稳定值自然恢复                realm.stability = Mathf.Lerp(realm.stability, 50f, 0.001f);

 // 阶层好感不再机械回归中性值——由 SocietyManager 按各阶层需求满足度驱动： // ClassNeedsSystem 评估多维需求 → ApplyClassRelations 平滑趋近满足度（见 GameWorld 政治Tick）。
 // 叛乱风险检测                if (realm.CalculateRebellionRisk() > 70f && UnityEngine.Random.value < 0.01f)
                {
 // 触发叛乱事件                    Debug.Log($"[Politics] {realm.realmName} 爆发叛乱！");
                }
            }
        }
    }
}
