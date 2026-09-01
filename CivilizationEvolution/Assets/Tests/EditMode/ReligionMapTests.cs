using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 宗教三级谱系与文化分支测试（地图模式数据层）
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
        public void ReligionCatalog_LoadsThreeTierHierarchy()
        {
            Assert.IsTrue(ContentRegistry.Religions.Count >= 3, "宗教数据已加载");
            Assert.IsNotNull(ReligionCatalog.Get(100), "宗教根存在");
            Assert.IsNotNull(ReligionCatalog.Get(101), "宗派存在");

            var root = ReligionCatalog.Get(100);
            Assert.IsTrue(root.IsRoot, "100 是宗教根");

            var sect = ReligionCatalog.Get(101);
            Assert.IsFalse(sect.IsRoot, "101 是宗派");
            Assert.AreEqual(100, sect.parentReligionId, "宗派从属宗教");
            Assert.IsNotEmpty(sect.rite, "宗派有礼拜仪轨");
            Assert.IsNotEmpty(sect.school, "宗派有教义学派");
        }

        [Test]
        public void ReligionCatalog_GetRoot_TracesToRoot()
        {
            var root = ReligionCatalog.GetRoot(201); // 主流河神派 → 河神信仰
            Assert.IsNotNull(root);
            Assert.AreEqual(200, root.religionId, "宗派上溯到宗教根");
        }

        [Test]
        public void ReligionCatalog_Colors_ThreeLevels()
        {
            ReligionCatalog.EnsureColors();

            var rootColor = ReligionCatalog.GetColor(100, ReligionMapLevel.Religion);
            var sectColor = ReligionCatalog.GetColor(101, ReligionMapLevel.Sect);
            var tradColor = ReligionCatalog.GetColor(101, ReligionMapLevel.Tradition);

            Assert.AreEqual(rootColor, ReligionCatalog.GetColor(101, ReligionMapLevel.Religion),
                "宗派在宗教级显示根色");
            Assert.AreNotEqual(Color.gray, sectColor, "宗派色非灰（自动分配）");
            Assert.AreNotEqual(sectColor, tradColor, "传统级色相偏移（同宗派内区分传统）");
        }

        [Test]
        public void CultureBranch_AllowsBranchingFlag()
        {
            // 默认允许分支（主文化）
            var culture = new CultureData { cultureId = 1, cultureName = "测试文化" };
            Assert.IsTrue(culture.allowsBranching, "默认允许分支");

            // 分支文化挂接
            culture.childCultureIds.Add(2);
            var branch = new CultureData { cultureId = 2, cultureName = "分支", parentCultureId = 1 };
            Assert.AreEqual(1, branch.parentCultureId, "分支指向父文化");

            // 不允许分支的文化（如封闭文化）
            culture.allowsBranching = false;
            Assert.IsFalse(culture.allowsBranching, "可关闭分支");
        }
    }
}
