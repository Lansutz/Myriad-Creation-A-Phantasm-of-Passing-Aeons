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
    /// 无主地图单位类型。
    /// 这些单位不一定有上级政权，在地图上自主行动，具有AI。
    /// 可扩展：流民、游牧民、商队、雇佣兵、野怪、动物灾害等。
    /// </summary>
    public enum MapActorType
    {
        Refugee = 0,        // 流民：从战乱/饥荒逃向安全地区
        Nomad = 1,          // 游牧民：草原逐水草而居
        Caravan = 2,        // 商队：贸易路线上运输物资
        Mercenary = 3,      // 雇佣兵：可被招募，各地游荡
        WildBeast = 4,      // 野怪/野兽：荒野活动，袭击聚落
        AnimalDisaster = 5, // 动物灾害：蝗虫/鼠患，破坏农业
        Bandit = 6,         // 土匪/强盗：劫掠商队和村镇
        Pilgrim = 7,        // 朝圣者：前往宗教圣地
        Custom = 99         // 模组自定义类型
    }

    /// <summary>
    /// 无主地图单位基类。
    /// 参考 CK3 的游荡单位（host）和 EU4 的叛军/游牧单位设计，
    /// 但更轻量：不绑定政权，自主AI决策，可与各系统联动。
    /// </summary>
    public class MapActor
    {
        public int actorId;
        public MapActorType type;
        public string actorName = "";

        // ===== 位置与移动 =====
        public int currentTile;
        public int targetTile = -1;
        public float movementProgress; // 0-1，到达下一地块时归零
        public float moveSpeed = 1.0f; // 地块/天

        // ===== 规模与状态 =====
        public int population;     // 人数/规模
        public float health = 100f; // 健康度(0-100)
        public float morale = 80f;  // 士气(0-100)
        public float supplies = 100f; // 补给(0-100)

        // ===== 阵营关系 =====
        public int ownerRealmId = -1;  // 所属政权(-1=无主)
        public int factionId = -1;     // 所属派系(-1=无)
        public bool isHostile = false; // 是否敌对状态

        // ===== 自定义属性（模组扩展用） =====
        public Dictionary<string, float> attributes = new Dictionary<string, float>();

        // ===== AI 行为 =====
        private IMapActorAI _ai;
        public IMapActorAI AI
        {
            get => _ai;
            set => _ai = value;
        }

        /// <summary>每日更新：移动 + AI决策</summary>
        public virtual void Tick(GameWorld world, float deltaDays)
        {
            // 移动
            if (targetTile >= 0 && targetTile != currentTile)
            {
                movementProgress += moveSpeed * deltaDays * GetTerrainMoveModifier(world);
                if (movementProgress >= 1f)
                {
                    movementProgress = 0f;
                    currentTile = targetTile;
                    OnArriveTile(world);
                }
            }

            // AI 决策
            _ai?.Think(world, this);
            _ai?.Act(world, this);

            // 补给消耗
            supplies -= deltaDays * 0.5f;
            if (supplies <= 0)
            {
                health -= deltaDays * 2f;
                morale -= deltaDays * 1f;
            }

            // 死亡判定
            if (health <= 0 || population <= 0)
            {
                OnDeath(world);
            }
        }

        /// <summary>地形移动修正（极难通行地区速度大幅降低）</summary>
        protected virtual float GetTerrainMoveModifier(GameWorld world)
        {
            if (world == null || currentTile < 0 || currentTile >= world.tiles.Length) return 1f;
            var tile = world.tiles[currentTile];
            float cost = tile.movementCost;
            if (cost <= 0) cost = 1f;
            return Mathf.Clamp(1f / cost, 0.02f, 1f);
        }

        /// <summary>到达新地块时触发</summary>
        protected virtual void OnArriveTile(GameWorld world) { }

        /// <summary>死亡/解散时触发</summary>
        protected virtual void OnDeath(GameWorld world)
        {
            Debug.Log($"[MapActor] {actorName}({type}) 已消亡，地块={currentTile}");
        }

        /// <summary>设置目标地块（寻路由AI决定）</summary>
        public void SetTarget(int tileIndex)
        {
            targetTile = tileIndex;
            movementProgress = 0f;
        }

        /// <summary>是否存活</summary>
        public bool IsAlive => health > 0 && population > 0;
    }

    /// <summary>
    /// 无主地图单位 AI 接口。
    /// 每种单位类型有自己的 AI 实现，决定移动目标和行为。
    /// </summary>
    public interface IMapActorAI
    {
        /// <summary>思考：决定目标和行为模式</summary>
        void Think(GameWorld world, MapActor actor);

        /// <summary>行动：执行具体行为（劫掠、交易、招募等）</summary>
        void Act(GameWorld world, MapActor actor);
    }
}
