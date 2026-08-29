using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 继承法系统 EditMode 测试（用户定稿四轴：范围/支系/性别/长幼）
    /// 经典组合：长子继承/幼子守灶/兄终弟及/母系继承/绝对均分
    /// </summary>
    public class InheritanceLawTests
    {
        private static CharacterData MakeChar(int id, int age, bool isMale, int familyId, int fatherId = -1)
        {
            return new CharacterData
            {
                characterId = id,
                firstName = "C" + id,
                lastName = "氏",
                age = age,
                isMale = isMale,
                familyId = familyId,
                fatherId = fatherId
                // isAlive 为只读（deathDay < 0 推导），默认存活
            };
        }

        // ===== 长子继承（血亲+长支+男子优先+年长） =====

        [Test]
        public void Primogeniture_EldestMaleWins()
        {
            var ruler = MakeChar(1, 60, true, 10);
            var eldestSon = MakeChar(2, 35, true, 10);
            var youngerSon = MakeChar(3, 30, true, 10);
            var daughter = MakeChar(4, 40, false, 10); // 女子年长但男子优先

            var law = InheritanceLaw.Primogeniture();
            var heir = law.DetermineHeir(new List<CharacterData> { youngerSon, daughter, eldestSon }, ruler);

            Assert.AreEqual(eldestSon.characterId, heir.characterId, "长子继承：男子优先中年长男子胜出");
        }

        // ===== 幼子守灶（年幼先） =====

        [Test]
        public void Ultimogeniture_YoungestWins()
        {
            var ruler = MakeChar(1, 60, true, 10);
            var eldest = MakeChar(2, 35, true, 10);
            var youngest = MakeChar(3, 20, true, 10);

            var law = InheritanceLaw.Ultimogeniture();
            var heir = law.DetermineHeir(new List<CharacterData> { eldest, youngest }, ruler);

            Assert.AreEqual(youngest.characterId, heir.characterId, "幼子守灶：年幼者先");
        }

        // ===== 兄终弟及（横向：同辈兄弟优先于子侄） =====

        [Test]
        public void Tanistry_BrotherBeforeSon()
        {
            var ruler = MakeChar(1, 60, true, 10);
            var son = MakeChar(2, 25, true, 10, fatherId: 1);
            var youngerBrother = MakeChar(3, 45, true, 10, fatherId: 50); // 与 ruler 共享父 50

            var law = InheritanceLaw.Tanistry();
            var heir = law.DetermineHeir(new List<CharacterData> { son, youngerBrother }, ruler);

            Assert.AreEqual(youngerBrother.characterId, heir.characterId, "兄终弟及：同辈兄弟优先于子侄");
        }

        // ===== 母系继承（女子专属） =====

        [Test]
        public void Matrilineal_FemaleOnly()
        {
            var ruler = MakeChar(1, 60, true, 10);
            var male = MakeChar(2, 50, true, 10);
            var female = MakeChar(3, 40, false, 10);

            var law = InheritanceLaw.Matrilineal();
            var heir = law.DetermineHeir(new List<CharacterData> { male, female }, ruler);

            Assert.AreEqual(female.characterId, heir.characterId, "母系继承：男子无继承权");
        }

        // ===== 范围轴：限本族（ClanOnly）排除外族 =====

        [Test]
        public void ClanOnly_ExcludesOtherFamilies()
        {
            var ruler = MakeChar(1, 60, true, 10);
            var clanHeir = MakeChar(2, 35, true, 10);
            var outsider = MakeChar(3, 70, true, 99); // 外族且年长

            var law = InheritanceLaw.Ultimogeniture(); // 幼子守灶=限本族
            var heir = law.DetermineHeir(new List<CharacterData> { outsider, clanHeir }, ruler);

            Assert.AreEqual(clanHeir.characterId, heir.characterId, "限本族：外族候选人应被排除");
        }

        // ===== 男子专属且无男性 → 无继承人 =====

        [Test]
        public void MaleOnly_NoMale_ReturnsNull()
        {
            var ruler = MakeChar(1, 60, true, 10);
            var females = new List<CharacterData> { MakeChar(2, 40, false, 10), MakeChar(3, 35, false, 10) };

            var law = InheritanceLaw.Tanistry(); // 男子专属
            Assert.IsNull(law.DetermineHeir(females, ruler), "男子专属且无男性候选人时应无继承人");
        }

        // ===== 男女平等：纯按年龄 =====

        [Test]
        public void EqualGender_AgeDecides()
        {
            var ruler = MakeChar(1, 60, true, 10);
            var olderFemale = MakeChar(2, 45, false, 10);
            var youngerMale = MakeChar(3, 30, true, 10);

            var law = new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.Equal, InheritanceAge.Seniority);
            var heir = law.DetermineHeir(new List<CharacterData> { youngerMale, olderFemale }, ruler);

            Assert.AreEqual(olderFemale.characterId, heir.characterId, "男女平等：年长者胜出（不分性别）");
        }

        // ===== 继承法名称 =====

        [Test]
        public void InheritanceLaw_Name_ReflectsAxes()
        {
            StringAssert.Contains("血亲", InheritanceLaw.Primogeniture().GetName());
            StringAssert.Contains("长支", InheritanceLaw.Primogeniture().GetName());
            StringAssert.Contains("男子优先", InheritanceLaw.Primogeniture().GetName());
            StringAssert.Contains("兄终弟及", InheritanceLaw.Tanistry().GetName());
            StringAssert.Contains("女子专属", InheritanceLaw.Matrilineal().GetName());
            StringAssert.Contains("年幼先", InheritanceLaw.Ultimogeniture().GetName());
        }
    }
}
