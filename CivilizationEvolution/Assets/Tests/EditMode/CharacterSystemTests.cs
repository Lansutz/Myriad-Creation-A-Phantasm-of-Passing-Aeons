using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 角色数值体系 EditMode 测试（企划书第九篇：容量型/上限型数值、人格七维、精神疾病、饮食联动）
    /// </summary>
    public class CharacterSystemTests
    {
        // ===== 统治类型判定（企划书 9.1：威望/恶名 → 明君/暴君/昏暴/平庸） =====

        [Test]
        public void RulerType_Benevolent_HighPrestigeLowNotoriety()
        {
            var c = MakeRuler();
            c.prestige = 80f;   // 容量 100 的 80%
            c.notoriety = 10f;
            Assert.AreEqual(RulerType.Benevolent, c.GetRulerType());
        }

        [Test]
        public void RulerType_Tyrant_HighNotorietyLowPrestige()
        {
            var c = MakeRuler();
            c.prestige = 10f;
            c.notoriety = 80f;
            Assert.AreEqual(RulerType.Tyrant, c.GetRulerType());
        }

        [Test]
        public void RulerType_TyrantFool_BothHigh()
        {
            var c = MakeRuler();
            c.prestige = 80f;
            c.notoriety = 80f;
            Assert.AreEqual(RulerType.TyrantFool, c.GetRulerType());
        }

        [Test]
        public void RulerType_Mediocre_BothLow()
        {
            var c = MakeRuler();
            c.prestige = 10f;
            c.notoriety = 10f;
            Assert.AreEqual(RulerType.Mediocre, c.GetRulerType());
        }

        // ===== 威望容量等级（当前值 + 容量等级 + 上限） =====

        [Test]
        public void PrestigeCapacity_UpgradeOnCap()
        {
            var c = MakeRuler();
            Assert.AreEqual(100f, c.GetPrestigeCapacity(), 0.001f);
            c.ModifyPrestige(150f);
            Assert.AreEqual(2, c.prestigeCapacityLevel, "达上限应升 2 级");
            Assert.AreEqual(300f, c.GetPrestigeCapacity(), 0.001f);
            Assert.AreEqual(100f, c.prestige, 0.001f, "升级时当前值保持旧上限");
        }

        [Test]
        public void PrestigeCapacity_DowngradeBelowThirtyPercent()
        {
            var c = MakeRuler();
            c.prestigeCapacityLevel = 3; // 上限 600
            c.prestige = 100f;            // < 600×0.3=180
            c.ModifyPrestige(0f);
            Assert.AreEqual(2, c.prestigeCapacityLevel, "低于 30% 应降级");
        }

        // ===== 人格七维家族遗传基线（企划书 9.3） =====

        [Test]
        public void Personality_InheritedFromParents_WithinTolerance()
        {
            var manager = new CharacterManager();
            var father = manager.CreateCharacter("父", "氏", 30, true, 0, 0, 0, CharacterRole.Noble);
            var mother = manager.CreateCharacter("母", "氏", 28, false, 0, 0, 0, CharacterRole.Noble);
            father.honor = 80f; mother.honor = 60f;      // 均值 70
            father.greed = -40f; mother.greed = -20f;    // 均值 -30

            var child = manager.CreateCharacter("子", "氏", 0, true, 0, 0, 0, CharacterRole.Commoner,
                null, father.characterId, mother.characterId);

            Assert.That(child.honor, Is.InRange(59f, 81f), "荣誉应接近双亲均值 70 ±10");
            Assert.That(child.greed, Is.InRange(-41f, -19f), "贪婪应接近双亲均值 -30 ±10");
        }

        [Test]
        public void Personality_Description_NonEmptyAndSceneBased()
        {
            var c = MakeRuler();
            c.honor = 90f;
            c.rationality = 70f;
            string desc = c.GetPersonalityDescription();
            Assert.IsFalse(string.IsNullOrEmpty(desc));
            StringAssert.Contains("为人", desc, "应为写实场景化描述而非标签");
            StringAssert.DoesNotContain("四字", desc);
        }

        // ===== 精神疾病（简单版：长期高压触发） =====

        [Test]
        public void MentalDisorder_HighStressNinetyDays_Triggers()
        {
            var manager = new CharacterManager();
            var c = manager.CreateCharacter("高压", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            c.stress = 90f;

            for (int day = 1; day <= 90; day++)
                manager.DailyTick(day, 1);

            Assert.AreNotEqual(MentalDisorderType.None, c.mentalDisorder, "压力>80 持续 90 天应触发精神疾病");
            Assert.IsTrue(c.mentalDisorder == MentalDisorderType.Depression || c.mentalDisorder == MentalDisorderType.Anxiety,
                "高压触发应为抑郁或焦虑");
        }

        [Test]
        public void MentalDisorder_RecoversWhenStressLow()
        {
            var manager = new CharacterManager();
            var c = manager.CreateCharacter("康复", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            c.mentalDisorder = MentalDisorderType.Depression;
            c.stress = 10f;

            for (int day = 1; day <= 130; day++)
                manager.DailyTick(day, 1);

            Assert.AreEqual(MentalDisorderType.None, c.mentalDisorder, "压力<30 持续 120 天应康复");
        }

        [Test]
        public void MentalDisorder_DementiaIrreversible()
        {
            var manager = new CharacterManager();
            var c = manager.CreateCharacter("老朽", "氏", 80, true, 0, 0, 0, CharacterRole.Ruler);
            c.mentalDisorder = MentalDisorderType.Dementia;
            c.stress = 10f;

            for (int day = 1; day <= 200; day++)
                manager.DailyTick(day, 1);

            Assert.AreEqual(MentalDisorderType.Dementia, c.mentalDisorder, "失智不可逆");
        }

        // ===== 饮食联动（缺粮 → 压力上升 + 肥胖下降） =====

        [Test]
        public void Diet_FamineRaisesStressAndLowersObesity()
        {
            var manager = new CharacterManager();
            var c = manager.CreateCharacter("饥民", "氏", 30, true, 0, 0, 0, CharacterRole.Commoner);
            c.realmId = 0;
            c.stress = 0f;
            c.obesity = 50f;

            // 最小经济注入：政权 0 核心地块 0（region 0）的贸易中心无粮
            var tiles = new TileData[64];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = new TileData { tileIndex = i, regionId = i / 16, exists = true };
            var tcs = new Dictionary<int, TradeCenter> { [0] = new TradeCenter { regionId = 0, inventory = new Dictionary<int, float>() } };
            var economy = new EconomyManager(tiles, tcs, new Dictionary<int, GoodsDef>(), new CurrencySystem(), new TaxSystem());
            var realm = new RealmData { realmId = 0 };
            realm.coreTiles.Add(0);
            var realms = new Dictionary<int, RealmData> { [0] = realm };

            manager.Economy = economy;
            manager.Tiles = tiles;
            manager.Realms = realms;

            manager.DailyTick(1, 1); // 一天：缺粮 → 压力+2，肥胖-0.03

            Assert.That(c.stress, Is.GreaterThan(1f), "缺粮应显著提高压力");
            Assert.That(c.obesity, Is.LessThan(50f), "缺粮应降低肥胖");
        }

        // ===== 辅助 =====

        private static CharacterData MakeRuler()
        {
            return new CharacterData
            {
                characterId = 1,
                firstName = "T",
                lastName = "C",
                role = CharacterRole.Ruler
            };
        }
    }
}
