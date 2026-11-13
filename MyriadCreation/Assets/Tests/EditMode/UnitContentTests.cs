using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.4：兵种表外提后的完整性与转换。</summary>
    public class UnitContentTests
    {
        [Serializable]
        private class Wrapper
        {
            public List<UnitContentDef> units = new List<UnitContentDef>();
        }

        [TearDown]
        public void TearDown()
        {
            UnitCatalog.ResetContent();
        }

        [Test]
        public void ShippedUnits_AreCompleteAndUnique()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets/Base/Unit/Units.json");
            Assert.IsTrue(File.Exists(path), "找不到 Units.json: " + path);
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            Assert.AreEqual(14, wrapper.units.Count);

            var ids = new HashSet<int>();
            foreach (UnitContentDef def in wrapper.units)
            {
                Assert.Greater(def.unitId, 0);
                Assert.IsTrue(ids.Add(def.unitId), "重复兵种 id: " + def.unitId);
                Assert.IsFalse(string.IsNullOrEmpty(def.unitName));
                Assert.IsTrue(Enum.IsDefined(typeof(GameEnums.UnitArmType), def.category));
                Assert.Greater(def.tier, 0);
                Assert.Greater(def.manpowerCost, 0f);
                Assert.Greater(def.recruitCost.Count, 0, "兵种应有招募物资: " + def.unitId);
            }
        }

        [Test]
        public void LoadFromContent_ConvertsCategoryCostsAndInnovations()
        {
            UnitCatalog.LoadFromContent(new List<UnitContentDef>
            {
                new UnitContentDef
                {
                    unitId = 100,
                    unitName = "外提步兵",
                    category = (int)GameEnums.UnitArmType.Cavalry,
                    tier = 2,
                    meleeAttack = 11f,
                    rangedAttack = 2f,
                    defense = 9f,
                    morale = 61f,
                    speed = 3f,
                    supplyConsumption = 1.5f,
                    manpowerCost = 55f,
                    recruitCost = new List<UnitCostDef>
                    {
                        new UnitCostDef { goodsId = 70, amount = 1.2f }
                    },
                    requiredInnovations = new List<int> { 202 }
                }
            });

            Assert.IsTrue(UnitCatalog.IsLoaded);
            UnitDef def = UnitCatalog.All[100];
            Assert.AreEqual("外提步兵", def.unitName);
            Assert.AreEqual(GameEnums.UnitArmType.Cavalry, def.category);
            Assert.AreEqual(55f, def.manpowerCost, 0.001f);
            Assert.AreEqual(1.2f, def.recruitCost[70], 0.001f);
            CollectionAssert.Contains(def.requiredInnovations, 202);
            Assert.AreEqual(0, def.terrainModifiers.Count);
        }
    }
}
