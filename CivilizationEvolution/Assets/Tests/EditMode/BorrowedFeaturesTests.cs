using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 借鉴《地图上发生的事》功能落地测试：WarRules 战争规则 / 停战机制 /
    /// 编年史 / 家族故国与代数标记
    /// </summary>
    public class BorrowedFeaturesTests
    {
        // ===== WarRules 战争规则参数 =====

        [Test]
        public void WarRules_DefaultParameters()
        {
            var rules = WarRules.Default();
            Assert.AreEqual(5, rules.truceYears, "默认停战 5 年");
            Assert.IsTrue(rules.allowWhitePeace, "允许白和");
            Assert.IsTrue(rules.allowAllianceIntervention, "允许联盟介入");
            Assert.IsTrue(rules.allowVassalObligation, "允许封臣义务");
            Assert.Greater(rules.scoreCapital, rules.scoreCity, "首都分高于城市分");
            Assert.Greater(rules.scoreCity, rules.scoreProvince, "城市分高于省份分");
        }

        [Test]
        public void WarRules_TruceUntilDay_Calculates()
        {
            var rules = WarRules.Default();
            Assert.AreEqual(365 * 5 + 1000, rules.GetTruceUntilDay(1000, 5), "停战到期日=当前日+年限×365");
        }

        // ===== 停战机制（truce_until_tick 借鉴） =====

        [Test]
        public void Truce_BlocksWarUntilExpiry()
        {
            var realms = new Dictionary<int, RealmData>();
            for (int i = 0; i < 2; i++)
                realms[i] = new RealmData { realmId = i, realmName = "国" + i };
            var dm = new DiplomacyManager(realms);
            dm.CurrentDay = 1000;

            // 宣战 → 和平（停战 5 年 = 至 1000+1825）
            Assert.IsTrue(dm.DeclareWar(0, 1, "边境冲突"));
            Assert.IsNotNull(dm.OfferPeace(0, 1, 0, 0), "应可求和");

            // 停战期内再宣战 → 拒绝
            Assert.IsFalse(dm.DeclareWar(0, 1, "再战"), "停战期内应不可宣战");

            // 停战期满后可宣战
            dm.CurrentDay = 1000 + 5 * 365 + 1;
            Assert.IsTrue(dm.DeclareWar(0, 1, "停战期满再战"), "停战期满应可宣战");
        }

        // ===== 编年史（Chronicle 借鉴） =====

        [Test]
        public void Chronicle_RecordsAndFilters()
        {
            var chronicle = new Chronicle { CurrentTick = 100, CurrentYear = 3 };
            chronicle.Add("war", "甲国对乙国宣战", major: true, 1, 2);
            chronicle.Add("alliance", "丙丁结盟");
            chronicle.Add("peace", "甲乙停战", major: true, 1, 2);

            Assert.AreEqual(3, chronicle.Count);
            Assert.AreEqual(2, chronicle.GetMajorEntries().Count, "重大事件过滤");
            Assert.AreEqual(2, chronicle.GetEntriesByType("war").Count
                + chronicle.GetEntriesByType("peace").Count - 1 + 1, "类型过滤");
            Assert.AreEqual(3, chronicle.GetEntries()[0].year, "条目带时间");
        }

        [Test]
        public void Chronicle_RollsOverAtLimit()
        {
            var chronicle = new Chronicle();
            for (int i = 0; i < 550; i++)
                chronicle.Add("tick", "第" + i + "条");
            Assert.AreEqual(500, chronicle.Count, "滚动保留上限 500");
        }

        // ===== 家族故国 + 代数标记（homeland_country/generation_marks 借鉴） =====

        [Test]
        public void Family_HomelandAndGenerationMarks()
        {
            var manager = new CharacterManager();
            var ruler = manager.CreateCharacter("祖", "氏", 30, true, 0, 0, 0, CharacterRole.Ruler);
            var family = manager.CreateFamily("故国氏", ruler.characterId, 1, realmId: 3);

            family.homelandCountryId = 3; // 故国
            family.generationMarks.Add("gen_mark_first");
            family.generationMarks.Add("gen_mark_second");

            Assert.AreEqual(3, family.homelandCountryId, "家族故国");
            Assert.AreEqual(2, family.generationMarks.Count, "代数标记");
            Assert.AreEqual(3, family.holderRealmId, "所属政权");
        }
    }
}
