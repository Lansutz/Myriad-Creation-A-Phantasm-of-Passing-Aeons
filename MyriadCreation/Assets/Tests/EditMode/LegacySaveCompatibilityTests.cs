using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 旧档兼容（审计 A-P1-7 的最小验证）：手工构造 v11 之前的存档 JSON
    /// （缺 provinces/habitationSites/armies/innovations/characters 等后加字段），
    /// 读档不应抛异常，缺失的表按空世界处理；版本号不一致只警告不拒绝。
    /// </summary>
    public class LegacySaveCompatibilityTests
    {
        private const string SaveName = "__legacy_compat_test";
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            MapUnitRegistry.Clear();
            CharacterKnowledgeRegistry.Clear();
            RelationRegistry.Clear();
            ContractRegistry.Clear();
            SaveSystem.DeleteSave(SaveName);
        }

        private static void WriteRawSave(string json)
        {
            string path = Path.Combine(SaveSystem.GetSaveDirectory(), SaveName + ".sav");
            Directory.CreateDirectory(SaveSystem.GetSaveDirectory());
            File.WriteAllText(path, json);
        }

        /// <summary>v11 时代的存档：只有地图/基础字段，没有后来的省份/聚落/军队/革新/角色段。</summary>
        private const string LegacyV11Json =
            "{\"version\":11,\"mapWidth\":4,\"mapHeight\":1,\"currentYear\":100,\"currentDay\":5," +
            "\"tiles\":[" +
            "{\"tileIndex\":0,\"exists\":true,\"isLand\":true,\"elevation01\":0.3}," +
            "{\"tileIndex\":1,\"exists\":true,\"isLand\":true,\"elevation01\":0.4}," +
            "{\"tileIndex\":2,\"exists\":true,\"isLand\":true,\"elevation01\":0.5}," +
            "{\"tileIndex\":3,\"exists\":true,\"isLand\":true,\"elevation01\":0.6}]}";

        [Test]
        public void LegacyV11Save_LoadsWithoutNewSections()
        {
            WriteRawSave(LegacyV11Json);

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "旧档（v11）应能读入，缺失段按空世界处理");
            _objects.Add(loaded.gameObject);

            Assert.AreEqual(4, loaded.tiles.Length, "地块数");
            Assert.AreEqual(100, loaded.currentYear, "年份");
            Assert.AreEqual(5, loaded.currentDay, "日");
            Assert.AreEqual(0, loaded.provinces.Count, "旧档无省份段");
            Assert.AreEqual(0, loaded.settlements.Count, "旧档无聚落段");
            Assert.AreEqual(0, loaded.armies.Count, "旧档无军队段");
            Assert.Greater(loaded.unitDefs.Count, 0, "读档应补齐兵种定义（PrepareLoadedWorld）");
        }

        [Test]
        public void MismatchedVersion_WarnsButStillLoads()
        {
            string json = LegacyV11Json.Replace("\"version\":11", "\"version\":99");
            WriteRawSave(json);

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "版本号不一致应只警告、仍尝试兼容加载");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(4, loaded.tiles.Length);
        }

        [Test]
        public void GarbageJson_ReturnsNull_AndDoesNotCrash()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            WriteRawSave("{ this is not valid json ]");

            GameWorld loaded = null;
            Assert.DoesNotThrow(() => loaded = SaveSystem.LoadGame(SaveName));
            Assert.IsNull(loaded, "非法 JSON 应返回 null");
        }
    }
}
