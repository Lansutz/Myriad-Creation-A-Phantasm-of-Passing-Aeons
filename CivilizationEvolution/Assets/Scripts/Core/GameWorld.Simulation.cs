using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Climate;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Character;
using CivilizationEvolution.Thought;
using CivilizationEvolution.Disaster;
using CivilizationEvolution.Building;
using CivilizationEvolution.Tech;
using CivilizationEvolution.AI;

namespace CivilizationEvolution.Core
{
 /// GameWorld.Simulation —— 主循环模拟Tick（人口/政治/社会/传教/信仰/时间推进）（partial class，与 GameWorld.cs 共享字段与子系统）
    public partial class GameWorld
    {

 /// 人口Tick：自然增长、满意度、迁徙——Parallel.For 多核并行
 /// （优化 2026-09-04：52万-472万地块每日热循环——每 tile 独立无
 /// 共享写——事件入队改并发收集[ConcurrentQueue]后主线程统一入队——
 /// 读共享字典/贸易中心安全[模拟单线程 Tick 内独占并行段]）
        private void PopulationTick()
        {
            var parallelEvents = new System.Collections.Concurrent.ConcurrentQueue<GameEvent>();
            System.Threading.Tasks.Parallel.For(0, tiles.Length, i =>
            {
                if (!tiles[i].exists || !tiles[i].isLand || tiles[i].populationBlocks == null) return;

                for (int j = 0; j < tiles[i].populationBlocks.Count; j++)
                {
                    var pb = tiles[i].populationBlocks[j];

                    if (races.TryGetValue(pb.raceId, out var race))
                    {
                        float growthRate = race.CalculatePopulationGrowthRate(tiles[i], pb.satisfaction);

 // 人口承载约束（超载抑制增长——地理/仓储/贸易决定上限）
                        float capacity = CarryingCapacitySystem.CalculateCarryingCapacity(tiles[i], tradeCenters);
                        float overload = CarryingCapacitySystem.GetOverloadRatio(tiles[i], capacity);
                        if (overload > 1f)
                        {
 // 超载 10% 起线性抑制，超载 200% 完全停止
                            float suppression = Mathf.Clamp01((overload - 1.1f) / 0.9f);
                            growthRate *= 1f - suppression;
                        }

                        pb.count *= (1f + growthRate * daysPerTick);
                    }

                    float satisfactionDelta = CalculateSatisfactionDelta(i, pb);
                    pb.satisfaction = Mathf.Clamp(pb.satisfaction + satisfactionDelta, 0f, 100f);

                    if (pb.satisfaction < 20f && ThreadRng.Value.NextDouble() < 0.01)
                    {
                        parallelEvents.Enqueue(new GameEvent
                        {
                            eventType = GameEventType.Rebellion,
                            tileIndex = i,
                            severity = 100f - pb.satisfaction
                        });
                    }

                    tiles[i].populationBlocks[j] = pb;
                }

                if (tradeCenters.TryGetValue(tiles[i].regionId, out var tc))
                {
                    float foodStock = tc.inventory.GetValueOrDefault(0, 0f);
                    float totalPop = GetTilePopulation(i);
                    if (foodStock < totalPop * 0.01f && totalPop > 0)
                    {
                        parallelEvents.Enqueue(new GameEvent
                        {
                            eventType = GameEventType.Famine,
                            tileIndex = i,
                            severity = 50f
                        });
                    }
                }
            });

 // 并发收集事件统一入队（EnqueueEvent 主线程专用——顺序无关）
            while (parallelEvents.TryDequeue(out var ev))
                EnqueueEvent(ev);
        }


 /// <summary>计算人口块满意度变化</summary>
        private float CalculateSatisfactionDelta(int tileIndex, PopulationBlock pb)
        {
            float delta = 0f;
            ref TileData tile = ref tiles[tileIndex];

            delta += (tile.stability - 50f) * 0.01f;

            if (realms.TryGetValue(tile.ownerRealmId, out var realm))
            {
                delta += realm.taxSystem.GetTaxSatisfactionImpact(pb.socialClass) * 0.01f;
            }

            if (cultures.TryGetValue(pb.cultureId, out var culture))
            {
                delta += (pb.culturePenetration - 50f) * 0.005f;
            }

            if (races.TryGetValue(pb.raceId, out var race))
            {
                delta += (race.ClassRelationBaseline - 50f) * 0.005f;
            }

            return delta * daysPerTick;
        }


 /// <summary>政治Tick（兼容层：税收结算+稳定值）</summary>
        private void PoliticsTick()
        {
 // 社会分化按固定间隔推进（对所有政权同步），避免每天搬运人口
            _differentiationTimer += daysPerTick;
            bool doDifferentiation = _differentiationTimer >= DifferentiationIntervalDays;
            if (doDifferentiation) _differentiationTimer = 0f;

 // 税收结算（政权循环——轻）
            foreach (var realm in realms.Values)
            {
                float taxIncome = _economyManager.SettleTaxes(realm.realmId);
                realm.treasury += taxIncome;
            }

 // 稳定值收敛（优化 2026-09-04：原为政权循环内嵌全图扫=N×tiles——
 // 改单次全扫 O(tiles)——每 tile 按其所属政权集权度收敛）
            {
 // 政权集权度缓存（每 tick 一次构建——避免循环内 realms 查）
                var centralCache = new Dictionary<int, float>(realms.Count);
                foreach (var r in realms.Values)
                    if (r != null) centralCache[r.realmId] = 50f + r.centralization * 20f;
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (!tiles[i].exists) continue;
                    int owner = tiles[i].ownerRealmId;
                    if (owner < 0) continue;
                    if (centralCache.TryGetValue(owner, out float target))
                        tiles[i].stability = Mathf.Lerp(tiles[i].stability, target, 0.01f * daysPerTick);
                }
            }

 // 领地索引（优化：单次全扫构建 ownerRealmId→地块——各政权社会
 // 评估复用——替代每政权全扫[N×tiles]）
            var realmTilesIndex = new Dictionary<int, List<int>>();
            for (int i = 0; i < tiles.Length; i++)
            {
                if (!tiles[i].exists) continue;
                int owner = tiles[i].ownerRealmId;
                if (owner < 0) continue;
                if (!realmTilesIndex.TryGetValue(owner, out var list))
                {
                    list = new List<int>();
                    realmTilesIndex[owner] = list;
                }
                list.Add(i);
            }

 // 社会-派系-政体变迁脉冲（每政权——内部评估——用领地索引免全扫）
            foreach (var realm in realms.Values)
            {
                realmTilesIndex.TryGetValue(realm.realmId, out var rt);
                SocietyPulse(realm, doDifferentiation, rt);
            }
        }


 /// 社会-派系-政体变迁脉冲：情境采集 → 阶层需求满足度 → 政治能量 → 派系组织化 → 关键节点博弈。
 /// 阶层好感在此由真实需求满足度驱动（替代旧的机械回归中性值）。
        private void SocietyPulse(RealmData realm, bool doDifferentiation,
            IReadOnlyList<int> realmTiles = null)
        {
            var sit = RealmSituationBuilder.Build(realm, tiles, _economyManager, _wars, armies,
                _disasterSystem, _innovationTree, realmTiles);

 // 先推进社会分工（人口在阶层间缓慢、守恒转移），再统计社会画像，保证派系看到的是最新阶层结构
            if (doDifferentiation)
                SocialDifferentiation.DifferentiateRealm(realm, tiles, sit, realmTiles);

            var society = _societyManager.EvaluateRealm(realm, tiles, sit, realmTiles);
            _societyManager.ApplyClassRelations(realm, society, daysPerTick);
            var characters = _characterManager.GetCharactersByRealm(realm.realmId);
            _factionManager.UpdateRealmFactions(society, realm, characters);
            _regimeDynamics.Tick(currentDay, realm, society, sit, _factionManager);
            _societyCache[realm.realmId] = society;
        }


 /// 行政区划树生成（政权辖境→AdminDivisionSystem——治理模式定层数：
 /// 分封 4 层/郡县=行政容量弹性——官僚头衔随层绑定——批4）
        private void EnsureAdminDivisions()
        {
            for (int i = 0; i < realms.Count; i++)
            {
                var realm = realms[i];
                if (realm == null || realm.adminDivisions.Count > 0) continue;
 // 辖境=核心+占领地块
                var territory = new HashSet<int>(realm.coreTiles);
                for (int t = 0; t < tiles.Length; t++)
                    if (tiles[t].ownerRealmId == realm.realmId) territory.Add(t);
                if (territory.Count == 0) continue;
                var effects = realm.composition != null
                    ? Politics.GovernmentEffects.CalculateEffects(realm.composition) : null;
                float capacity = effects != null ? effects.administrativeCapacity : 0.5f;
                realm.adminDivisions = Culture.AdminDivisionSystem.Generate(realm,
                    realm.composition, capacity, territory);
            }
        }


 /// 政权传教（15 天一次——国教政权对其境内异教主流块传教——
 /// 成功率=冲突度[同根 0.6/异教 0.2]——ConvertTile——限频省扫描）
        private void MissionaryTick()
        {
            _missionaryTimer += daysPerTick;
            if (_missionaryTimer < MissionaryIntervalDays) return;
            _missionaryTimer = 0;
            if (tiles == null) return;

 // 政权→国教映射（一次构建）
            var realmFaith = new Dictionary<int, int>();
            foreach (var r in realms.Values)
                if (r != null && r.stateReligionId >= 0) realmFaith[r.realmId] = r.stateReligionId;
            if (realmFaith.Count == 0) return;

            for (int i = 0; i < tiles.Length; i++)
            {
                ref TileData tile = ref tiles[i];
                if (!tile.exists || tile.populationBlocks == null || tile.populationBlocks.Count == 0) continue;
                int owner = tile.ownerRealmId;
                if (owner < 0 || !realmFaith.TryGetValue(owner, out int stateFaith)) continue;
 // 本地主流已是国教→跳过
                int localFaith = Politics.PopulationStats.GetDominantFaith(tile);
                if (localFaith == stateFaith) continue;
 // 传教（成功率=冲突度）
                float chance = Culture.MissionarySystem.CalculateSuccessChance(tile, stateFaith,
                    id => Culture.ReligionCatalog.Get(id),
                    id => { var r = Culture.ReligionCatalog.GetRoot(id); return r != null ? r.religionId : -1; });
                if (chance > 0f)
                    Culture.MissionarySystem.ConvertTile(tile, stateFaith, owner, chance, _missionaryRng);
            }
        }


        private void OnInnovationCompletedHandler(int realmId, int innovationId)
        {
            if (!realms.TryGetValue(realmId, out var realm)) return;
            CultureData culture = null;
            if (realm.primaryCultureId >= 0)
                cultures.TryGetValue(realm.primaryCultureId, out culture);
            Economy.ClassEmergenceEvents.RecordEmergence(innovationId, realm.realmName,
                culture, _innovationTree, realmId, _chronicle);
        }


 /// 官职任命（Governor 等 6 官职——空缺补任——从政权角色中选非统治者者——
 /// OfficeTitle 官职称号系统的持有者数据源）
        private void EnsureOfficeHolders()
        {
            if (_characterManager == null) return;
            for (int i = 0; i < realms.Count; i++)
            {
                var realm = realms[i];
                if (realm == null) continue;
                int rulerId = realm.GetSupremeRulerId();
                for (int o = 0; o < 6; o++)
                {
                    if (realm.officeHolders.ContainsKey(o) && realm.officeHolders[o] >= 0) continue;
 // 空缺——从政权角色选（非统治者——优先 Noble/Military）
                    var candidates = _characterManager.GetCharactersByRealm(realm.realmId);
                    CharacterData pick = null;
                    foreach (var c in candidates)
                    {
                        if (c == null || c.characterId == rulerId || !c.isAlive) continue;
                        if (c.role == Character.CharacterRole.Noble || c.role == Character.CharacterRole.Military
                            || c.role == Character.CharacterRole.Scholar)
                        { pick = c; break; }
                    }
                    if (pick == null && candidates.Count > 0)
                        foreach (var c in candidates)
                            if (c != null && c.characterId != rulerId && c.isAlive) { pick = c; break; }
                    if (pick != null)
                        realm.officeHolders[o] = pick.characterId;
                    else
                        realm.officeHolders[o] = -1; // 无人可任（保留空缺位）
                }
            }
        }


 /// <summary>时间推进</summary>
        private void AdvanceTime()
        {
            currentDay += daysPerTick;
            if (currentDay > 365)
            {
                currentDay = 1;
                currentYear++;
            }

            int newSeason = (currentDay - 1) / 91;
            if (newSeason != currentSeason && newSeason < 4)
            {
                currentSeason = newSeason;
                _climateSimulator.UpdateForSeason(currentSeason);
                EnqueueEvent(new GameEvent
                {
                    eventType = GameEventType.SeasonChange,
                    severity = currentSeason
                });
            }
        }


 /// <summary>信仰热忱每日更新（30 天限频——长期和平冷却 -10/年——圣地丢失由
 /// 圣地系统检测；异教冲突由宣战处 AddFervor）</summary>
        private void UpdateFaithFervor(int currentDay)
        {
            if (currentDay - _faithFervorDay < FaithFervorInterval) return;
            _faithFervorDay = currentDay;
            foreach (var faith in _faithSystems)
            {
 // 长期和平冷却（约每年一次——每 360 天 -10）
                if (faith.fervor > 10f && currentDay % 360 == 0)
                    faith.AddFervor(-10f);

 // 圣地丢失检测（己方圣地被异教政权控制 → +50——"收复失地"狂热——
 // 十字军启动器；一次性标记防重复刷）
                CheckHolySiteLoss(faith);
            }
        }


 /// <summary>圣地丢失检测（圣地地块被非本教统国教政权控制 → 热忱+50）</summary>
        private void CheckHolySiteLoss(FaithSystem faith)
        {
            foreach (var tileIndex in faith.holySiteTileIndices)
            {
                int key = faith.faithId * 100000 + tileIndex;
                if (_holySiteLostProcessed.Contains(key)) continue;
                if (tileIndex < 0 || tileIndex >= tiles.Length) continue;
                int owner = tiles[tileIndex].ownerRealmId;
                if (owner < 0 || owner >= realms.Count) continue;
                int ownerFaith = realms[owner].stateReligionId;
 // 被异教控制（有国教且不同信仰）→ 丢失
                if (ownerFaith >= 0 && ownerFaith != faith.faithId)
                {
                    faith.AddFervor(50f);
                    _holySiteLostProcessed.Add(key);
                    _chronicle?.Add("religion", $"圣地失陷：{faith.faithName} 的圣地被异教控制——信仰热忱高涨",
                        major: true, owner);
                }
            }
        }


 /// <summary>大圣战结算检查（关联 WarState 结束→OnLinkedWarEnded——
 /// 圣战方胜→受益人谈判[继承法线外者]——土地归受益人[简化：目标地块转移]）</summary>
        private void CheckGreatHolyWarSettlements()
        {
            var active = new List<War.GreatHolyWarState>(War.GreatHolyWarSystem.ActiveWars);
            foreach (var ghw in active)
            {
                if (ghw.ended) continue;
                var linked = _wars.Find(w => w.warId == ghw.linkedWarId);
                if (linked == null || !linked.ended) continue;

                bool holySideWon = linked.winnerId == ghw.callerRealmId;
                ghw.holySideWon = holySideWon;
                var callerRealm = ghw.callerRealmId >= 0 && ghw.callerRealmId < realms.Count
                    ? realms[ghw.callerRealmId] : null;
                int beneficiary = War.GreatHolyWarSystem.Resolve(ghw, GetCharacterManager(),
                    callerRealm, Mathf.Max(1, GetRealmDivisibleEstates(ghw.callerRealmId)),
                    GetEffectiveLaw(ghw.callerRealmId));
                if (holySideWon && beneficiary >= 0 && ghw.targetTile >= 0 && ghw.targetTile < tiles.Length)
                {
 // 目标地块归受益人（新政权/附庸——简化：地块归属转移+编年史）
                    tiles[ghw.targetTile].ownerRealmId = ghw.callerRealmId;
                    _chronicle?.Add("religion",
                        $"大圣战胜利：{GetCharacterName(beneficiary)} 受封圣地（地块 {ghw.targetTile}）",
                        major: true, ghw.callerRealmId);
                }
                ghw.ended = true;
            }
            War.GreatHolyWarSystem.Cleanup();
        }

    }
}
