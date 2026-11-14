using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P9/M2：建筑、单位、装备、配方、货物都必须有革新门槛，且门槛 id 能在革新目录解析。
    /// JSON 是唯一来源；这里直接读随包内容，不复制一份基准。
    /// </summary>
    public class ContentInnovationGateAuditTests
    {
        [Serializable]
        private class BuildingWrapper { public List<BuildingContentDef> buildings = new List<BuildingContentDef>(); }

        [Serializable]
        private class UnitGate { public int unitId; public string unitName; public List<int> requiredInnovations = new List<int>(); }

        [Serializable]
        private class UnitWrapper { public List<UnitGate> units = new List<UnitGate>(); }

        [Serializable]
        private class RecipeGate { public string recipeId; public int requiredInnovation; }

        [Serializable]
        private class RecipeWrapper { public List<RecipeGate> recipes = new List<RecipeGate>(); }

        [Serializable]
        private class ReligionGate { public int religionId; public string religionName; public List<int> requiredInnovations = new List<int>(); }

        [Serializable]
        private class ReligionWrapper { public List<ReligionGate> religions = new List<ReligionGate>(); }

        [Serializable]
        private class GoodsGate { public int goodsId; public string goodsName; public int requiredInnovation; }

        [Serializable]
        private class GoodsWrapper { public List<GoodsGate> goods = new List<GoodsGate>(); }

        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
        }

        private static string BasePath(string relative)
        {
            return Path.Combine(Application.dataPath, "StreamingAssets/Base/" + relative);
        }

        private static void AssertResolves(List<string> problems, string owner, int innovationId)
        {
            if (innovationId <= 0)
            {
                problems.Add(owner + " 缺革新门槛");
                return;
            }
            if (!ContentRegistry.Innovations.ContainsKey(innovationId))
            {
                problems.Add(owner + " 引用不存在的革新 " + innovationId);
            }
        }

        [Test]
        public void Buildings_AllHaveResolvableGate()
        {
            string path = BasePath("Building/Buildings.json");
            Assert.IsTrue(File.Exists(path), path);
            var wrapper = JsonUtility.FromJson<BuildingWrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            var problems = new List<string>();
            foreach (var def in wrapper.buildings)
            {
                AssertResolves(problems, "建筑 " + def.buildingId + " " + def.buildingName, def.requiredInnovation);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Units_AllHaveResolvableGate()
        {
            string path = BasePath("Unit/Units.json");
            var wrapper = JsonUtility.FromJson<UnitWrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            var problems = new List<string>();
            foreach (var def in wrapper.units)
            {
                if (def.requiredInnovations == null || def.requiredInnovations.Count == 0)
                {
                    problems.Add("单位 " + def.unitId + " " + def.unitName + " 缺革新门槛");
                    continue;
                }
                foreach (int id in def.requiredInnovations) AssertResolves(problems, "单位 " + def.unitId, id);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Recipes_AllHaveResolvableGate()
        {
            string path = BasePath("Crafting/Recipes.json");
            var wrapper = JsonUtility.FromJson<RecipeWrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            var problems = new List<string>();
            foreach (var def in wrapper.recipes)
            {
                AssertResolves(problems, "配方 " + def.recipeId, def.requiredInnovation);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Goods_AllHaveResolvableGate()
        {
            string path = BasePath("Goods/Goods.json");
            var wrapper = JsonUtility.FromJson<GoodsWrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            var problems = new List<string>();
            foreach (var def in wrapper.goods)
            {
                // 原料（小麦/石料/矿石等）不依赖革新：-1/0 是显式默认可用。
                // 加工品若声明门槛，则必须能解析。
                if (def.requiredInnovation > 0)
                {
                    AssertResolves(problems, "货物 " + def.goodsId + " " + def.goodsName, def.requiredInnovation);
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void Religions_AllHaveResolvableGate()
        {
            string path = BasePath("Religion/Religions.json");
            var wrapper = JsonUtility.FromJson<ReligionWrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            var problems = new List<string>();
            foreach (var def in wrapper.religions)
            {
                // 600/601 是默认民间信仰层：世界开始时人口与政权就要能持有它，故显式不设门槛。
                if (def.religionId == 600 || def.religionId == 601) continue;
                if (def.requiredInnovations == null || def.requiredInnovations.Count == 0)
                {
                    problems.Add("宗教 " + def.religionId + " " + def.religionName + " 缺革新门槛");
                    continue;
                }
                foreach (int id in def.requiredInnovations) AssertResolves(problems, "宗教 " + def.religionId, id);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void BuildingAvailability_ChangesWithInnovation()
        {
            var tiles = new[] { new TileData { exists = true, isLand = true, ownerRealmId = 1 } };
            var system = new BuildingSystem(tiles) { Innovations = new InnovationTree() };
            var realm = new RealmData { realmId = 1, treasury = 10000f };

            Assert.IsFalse(system.CanBuildBuilding(0, 100, 1, realm), "未掌握革新时建筑应锁定");
            system.Innovations.GrantInnovation(1, 100);
            Assert.IsTrue(system.CanBuildBuilding(0, 100, 1, realm), "掌握后建筑应可选");
        }

        [Test]
        public void Equipment_AllHaveResolvableGate()
        {
            var problems = new List<string>();
            foreach (var pair in ContentRegistry.Equipment)
            {
                AssertResolves(problems, "装备 " + pair.Key, pair.Value.requiredInnovation);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
