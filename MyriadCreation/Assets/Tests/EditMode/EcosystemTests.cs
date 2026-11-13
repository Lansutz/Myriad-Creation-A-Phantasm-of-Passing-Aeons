using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P3.2 生态演替：植被向 NPP 潜力恢复、被人群开垦压低；野生群落跟随植被与压力。
    /// 只断言方向与守恒，不锁定会被平衡调整的速率。
    /// </summary>
    public class EcosystemTests
    {
        private static TileData Land(float npp, float vegetation = 0f, float wildlife = 0f, int people = 0)
        {
            var blocks = new List<PopulationBlock>();
            if (people > 0)
            {
                blocks.Add(new PopulationBlock { count = people, socialClass = GameEnums.SocialClass.Peasant });
            }
            return new TileData
            {
                exists = true,
                isLand = true,
                npp = npp,
                vegetation = vegetation,
                wildlife = wildlife,
                populationBlocks = blocks
            };
        }

        [Test]
        public void EnsureInitialized_BackfillsOldSaveFromNpp()
        {
            TileData tile = Land(0.6f);

            EcosystemSystem.EnsureInitialized(ref tile);

            Assert.AreEqual(0.6f, tile.vegetation, 0.001f, "旧档缺植被时按 NPP 补齐");
            Assert.AreEqual(0.24f, tile.wildlife, 0.001f, "旧档缺野生群落时按植被比例补齐");
        }

        [Test]
        public void Tick_MovesVegetationTowardNppTarget()
        {
            TileData[] tiles = { Land(0.8f, vegetation: 0.1f, wildlife: 0.04f) };

            EcosystemSystem.Tick(tiles, 30f * 12f);
            TileData tile = tiles[0];

            Assert.Greater(tile.vegetation, 0.1f, "无人的地块植被应向 NPP 潜力恢复");
            Assert.LessOrEqual(tile.vegetation, 0.8f, "不得越过 NPP 上限");
            Assert.Greater(tile.wildlife, 0.04f, "植被恢复后野生群落应随之增长");
        }

        [Test]
        public void Tick_HumanPressureLowersVegetationAndWildlife()
        {
            TileData[] tiles = { Land(0.8f, vegetation: 0.8f), Land(0.8f, vegetation: 0.8f, people: 1200) };

            EcosystemSystem.Tick(tiles, 30f * 24f);
            TileData wild = tiles[0];
            TileData settled = tiles[1];

            Assert.Less(settled.vegetation, wild.vegetation, "人口压力应压低植被覆盖（开垦/采伐）");
            Assert.Less(settled.wildlife, wild.wildlife, "人口压力应压低野生群落（狩猎/失去栖息地）");
        }

        [Test]
        public void Tick_LeavesSeaAndVoidUntouched()
        {
            TileData sea = Land(0.8f, vegetation: 0.3f, wildlife: 0.2f);
            sea.isLand = false;
            TileData voidTile = Land(0.8f, vegetation: 0.3f, wildlife: 0.2f);
            voidTile.exists = false;
            TileData[] tiles = { sea, voidTile };

            EcosystemSystem.Tick(tiles, 30f * 12f);
            sea = tiles[0];
            voidTile = tiles[1];

            Assert.AreEqual(0.3f, sea.vegetation, 0.001f, "海洋不参与陆地演替");
            Assert.AreEqual(0.2f, sea.wildlife, 0.001f);
            Assert.AreEqual(0.3f, voidTile.vegetation, 0.001f, "未存在地块不参与演替");
        }

        [Test]
        public void EcosystemFields_SurviveJsonRoundTrip()
        {
            TileData tile = Land(0.6f, vegetation: 0.45f, wildlife: 0.2f);

            string json = UnityEngine.JsonUtility.ToJson(tile);
            TileData restored = UnityEngine.JsonUtility.FromJson<TileData>(json);

            Assert.AreEqual(0.45f, restored.vegetation, 0.001f, "植被必须随地块存档往返");
            Assert.AreEqual(0.2f, restored.wildlife, 0.001f, "野生群落必须随地块存档往返");
            Assert.AreEqual(0.6f, restored.npp, 0.001f, "NPP 必须随地块存档往返");
        }

        [Test]
        public void Tick_IsDeterministic()
        {
            TileData[] a = { Land(0.7f, vegetation: 0.2f, wildlife: 0.1f, people: 300) };
            TileData[] b = { Land(0.7f, vegetation: 0.2f, wildlife: 0.1f, people: 300) };

            EcosystemSystem.Tick(a, 30f * 6f);
            EcosystemSystem.Tick(b, 30f * 6f);

            Assert.AreEqual(a[0].vegetation, b[0].vegetation, 0.0001f, "同输入必须同输出（无随机）");
            Assert.AreEqual(a[0].wildlife, b[0].wildlife, 0.0001f);
        }
    }
}
