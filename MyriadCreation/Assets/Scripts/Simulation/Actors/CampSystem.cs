using System.Collections.Generic;

using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;


namespace CivilizationEvolution.Simulation.Actors
{
    /// <summary>
    /// 营寨类型。
    /// 军队、盗匪、蛮族、游牧政权都可以建立营寨。
    /// 营寨是临时/半永久据点，与定居聚落（Burg）区分。
    /// </summary>
    public enum CampType
    {
        Temporary = 0,    // 临时营地：军队行军途中扎营
        Military = 1,     // 军营：长期驻军的要塞营地
        Bandit = 2,       // 土匪寨：盗匪据点
        Nomad = 3,        // 游牧营地：游牧部落的季节性营地
        Refugee = 4,      // 难民营：流民聚集的临时营地
        Custom = 99       // 模组自定义
    }

    /// <summary>
    /// 营寨数据。
    /// 与定居聚落（Burg）的区别：
    /// - 营寨可以快速建立和拆除
    /// - 营寨不发展经济，主要提供补给和防御
    /// - 游牧营寨是行国的核心据点，可随季节迁移
    /// - 长期驻扎的营寨可能演变为聚落
    /// </summary>
    [System.Serializable]
    public class CampData
    {
        public int campId;
        public string campName = "";
        public CampType type;
        public int tileIndex;
        public int ownerRealmId = -1;   // 所属政权（-1=无主，如土匪寨）
        public int ownerActorId = -1;   // 所属MapActor（如游牧部落、土匪帮）

        // ===== 规模与防御 =====
        public int population;          // 驻扎人口
        public float defense = 10f;     // 防御值（影响攻破难度）
        public float supplies = 100f;   // 补给储备
        public float morale = 80f;      // 守军士气

        // ===== 状态 =====
        public int establishedDay;      // 建立日期（游戏日）
        public float permanence = 0f;   // 永久化进度 0-100（达到100可演变为聚落）
        public bool isAbandoned = false; // 是否被废弃

        // ===== 自定义属性 =====
        public Dictionary<string, float> attributes = new Dictionary<string, float>();

        /// <summary>每日更新：补给消耗、永久化进度、废弃判定</summary>
        public void Tick(GameWorld world, float deltaDays)
        {
            if (isAbandoned) return;

            // 补给消耗
            float dailyConsumption = population * 0.1f * deltaDays;
            supplies -= dailyConsumption;

            if (supplies <= 0)
            {
                morale -= deltaDays * 2f;
                population = Mathf.Max(0, population - Mathf.RoundToInt(deltaDays));
                if (population <= 0 || morale <= 0)
                {
                    isAbandoned = true;
                    Debug.Log($"[Camp] 营寨#{campId}({type}) 因补给耗尽被废弃，地块={tileIndex}");
                    return;
                }
            }

            // 永久化进度：长期驻扎且补给充足时缓慢增长
            if (supplies > 50f && population > 20)
            {
                permanence += deltaDays * 0.05f;
                if (permanence >= 100f)
                    permanence = 100f;
            }
            // 游牧营地不永久化（逐水草而居）
            if (type == CampType.Nomad)
                permanence = 0f;
        }

        /// <summary>是否可以演变为聚落（永久化满 + 非游牧 + 人口足够）</summary>
        public bool CanEvolveToSettlement =>
            permanence >= 100f && type != CampType.Nomad && type != CampType.Temporary && population >= 50;
    }

    /// <summary>
    /// 营寨管理器。
    /// 负责：营寨的建立、拆除、更新、查询。
    /// 与MapActor、军事系统、游牧政权联动。
    /// </summary>
    public class CampManager
    {
        private readonly GameWorld _world;
        private readonly List<CampData> _camps = new List<CampData>();
        private readonly Dictionary<int, CampData> _campById = new Dictionary<int, CampData>();
        private int _nextCampId = 1;

        public CampManager(GameWorld world)
        {
            _world = world;
        }

        public IReadOnlyList<CampData> AllCamps => _camps;
        public int Count => _camps.Count;

        public CampData GetCamp(int campId)
        {
            _campById.TryGetValue(campId, out var camp);
            return camp;
        }

        public List<CampData> GetCampsAtTile(int tileIndex)
        {
            var result = new List<CampData>();
            foreach (var camp in _camps)
                if (camp.tileIndex == tileIndex && !camp.isAbandoned) result.Add(camp);
            return result;
        }

        public List<CampData> GetCampsOfRealm(int realmId)
        {
            var result = new List<CampData>();
            foreach (var camp in _camps)
                if (camp.ownerRealmId == realmId && !camp.isAbandoned) result.Add(camp);
            return result;
        }

        /// <summary>建立营寨</summary>
        public CampData EstablishCamp(CampType type, int tileIndex, int population,
            int ownerRealmId = -1, int ownerActorId = -1, string name = "")
        {
            if (tileIndex < 0 || tileIndex >= _world.tiles.Length) return null;
            var tile = _world.tiles[tileIndex];
            if (!tile.exists || !tile.isLand) return null;

            var camp = new CampData
            {
                campId = _nextCampId++,
                type = type,
                tileIndex = tileIndex,
                population = population,
                ownerRealmId = ownerRealmId,
                ownerActorId = ownerActorId,
                establishedDay = _world.currentDay,
                campName = string.IsNullOrEmpty(name) ? GetDefaultCampName(type) + "#" + _nextCampId : name
            };
            _camps.Add(camp);
            _campById[camp.campId] = camp;

            // 关联到地块
            tile.campId = camp.campId;
            _world.tiles[tileIndex] = tile;

            Debug.Log($"[CampManager] 建立{type}营寨#{camp.campId}，地块={tileIndex}，人口={population}");
            return camp;
        }

        /// <summary>拆除/废弃营寨</summary>
        public void AbandonCamp(int campId)
        {
            if (_campById.TryGetValue(campId, out var camp))
            {
                camp.isAbandoned = true;
                // 清除地块关联
                if (camp.tileIndex >= 0 && camp.tileIndex < _world.tiles.Length)
                {
                    var tile = _world.tiles[camp.tileIndex];
                    if (tile.campId == campId) tile.campId = -1;
                    _world.tiles[camp.tileIndex] = tile;
                }
                Debug.Log($"[CampManager] 营寨#{campId}被废弃");
            }
        }

        /// <summary>营寨被摧毁（战斗中攻破）</summary>
        public void DestroyCamp(int campId, int attackerRealmId = -1)
        {
            if (_campById.TryGetValue(campId, out var camp))
            {
                camp.isAbandoned = true;
                camp.population = Mathf.Max(0, camp.population / 2); // 半数伤亡
                // 清除地块关联
                if (camp.tileIndex >= 0 && camp.tileIndex < _world.tiles.Length)
                {
                    var tile = _world.tiles[camp.tileIndex];
                    if (tile.campId == campId) tile.campId = -1;
                    tile.order = Mathf.Max(0f, tile.order - 10f);
                    _world.tiles[camp.tileIndex] = tile;
                }
                Debug.Log($"[CampManager] 营寨#{campId}被摧毁，攻击者政权={attackerRealmId}");
            }
        }

        /// <summary>每日更新所有营寨</summary>
        public void Tick(float deltaDays)
        {
            for (int i = _camps.Count - 1; i >= 0; i--)
            {
                var camp = _camps[i];
                camp.Tick(_world, deltaDays);
                // 清理长期废弃的营寨（30天后从列表移除）
                if (camp.isAbandoned && _world.currentDay - camp.establishedDay > 30)
                {
                    _camps.RemoveAt(i);
                    _campById.Remove(camp.campId);
                }
            }
        }

        private string GetDefaultCampName(CampType type)
        {
            switch (type)
            {
                case CampType.Temporary: return "临时营地";
                case CampType.Military: return "军营";
                case CampType.Bandit: return "土匪寨";
                case CampType.Nomad: return "游牧营地";
                case CampType.Refugee: return "难民营";
                default: return "营地";
            }
        }

        public void Clear()
        {
            _camps.Clear();
            _campById.Clear();
            _nextCampId = 1;
        }
    }
}
