using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Rendering;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P11.4 截图工件：把真实地形纹理导出为 PNG（山影+等高线可见），供 docs/art 人工核对。
    /// Explicit：只在需要更新截图时运行。
    /// </summary>
    public class TerrainPreviewCaptureTests
    {
        [Test]
        [Explicit("导出地形材质截图工件")]
        public void Capture_TerrainPreviewPng()
        {
            var worldGo = new GameObject("PreviewWorld");
            var world = worldGo.AddComponent<GameWorld>();
            world.mapWidth = 512;
            world.mapHeight = 256;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(4242);

            var mapGo = new GameObject("PreviewMap");
            var renderer = mapGo.AddComponent<MapRenderer>();
            renderer.SetWorldView(new MapWorldViewAdapter(world));
            renderer.SetDisplayMode(MapDisplayMode.Terrain);

            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(MapRenderer).GetMethod("TrySyncMapSizeFromView", flags)?.Invoke(renderer, null);
            typeof(MapRenderer).GetMethod("InitializeRenderer", flags)?.Invoke(renderer, null);
            typeof(MapRenderer).GetMethod("InitializeColors", flags)?.Invoke(renderer, null);
            typeof(MapRenderer).GetMethod("UpdateMapTexture", flags)?.Invoke(renderer, null);

            Texture2D texture = renderer.GetMapTexture();
            Assert.IsNotNull(texture, "地形纹理应已生成");

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../docs/art"));
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "terrain-preview.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            TestContext.WriteLine("PREVIEW " + path);

            Object.DestroyImmediate(mapGo);
            Object.DestroyImmediate(worldGo);
        }
    }
}
