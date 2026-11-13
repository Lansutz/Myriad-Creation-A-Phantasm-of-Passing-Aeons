using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Content.Modding;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P4.4 生态物种表：内容加载/Mod 覆盖；承载与捕猎受栖息地物种性状影响。
    /// </summary>
    public class EcologySpeciesTests
    {
        [TearDown]
        public void TearDown()
        {
            EcologySpeciesCatalog.ResetContent();
        }

        private static void LoadRealContent()
        {
            var index = new ContentDefinitionIndex();
            index.Initialize(Application.streamingAssetsPath, ContentDefinitionIndex.InnovationFallbackGroups.Default);
            EcologySpeciesCatalog.LoadFromContent(index.EcologySpecies.Values);
        }

        private static TileData Tile(GameEnums.BiomeType biome, float npp = 0.8f)
        {
            return new TileData
            {
                exists = true,
                isLand = true,
                biome = biome,
                npp = npp,
                vegetation = 0.8f,
                wildlife = 0.3f,
                populationBlocks = new List<PopulationBlock>()
            };
        }

        [Test]
        public void RealContent_LoadsAndClassifiesHabitat()
        {
            LoadRealContent();

            Assert.IsTrue(EcologySpeciesCatalog.IsLoaded);
            Assert.AreEqual("forest", EcosystemSystem.HabitatOf(Tile(GameEnums.BiomeType.DeciduousForest)));
            Assert.AreEqual("arid", EcosystemSystem.HabitatOf(Tile(GameEnums.BiomeType.HotDesert)));
            Assert.AreEqual("grass", EcosystemSystem.HabitatOf(Tile(GameEnums.BiomeType.TemperateGrassland)));
        }

        [Test]
        public void SpeciesPool_ChangesCarryingAndHuntFactor()
        {
            LoadRealContent();

            float forest = EcologySpeciesCatalog.CarryingFactor("forest", "herbivore");
            float arid = EcologySpeciesCatalog.CarryingFactor("arid", "herbivore");
            Assert.Greater(forest, 0f);
            Assert.AreNotEqual(forest, arid, "不同栖息地物种池应有不同承载");
            Assert.Greater(EcologySpeciesCatalog.HuntYieldFactor("grass"), EcologySpeciesCatalog.HuntYieldFactor("forest"),
                "草原有更大体型猎物，捕猎产出系数应更高");
        }

        [Test]
        public void Tick_WildlifeDiffersByHabitatSpeciesPool()
        {
            LoadRealContent();
            TileData forest = Tile(GameEnums.BiomeType.DeciduousForest);
            TileData desert = Tile(GameEnums.BiomeType.HotDesert);
            TileData[] tiles = { forest, desert };

            EcosystemSystem.Tick(tiles, 30f * 24f, 9);

            Assert.Greater(tiles[0].wildlife, tiles[1].wildlife, "森林物种池的承载应高于荒漠");
        }

        [Test]
        public void Mods_OverrideSpeciesPool()
        {
            string root = Path.Combine(Path.GetTempPath(), "myriad_eco_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "Base", "Ecology"));
            File.WriteAllText(Path.Combine(root, "Base", "Ecology", "Species.json"),
                "{\"species\":[{\"speciesId\":\"eco_big\",\"name\":\"巨兽\",\"role\":\"herbivore\",\"habitat\":\"forest\",\"bodySize\":3.0,\"fecundity\":1.0,\"dietHerbivore\":1.0}]}");
            string modDir = Path.Combine(root, "Mods", "mod_a", "Ecology");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "Species.json"),
                "{\"species\":[{\"speciesId\":\"eco_big\",\"name\":\"巨兽改\",\"role\":\"herbivore\",\"habitat\":\"forest\",\"bodySize\":1.0,\"fecundity\":1.0,\"dietHerbivore\":1.0}]}");
            try
            {
                var index = new ContentDefinitionIndex();
                index.Initialize(root, ContentDefinitionIndex.InnovationFallbackGroups.Default);
                EcologySpeciesCatalog.LoadFromContent(index.EcologySpecies.Values);

                Assert.AreEqual("巨兽改", index.EcologySpecies["eco_big"].name, "Mods 应按 id 覆盖");
                Assert.AreEqual(1.0f, EcologySpeciesCatalog.CarryingFactor("forest", "herbivore"), 0.001f);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
