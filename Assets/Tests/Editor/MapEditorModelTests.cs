using NUnit.Framework;
using Myriad.MapEditor;
using UnityEngine;

namespace Myriad.Tests.Editor
{
    public sealed class MapEditorModelTests
    {
        [Test]
        public void LandAndCoastAreDerivedFromElevationAndEdgeNeighbours()
        {
            var map = new MapEditorModel(3, 3);

            map.SetElevationAt(1, 1, 1f);

            Assert.That(map.GetCell(1, 1).IsLand, Is.True);
            Assert.That(map.GetCell(1, 1).IsCoast, Is.True);
            Assert.That(map.GetCell(1, 1).OceanTier, Is.EqualTo(OceanTier.None));
            Assert.That(map.GetCell(1, 0).OceanTier, Is.EqualTo(OceanTier.Coast));
        }

        [Test]
        public void DiagonalSeaCellsDoNotConnectThroughLand()
        {
            var map = new MapEditorModel(2, 2);
            map.SetElevationAt(1, 0, 1f);
            map.SetElevationAt(0, 1, 1f);

            Assert.That(map.GetCell(0, 0).SeaConnectId, Is.Not.EqualTo(map.GetCell(1, 1).SeaConnectId));
        }

        [Test]
        public void UndoAndRedoRestoreSourceHeightAndDerivedOceanData()
        {
            var map = new MapEditorModel(3, 3);
            map.SetElevationAt(1, 1, 1f);

            map.Undo();
            Assert.That(map.GetCell(1, 1).IsLand, Is.False);
            Assert.That(map.GetCell(1, 1).SeaConnectId, Is.Not.Null);

            map.Redo();
            Assert.That(map.GetCell(1, 1).IsLand, Is.True);
            Assert.That(map.GetCell(1, 1).SeaConnectId, Is.Null);
        }

        [Test]
        public void SameGeneratorInputsProduceSameHeights()
        {
            var first = new MapEditorModel(8, 8);
            var second = new MapEditorModel(8, 8);

            first.GenerateCraterIslands(new Vector2(4f, 4f), 4f, 2, 77, 2f, 1f, 0.1f);
            second.GenerateCraterIslands(new Vector2(4f, 4f), 4f, 2, 77, 2f, 1f, 0.1f);

            for (var y = 0; y < 8; y++)
            for (var x = 0; x < 8; x++)
                Assert.That(first.GetCell(x, y).Elevation, Is.EqualTo(second.GetCell(x, y).Elevation));
        }
    }
}
