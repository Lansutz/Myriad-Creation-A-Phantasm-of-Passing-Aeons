using System.Collections.Generic;

using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;


namespace MyriadCreation.Simulation.Actors
{
    /// <summary>
    /// TemporaryEncampment 的用途细分，不代表独立的栖居点模式。
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
    /// 迁移期营地数据视图；实例本身继承 HabitationSite。
    /// </summary>
    [System.Serializable]
    public class CampData : HabitationSite
    {
        public int campId;
        public string campName
        {
            get => siteName;
            set => siteName = value;
        }

        private CampType _type;
        public CampType type
        {
            get => _type;
            set
            {
                _type = value;
                InitializeModeData(siteMode);
                if (siteMode == SiteMode.TemporaryEncampment)
                    temporaryEncampmentData.encampmentKind = value.ToString();
                else if (siteMode == SiteMode.FortifiedOutpost)
                    fortifiedOutpostData.purpose = value.ToString();
            }
        }

        public int tileIndex;
        public int ownerRealmId = -1;   // 所属政权（-1=无主，如土匪寨）
        public int ownerActorId = -1;   // 所属MapActor（如游牧部落、土匪帮）

        // ===== 规模与防御 =====
        public float defense = 10f;     // 防御值（影响攻破难度）
        public float supplies = 100f;   // 补给储备
        public float morale = 80f;      // 守军士气

        // ===== 状态 =====
        public int establishedDay;      // 建立日期（游戏日）
        public float permanence = 0f;   // 永久化进度 0-100（达到100可演变为聚落）
        public bool isAbandoned = false; // 是否被废弃

        // ===== 自定义属性 =====
        public Dictionary<string, float> attributes = new Dictionary<string, float>();

        public CampData()
        {
            siteMode = SiteMode.TemporaryEncampment;
            InitializeModeData(siteMode);
        }

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
                    population = 0;
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

        public IEnumerable<HabitationSite> ActiveHabitationSites
        {
            get
            {
                foreach (var camp in _camps)
                    if (!camp.isAbandoned)
                        yield return camp;
            }
        }

        public CampData GetCamp(int campId)
        {
            _campById.TryGetValue(campId, out var camp);
            return camp;
        }

        public bool TryGetActiveSiteByAnchor(int anchorId, out HabitationSite site)
        {
            foreach (var camp in _camps)
            {
                if (!camp.isAbandoned && camp.anchorId == anchorId)
                {
                    site = camp;
                    return true;
                }
            }

            site = null;
            return false;
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

        public void RestoreCamp(CampData camp)
        {
            if (camp == null)
                throw new System.ArgumentNullException(nameof(camp));
            if (camp.campId <= 0 || _campById.ContainsKey(camp.campId))
                throw new System.InvalidOperationException($"Invalid or duplicate camp ID {camp.campId}.");
            if (!System.Guid.TryParse(camp.siteId, out _))
                throw new System.InvalidOperationException($"Camp {camp.campId} has an invalid site UUID.");
            if (camp.tileIndex < 0 || _world.tiles == null || camp.tileIndex >= _world.tiles.Length)
                throw new System.InvalidOperationException($"Camp {camp.campId} has an invalid tile index.");

            camp.isAbandoned = false;
            camp.InitializeModeData(camp.siteMode);
            _camps.Add(camp);
            _campById.Add(camp.campId, camp);
            _nextCampId = System.Math.Max(_nextCampId, camp.campId + 1);

            var tile = _world.tiles[camp.tileIndex];
            tile.campId = camp.campId;
            _world.tiles[camp.tileIndex] = tile;
        }

        /// <summary>拆除/废弃营寨</summary>
        public void AbandonCamp(int campId)
        {
            if (_campById.TryGetValue(campId, out var camp))
            {
                if (camp.siteMode == SiteMode.FortifiedOutpost
                    && !HabitationSiteTransitionSystem.TryTransition(
                        (HabitationSite)camp,
                        SiteMode.TemporaryEncampment,
                        out var transitionFailure))
                {
                    Debug.LogError($"[CampManager] 无法先将据点转为营地再消散：{transitionFailure}");
                    return;
                }
                if (!HabitationSiteTransitionSystem.CanDissolve(camp))
                {
                    Debug.LogError($"[CampManager] 只有临时营地可以消散，目标#{campId}模式={SiteModeKeys.ToKey(camp.siteMode)}");
                    return;
                }

                RemoveCamp(camp);
                Debug.Log($"[CampManager] 营寨#{campId}被废弃");
            }
        }

        public void RemoveTransferredCamp(int campId, string replacementSiteId)
        {
            if (!_campById.TryGetValue(campId, out var camp))
                throw new System.InvalidOperationException($"Camp {campId} does not exist for site transfer.");
            if (camp.siteId != replacementSiteId)
                throw new System.InvalidOperationException($"Transferred site ID mismatch for camp {campId}.");
            if (!_world.TryGetHabitationSite(replacementSiteId, out var replacement)
                || System.Object.ReferenceEquals(replacement, camp))
                throw new System.InvalidOperationException($"Replacement habitation site '{replacementSiteId}' is not registered.");

            RemoveCamp(camp);
        }

        /// <summary>营寨被摧毁（战斗中攻破）</summary>
        public void DestroyCamp(int campId, int attackerRealmId = -1)
        {
            if (_campById.TryGetValue(campId, out var camp))
            {
                if (camp.siteMode == SiteMode.FortifiedOutpost
                    && !HabitationSiteTransitionSystem.TryTransition(
                        (HabitationSite)camp,
                        SiteMode.TemporaryEncampment,
                        out var transitionFailure))
                {
                    Debug.LogError($"[CampManager] 无法先将被摧毁据点转为营地：{transitionFailure}");
                    return;
                }
                if (!HabitationSiteTransitionSystem.CanDissolve(camp))
                {
                    Debug.LogError($"[CampManager] 只有临时营地可以在摧毁后消散，目标#{campId}模式={SiteModeKeys.ToKey(camp.siteMode)}");
                    return;
                }

                camp.population = Mathf.Max(0, camp.population / 2); // 半数伤亡
                // 清除地块关联
                if (camp.tileIndex >= 0 && camp.tileIndex < _world.tiles.Length)
                {
                    var tile = _world.tiles[camp.tileIndex];
                    if (tile.campId == campId) tile.campId = -1;
                    tile.order = Mathf.Max(0f, tile.order - 10f);
                    _world.tiles[camp.tileIndex] = tile;
                }
                RemoveCamp(camp);
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
                if (camp.isAbandoned)
                    RemoveCamp(camp);
            }
        }

        private void RemoveCamp(CampData camp)
        {
            camp.isAbandoned = true;
            if (camp.tileIndex >= 0 && camp.tileIndex < _world.tiles.Length)
            {
                var tile = _world.tiles[camp.tileIndex];
                if (tile.campId == camp.campId)
                    tile.campId = -1;
                _world.tiles[camp.tileIndex] = tile;
            }

            _camps.Remove(camp);
            _campById.Remove(camp.campId);
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
            foreach (var camp in _camps)
            {
                if (camp.tileIndex < 0 || _world.tiles == null || camp.tileIndex >= _world.tiles.Length)
                    continue;
                var tile = _world.tiles[camp.tileIndex];
                if (tile.campId == camp.campId)
                    tile.campId = -1;
                _world.tiles[camp.tileIndex] = tile;
            }

            _camps.Clear();
            _campById.Clear();
            _nextCampId = 1;
        }
    }
}
