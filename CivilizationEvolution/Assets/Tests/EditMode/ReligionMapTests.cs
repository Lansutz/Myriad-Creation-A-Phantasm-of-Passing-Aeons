using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 宗教谱系测试（双维度模型：组织父×学派父 / 节点类型 / 教统 / 分支传统）
    /// </summary>
    public class ReligionMapTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        [Test]
        public void ReligionCatalog_LoadsCompleteHierarchy()
        {
            Assert.IsTrue(ContentRegistry.Religions.Count >= 20, "宗教数据完整加载");

            // 三大宗教根
            Assert.IsTrue(ReligionCatalog.Get(100).IsRoot, "基督教=根");
            Assert.IsTrue(ReligionCatalog.Get(200).IsRoot, "伊斯兰教=根");
            Assert.IsTrue(ReligionCatalog.Get(300).IsRoot, "佛教=根");

            // 基督教三支平级（早期分裂）
            Assert.AreEqual(100, ReligionCatalog.Get(101).parentReligionId, "迦克墩派=基督教直接子");
            Assert.AreEqual(100, ReligionCatalog.Get(102).parentReligionId, "东方正统教会=基督教直接子（非迦克墩——平级）");
            Assert.AreEqual(100, ReligionCatalog.Get(103).parentReligionId, "亚述教会=基督教直接子");
        }

        [Test]
        public void SchoolParent_TwoDimensionModel()
        {
            // 亚述教会：组织父=基督教、学派父=塞琉西亚-泰西封（狄奥多若——与聂斯托里无关）
            var assyrian = ReligionCatalog.Get(103);
            Assert.AreEqual(100, assyrian.parentReligionId, "组织父=基督教");
            Assert.AreEqual(112, assyrian.schoolParentId, "学派父=塞琉西亚-泰西封学派");

            var theo = ReligionCatalog.Get(112);
            Assert.AreEqual(ReligionNodeType.School, theo.nodeType, "塞琉西亚-泰西封=学派");
            Assert.AreEqual(111, theo.schoolParentId, "塞琉西亚-泰西封←安提阿学派");

            // 科普特教会←亚历山大学派
            var coptic = ReligionCatalog.Get(120);
            Assert.AreEqual(110, coptic.schoolParentId, "科普特←亚历山大学派");
            Assert.AreEqual("亚历山大礼", coptic.PrimaryRite, "科普特=亚历山大礼");

            // 聂斯托里=无教统学派（与亚述教会无关——独立思想遗产）
            var nestorius = ReligionCatalog.Get(115);
            Assert.AreEqual(ReligionNodeType.School, nestorius.nodeType, "聂斯托里=学派");
            Assert.IsFalse(nestorius.hasSuccession, "聂斯托里无教统");
        }

        [Test]
        public void Sect_HasSuccession_SchoolNot()
        {
            Assert.IsTrue(ReligionCatalog.Get(104).hasSuccession, "罗马公教会有教统（主教链）");
            Assert.IsTrue(ReligionCatalog.Get(120).hasSuccession, "科普特教会有教统");
            Assert.IsFalse(ReligionCatalog.Get(110).hasSuccession, "亚历山大学派无教统（前形态）");
            Assert.IsFalse(ReligionCatalog.Get(111).hasSuccession, "安提阿学派无教统");
        }

        [Test]
        public void BranchTraditions_ArbitraryDepth()
        {
            // 佛教：大乘→法华宗→日莲宗（深度 3——分支传统）
            var nichiren = ReligionCatalog.Get(307);
            Assert.AreEqual(304, nichiren.parentReligionId, "日莲宗←法华宗");
            Assert.AreEqual(ReligionNodeType.Tradition, nichiren.nodeType, "日莲宗=传统");

            // 禅宗→灭喜禅（越南传播——深度 3）
            var thien = ReligionCatalog.Get(308);
            Assert.AreEqual(305, thien.parentReligionId, "灭喜禅←禅宗");
            Assert.AreEqual("毗尼多流支传承", thien.school, "灭喜禅学派传承");

            // 宗派/传统/学派类型正确
            Assert.AreEqual(ReligionNodeType.Sect, ReligionCatalog.Get(302).nodeType, "大乘=宗派");
            Assert.AreEqual(ReligionNodeType.Tradition, ReligionCatalog.Get(305).nodeType, "禅宗=传统");
        }

        [Test]
        public void ReligionCatalog_Colors_ThreeLevels()
        {
            ReligionCatalog.EnsureColors();

            var rootColor = ReligionCatalog.GetColor(100, ReligionMapLevel.Religion);
            var sectColor = ReligionCatalog.GetColor(120, ReligionMapLevel.Sect);
            var tradColor = ReligionCatalog.GetColor(120, ReligionMapLevel.Tradition);

            Assert.AreEqual(rootColor, ReligionCatalog.GetColor(120, ReligionMapLevel.Religion),
                "宗派在宗教级显示根色（宗教=基督教）");
            Assert.AreNotEqual(Color.gray, sectColor, "宗派色非灰");
            Assert.AreNotEqual(sectColor, tradColor, "传统级礼色相偏移");

            // 深层裂教宗派显示自身色（宗派就是宗派）
            var catholicColor = ReligionCatalog.GetColor(104, ReligionMapLevel.Sect);
            Assert.AreEqual(ReligionCatalog.Get(104).color, catholicColor, "深层宗派显示自身色");
            Assert.AreNotEqual(ReligionCatalog.GetColor(104, ReligionMapLevel.Religion), catholicColor,
                "宗派色≠宗教根色");
        }
    }
}
