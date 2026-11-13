using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.World.Editing;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>运行时地图编辑核心：选择、移动大陆、抬升、撤销/重做。</summary>
    public class MapEditCommandTests
    {
        private static TileData[] Map(int w, int h)
        {
            var tiles = new TileData[w * h];
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = new TileData { tileIndex = i, exists = true, isLand = true, elevation01 = 0.5f };
            return tiles;
        }

        [Test]
        public void Selection_SelectsRectangle()
        {
            var selection = new MapSelection(5, 5);
            selection.SelectRect(1, 1, 3, 2);
            Assert.AreEqual(6, selection.Count);
            Assert.IsTrue(selection[1 * 5 + 1]);
            Assert.IsFalse(selection[0]);
        }

        [Test]
        public void Move_Selection_ShiftsAndUndoRestores()
        {
            int w = 5, h = 5;
            var tiles = Map(w, h);
            tiles[1 * 5 + 1].elevation01 = 0.9f;
            tiles[1 * 5 + 2].elevation01 = 0.8f;
            var selection = new MapSelection(w, h);
            selection.SelectRect(1, 1, 2, 1);
            var history = new MapEditHistory();
            history.Execute(tiles, new MoveSelectionCommand(selection, 1, 0));

            Assert.AreEqual(0.9f, tiles[1 * 5 + 2].elevation01, "目标位置承接源地块");
            Assert.IsFalse(tiles[1 * 5 + 1].exists, "源位置清空");

            Assert.IsTrue(history.Undo(tiles));
            Assert.IsTrue(tiles[1 * 5 + 1].exists);
            Assert.AreEqual(0.9f, tiles[1 * 5 + 1].elevation01);
            Assert.IsTrue(history.Redo(tiles));
            Assert.IsFalse(tiles[1 * 5 + 1].exists);
        }

        [Test]
        public void RaiseSelection_AppliesDeltaAndUndo()
        {
            var tiles = Map(4, 4);
            var selection = new MapSelection(4, 4);
            selection.SelectRect(0, 0, 1, 1);
            var history = new MapEditHistory();
            history.Execute(tiles, new RaiseSelectionCommand(selection, 0.2f));
            Assert.AreEqual(0.7f, tiles[0].elevation01, 1e-5f);
            history.Undo(tiles);
            Assert.AreEqual(0.5f, tiles[0].elevation01, 1e-5f);
        }
    }
}
