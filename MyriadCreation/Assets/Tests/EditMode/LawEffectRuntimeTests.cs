using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 法律/法令效果消费层测试：SumLawEffects/SumDecreeEffects 合并后的税收/人力乘子、
    /// 年度 stability/legitimacy/unrest 效果、法令到期不计入、效果叠加、空政权不抛，
    /// 以及补员计算点（Army.ReinforceUnit）对人力乘子的消费。
    /// </summary>
    public class LawEffectRuntimeTests
    {
        private static RealmData NewRealm(int realmId = 1)
        {
            var realm = new RealmData
            {
                realmId = realmId,
                realmName = "效果测试国",
                successionLawFromCulture = false,
                monarchId = 100,
                consulId = -1,
                heirId = -1,
                treasury = 1000f,
                prestige = 500f,
                centralization = 0.5f
            };
            realm.lawState = new RealmLawState { legitimacy = 60f };
            realm.decreeState = new RealmDecreeState();
            LawRealmHost.EnsureKnownLaws(realm);
            return realm;
        }

        private static void EnactDecree(RealmData realm, string decreeId, int day)
        {
            DecreeDef decree = DefaultLawCatalog.IndexDecrees(DefaultLawCatalog.CreateDecrees())[decreeId];
            float authority = realm.lawState.authority;
            ActiveDecree active = LawAndDecreeSystem.EnactDecree(decree, realm.lawState, realm.decreeState,
                ref realm.treasury, ref authority, realm.lawState.legitimacy, day, null, out string reason);
            realm.lawState.authority = authority;
            Assert.IsNotNull(active, reason);
        }

        // ===================== 1. 税收乘子：法律 × 法令；到期不计入 =====================

        [Test]
        public void TaxMultiplier_CombinesLawsAndDecrees_AndExpires()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_standard", 1); // ×1.1
            realm.lawState.SetActiveLaw("military", "militia", 1);       // ×1.0
            EnactDecree(realm, "decree_tax_relief", 10);                 // ×0.7，endDay=190

            Assert.AreEqual(0.77f, LawEffectRuntime.GetTaxMultiplier(realm, 20), 0.001f,
                "税收乘子 = 生效法律乘子 × 生效法令乘子");

            int endDay = realm.decreeState.active[0].endDay;
            LawAndDecreeSystem.TickDecreeExpiry(realm.decreeState, endDay + 1);
            Assert.AreEqual(1.1f, LawEffectRuntime.GetTaxMultiplier(realm, endDay + 1), 0.001f,
                "法令到期后不再计入税收乘子");
        }

        // ===================== 2. 人力乘子 =====================

        [Test]
        public void ManpowerMultiplier_FromActiveLaw()
        {
            var realm = NewRealm();
            Assert.AreEqual(1f, LawEffectRuntime.GetManpowerMultiplier(realm, 1), 0.001f, "无生效法律时为 1");

            realm.lawState.SetActiveLaw("military", "standing_army", 1);
            Assert.AreEqual(1.5f, LawEffectRuntime.GetManpowerMultiplier(realm, 1), 0.001f);

            realm.lawState.SetActiveLaw("military", "militia", 2);
            Assert.AreEqual(1.2f, LawEffectRuntime.GetManpowerMultiplier(realm, 2), 0.001f,
                "同组替换后只计现行法律");
        }

        // ===================== 3. 年度效果：stability / legitimacy =====================

        [Test]
        public void YearlyEffects_ApplyStabilityAndLegitimacy()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_heavy", 1); // stability -4 / legitimacy -2
            float stabilityBefore = realm.stability;
            float legitimacyBefore = realm.lawState.legitimacy;

            LawEffectTickResult result = LawEffectRuntime.ApplyYearlyEffects(realm, null, 100);

            Assert.AreEqual(-4f, result.stabilityDelta, 0.001f);
            Assert.AreEqual(-2f, result.legitimacyDelta, 0.001f);
            Assert.AreEqual(stabilityBefore - 4f, realm.stability, 0.001f, "写入 realm.stability");
            Assert.AreEqual(legitimacyBefore - 2f, realm.lawState.legitimacy, 0.001f, "写入 lawState.legitimacy");
        }

        // ===================== 4. 同类效果叠加（法律 + 法令） =====================

        [Test]
        public void YearlyEffects_StackAcrossLawsAndDecrees()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_light", 1); // stability +2 / legitimacy +1
            realm.lawState.SetActiveLaw("military", "militia", 1);   // stability +1
            EnactDecree(realm, "decree_tax_relief", 10);             // stability +2 / unrest -3
            realm.unrest = 10f;
            var society = new RealmSociety { realmId = 1, unrestScore = 10f };

            LawEffectTickResult result = LawEffectRuntime.ApplyYearlyEffects(realm, society, 20);

            Assert.AreEqual(5f, result.effect.stabilityPerYear, 0.001f, "stabilityPerYear 相加");
            Assert.AreEqual(-3f, result.effect.unrestPerYear, 0.001f, "unrestPerYear 相加");
            Assert.AreEqual(5f, result.stabilityDelta, 0.001f, "stability 只受 stabilityPerYear 影响");
            Assert.AreEqual(-3f, result.unrestDelta, 0.001f, "unrest 单独结算");
            Assert.AreEqual(55f, realm.stability, 0.001f);
            Assert.AreEqual(61f, realm.lawState.legitimacy, 0.001f);
            Assert.AreEqual(7f, realm.unrest, 0.001f, "unrest 写入 RealmData 持久字段");
            Assert.AreEqual(7f, society.unrestScore, 0.001f, "传入 society 时同步瞬时 unrestScore");
        }

        // ===================== 5. 法令到期后的年度效果不计入 =====================

        [Test]
        public void YearlyEffects_ExpiredDecreeNotCounted()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_light", 1);
            realm.lawState.SetActiveLaw("military", "militia", 1);
            EnactDecree(realm, "decree_tax_relief", 10);
            int endDay = realm.decreeState.active[0].endDay;
            LawAndDecreeSystem.TickDecreeExpiry(realm.decreeState, endDay + 1);

            LawEffectTickResult result = LawEffectRuntime.ApplyYearlyEffects(realm, null, endDay + 1);

            Assert.AreEqual(3f, result.effect.stabilityPerYear, 0.001f, "到期法令不再提供 stability");
            Assert.AreEqual(0f, result.effect.unrestPerYear, 0.001f, "到期法令不再提供 unrest");
            Assert.AreEqual(53f, realm.stability, 0.001f);
        }

        // ===================== 6. 周期效果按天数折算 =====================

        [Test]
        public void PeriodicEffects_ProrateByDays()
        {
            var realm = NewRealm(2);
            realm.lawState.SetActiveLaw("taxation", "tax_light", 1); // stability +2/年

            LawEffectTickResult result = LawEffectRuntime.ApplyPeriodicEffects(realm, null, 1, 182);

            float expected = 2f * 182f / 365f;
            Assert.AreEqual(expected, result.stabilityDelta, 0.001f);
            Assert.AreEqual(50f + expected, realm.stability, 0.01f, "月度调度按 deltaDays/365 折算");
        }

        // ===================== 7. 空政权 / 空目录不抛 =====================

        [Test]
        public void EmptyRealmAndNullInput_DoNotThrow()
        {
            Assert.AreEqual(1f, LawEffectRuntime.GetTaxMultiplier(null, 0), 0.001f);
            Assert.AreEqual(1f, LawEffectRuntime.GetManpowerMultiplier(null, 0), 0.001f);

            LawEffect neutral = LawEffectRuntime.GetCombinedEffect(null, 0);
            Assert.AreEqual(1f, neutral.taxMultiplier, 0.001f);
            Assert.AreEqual(1f, neutral.manpowerMultiplier, 0.001f);

            var empty = new RealmData { realmId = 9 };
            float stabilityBefore = empty.stability;
            LawEffectTickResult result = LawEffectRuntime.ApplyYearlyEffects(empty, null, 1);
            Assert.AreEqual(stabilityBefore, empty.stability, 0.001f, "空政权不产生年度变化");
            Assert.AreEqual(0f, result.stabilityDelta, 0.001f);
        }

        // ===================== 8. 补员计算点消费人力乘子 =====================

        [Test]
        public void ReinforceUnit_ConsumesManpowerMultiplier()
        {
            var unitDefs = new Dictionary<int, UnitDef>
            {
                { 1, new UnitDef { unitId = 1, manpowerCost = 10f } }
            };

            var armyOld = new Army();
            armyOld.unitCounts[1] = 10f;
            int oldResult = armyOld.ReinforceUnit(1, 100f, 100, unitDefs);
            Assert.AreEqual(100, oldResult, "乘子=1 时保持旧行为（可用 100 人力全部用完）");
            Assert.AreEqual(20f, armyOld.unitCounts[1], 0.001f);

            var armyNew = new Army();
            armyNew.unitCounts[1] = 10f;
            int scaledResult = armyNew.ReinforceUnit(1, 100f, 100, unitDefs, 0.5f);
            Assert.AreEqual(50, scaledResult, "乘子 0.5 → 可用补员人力减半");
            Assert.AreEqual(15f, armyNew.unitCounts[1], 0.001f);
        }

        // ===================== 9. 征收结算点消费税收乘子（EconomyManager.CollectTaxes） =====================

        [Test]
        public void CollectTaxes_AppliesTaxMultiplier()
        {
            var realm = new RealmData { realmId = 1, realmName = "税国", treasury = 0f };
            var manager = new CharacterManager();
            CharacterData ruler = manager.CreateCharacter("君", "氏", 40, true, 0, 0, 0, CharacterRole.Ruler);
            ruler.realmId = 1;
            ruler.currentTile = 0;
            realm.monarchId = ruler.characterId;
            var division = new AdminDivision
            {
                divisionId = 1,
                realmId = 1,
                level = 1,
                holderCharacterId = ruler.characterId
            };
            division.tiles.Add(0);
            realm.adminDivisions.Add(division);

            var tiles = new[]
            {
                new TileData
                {
                    exists = true,
                    isLand = true,
                    ownerRealmId = 1,
                    regionId = 0,
                    populationBlocks = new List<PopulationBlock>
                    {
                        new PopulationBlock { count = 10f, socialClass = MyriadCreation.Core.Enums.GameEnums.SocialClass.Peasant }
                    }
                }
            };
            var centers = new Dictionary<int, TradeCenter>
            {
                [0] = new TradeCenter { regionId = 0, centerTileIndex = 0, inventory = new Dictionary<int, float> { [0] = 1000f } }
            };
            var goods = new Dictionary<int, GoodsDef>
            {
                [0] = new GoodsDef { goodsId = 0, category = MyriadCreation.Core.Enums.GameEnums.GoodsCategory.Crop, edible = true, nutrition = 1f }
            };
            var realms = new Dictionary<int, RealmData> { [1] = realm };
            var economy = new EconomyManager(tiles, centers, goods, new CurrencySystem(), new TaxSystem(), realms, 1);

            float baseGoods = economy.CollectTaxes(1, manager, 1).goodsCollected;
            Assert.That(baseGoods, Is.GreaterThan(0f), "有仓储、有持有人应产生正实物税");

            centers[0].inventory[0] = 1000f;
            float doubled = economy.CollectTaxes(1, manager, 1, 2f).goodsCollected;
            Assert.AreEqual(baseGoods * 2f, doubled, 0.01f, "征收强度乘子应放大实物征收量");

            centers[0].inventory[0] = 1000f;
            float halved = economy.CollectTaxes(1, manager, 1, 0.5f).goodsCollected;
            Assert.AreEqual(baseGoods * 0.5f, halved, 0.01f, "征收强度乘子应缩小实物征收量");
        }

        // ===================== 10. B1：yearly 年份 guard（daysPerTick 跳跃不漏不重） =====================

        [Test]
        public void YearlyGuard_AppliesOncePerYear_WithDayJumps()
        {
            var realm = NewRealm(6);
            realm.lawState.SetActiveLaw("taxation", "tax_light", 1); // stability +2/年
            var realms = new List<RealmData> { realm };
            int lastAppliedYear = int.MinValue;

            Assert.AreEqual(1, LawEffectRuntime.ApplyYearlyIfNewYear(realms, _ => null, 1, 10, ref lastAppliedYear),
                "新的一年第一次 tick 结算一次");
            Assert.AreEqual(52f, realm.stability, 0.001f);

            Assert.AreEqual(0, LawEffectRuntime.ApplyYearlyIfNewYear(realms, _ => null, 1, 300, ref lastAppliedYear),
                "同一年内 daysPerTick 跳跃不应重复结算");
            Assert.AreEqual(52f, realm.stability, 0.001f);

            Assert.AreEqual(1, LawEffectRuntime.ApplyYearlyIfNewYear(realms, _ => null, 2, 1, ref lastAppliedYear),
                "跨年后即使 currentDay 跳跃也应结算");
            Assert.AreEqual(54f, realm.stability, 0.001f);
            Assert.AreEqual(0, LawEffectRuntime.ApplyYearlyIfNewYear(realms, _ => null, 2, 200, ref lastAppliedYear));
        }

        // ===================== 11. B2：人力入口消费 manpowerMultiplier =====================

        [Test]
        public void ManpowerSystem_AppliesLawMultiplier()
        {
            var realm = NewRealm(5);
            realm.coreTiles.Add(0);
            var tiles = new[]
            {
                new TileData
                {
                    exists = true,
                    isLand = true,
                    ownerRealmId = 5,
                    populationBlocks = new List<PopulationBlock>
                    {
                        new PopulationBlock { socialClass = MyriadCreation.Core.Enums.GameEnums.SocialClass.Peasant, count = 10f }
                    }
                }
            };
            var realms = new Dictionary<int, RealmData> { { 5, realm } };

            float baseline = ManpowerSystem.GetRealmTotalManpower(5, tiles, realms);
            Assert.That(baseline, Is.GreaterThan(0f), "测试地块应产生正人力池");

            realm.lawState.SetActiveLaw("military", "standing_army", 1); // manpower ×1.5
            float boosted = ManpowerSystem.GetRealmTotalManpower(5, tiles, realms, 1);

            Assert.AreEqual(baseline * 1.5f, boosted, 0.001f, "人力入口应乘法律人力乘子");
            Assert.AreEqual(baseline, ManpowerSystem.GetRealmTotalManpower(5, tiles, realms), 0.001f,
                "旧无 day 重载保持乘子 1（兼容）");
        }

        // ===================== 12. B3：catalog 可切换（mod 扩展点） =====================

        [Test]
        public void Catalog_CanBeSwitchedForMods()
        {
            var realm = NewRealm(8);
            realm.lawState.SetActiveLaw("custom", "custom_tax", 1);
            var customGroups = new List<LawGroupDef>
            {
                new LawGroupDef
                {
                    groupId = "custom",
                    name = "自定义",
                    order = 1,
                    laws = new List<LawDef>
                    {
                        new LawDef
                        {
                            lawId = "custom_tax",
                            groupId = "custom",
                            name = "自定义税",
                            effects = new LawEffect { taxMultiplier = 2f }
                        }
                    }
                }
            };

            try
            {
                Assert.AreEqual(1f, LawEffectRuntime.GetTaxMultiplier(realm, 1), 0.001f,
                    "默认目录不认识自定义 lawId → 中性乘子");
                LawEffectRuntime.SetCatalogs(customGroups, DefaultLawCatalog.CreateDecrees());
                Assert.IsTrue(LawEffectRuntime.CurrentLaws.ContainsKey("custom_tax"), "切换后目录应包含自定义法律");
                Assert.AreEqual(2f, LawEffectRuntime.GetTaxMultiplier(realm, 1), 0.001f, "切换目录后自定义法律生效");
            }
            finally
            {
                LawEffectRuntime.ResetCatalogs();
            }

            Assert.AreEqual(1f, LawEffectRuntime.GetTaxMultiplier(realm, 1), 0.001f, "Reset 后恢复默认目录");
            Assert.IsFalse(LawEffectRuntime.CurrentLaws.ContainsKey("custom_tax"));
        }

        // ===================== 13. B4：unrest 持久字段随 RealmDTO 往返 =====================

        [Test]
        public void RealmDto_RoundTrip_PreservesUnrest()
        {
            var realm = NewRealm(7);
            realm.unrest = 42f;
            realm.stability = 66f;

            RealmDTO dto = RealmDTO.FromRealmData(realm);
            Assert.AreEqual(42f, dto.unrest, 0.001f, "RealmDTO 捕获 unrest");
            string json = JsonUtility.ToJson(dto);
            RealmData restored = JsonUtility.FromJson<RealmDTO>(json).ToRealmData();

            Assert.AreEqual(42f, restored.unrest, 0.001f, "旧档/读档 unrest 往返保留");
            Assert.AreEqual(66f, restored.stability, 0.001f);
        }
    }
}
