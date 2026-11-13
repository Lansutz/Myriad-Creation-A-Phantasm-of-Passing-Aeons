using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Rendering;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI;
using MyriadCreation.UI.Panels;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;




namespace MyriadCreation.Tests
{
    /// <summary>
    /// 地图视觉链路验证（用户核心诉求：点开始游戏后地图必须真实加载并
    /// 可见）——生成后截图保存 + 中心像素采样（确认非纯背景色——
    /// 相机对准了地图）
    /// </summary>
    public class MapVisibilityPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            // 销毁本测试生成的大世界/场景对象，避免 524k TileData 泄漏导致后续测试 GC 全堆扫描（曾使后续测试膨胀到 ~100s）。
            foreach (var w in Object.FindObjectsByType<GameWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (w != null) Object.DestroyImmediate(w.gameObject);
            foreach (var r in Object.FindObjectsByType<MapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r != null) Object.DestroyImmediate(r.gameObject);
            foreach (var f in Object.FindObjectsByType<SceneFlowController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (f != null) Object.DestroyImmediate(f.gameObject);
            foreach (var m in Object.FindObjectsByType<MainMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (m != null) Object.DestroyImmediate(m.gameObject);
            System.GC.Collect();
        }

        [UnityTest]
        public IEnumerator Map_GeneratesAnd_VisibleInCamera()
        {
            LogAssert.ignoreFailingMessages = false;
            SceneManager.LoadScene("Main");
            yield return null;

            var world = Object.FindAnyObjectByType<GameWorld>();
            Assert.IsNotNull(world);
            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null && world.GetLandTileCount() <= 0)
                bootstrap.StartNewGame();

            float wait = 0f;
            while (world.GetLandTileCount() <= 0 && wait < 40f)
            {
                yield return null;
                wait += Time.deltaTime;
            }
            Assert.Greater(world.GetLandTileCount(), 0, "地图已生成");

            // 等地图纹理/相机就位（MapRenderer SetupMapDisplay 在同步后）
            yield return new WaitForSeconds(1f);

            var mapRenderer = Object.FindAnyObjectByType<MapRenderer>();
            Assert.IsNotNull(mapRenderer, "MapRenderer 存在");

            // 显式切到地形模式再截图：默认模式可能被 UI 切到宗教/教统等
            // 缺少数据时会是纯白，容易把"模式无数据"误判成"渲染管线坏了"。
            mapRenderer.SetDisplayMode(MapDisplayMode.Terrain);
            yield return null;
            yield return new WaitForSeconds(1.5f); // 等地图纹理/材质完成一帧
            UnityEngine.Debug.Log($"[MapVisible] 显示模式：{mapRenderer.DisplayMode}");

            // 视觉验证（batchmode 无帧尾——相机手动 Render 到 RenderTexture）
            var cam = Camera.main;
            if (cam == null)
            {
                var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cams != null && cams.Length > 0) cam = cams[0];
            }
            Assert.IsNotNull(cam, "相机存在");

            // 取景回归（曾经两个 bug：平面被 10× 缩放 + 相机放在地图中心导致看向地图外）：
            // 相机中心射线与 y=0 平面的交点，必须落在网格平面中心附近。
            var meshFilter = mapRenderer.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter, "MapPlane 有网格");
            Assert.IsNotNull(meshFilter.sharedMesh, "网格已生成");
            Vector3 planeCenter = mapRenderer.transform.TransformPoint(meshFilter.sharedMesh.bounds.center);
            Assert.IsTrue(Vector3.Distance(Vector3.one, mapRenderer.transform.localScale) < 0.01f,
                $"地图平面不应再被缩放（当前 {mapRenderer.transform.localScale}）");
            float zdist = cam.transform.forward.y;
            Assert.Less(zdist, -0.1f, "正交相机应向下俯视");
            float t = -cam.transform.position.y / zdist;
            Vector3 hit = cam.transform.position + cam.transform.forward * t;
            float tolerance = Mathf.Max(5f, meshFilter.sharedMesh.bounds.size.magnitude * 0.08f);
            Assert.Less(Vector3.Distance(new Vector3(hit.x, 0f, hit.z), new Vector3(planeCenter.x, 0f, planeCenter.z)),
                tolerance, $"相机中心射线应落在地图中心（hit={hit}, planeCenter={planeCenter}）");
            UnityEngine.Debug.Log($"[MapVisible] 相机: {(cam != null ? cam.name : "null")}——MapRenderer: {mapRenderer.name}");
            int w = 640, h = 360;
            var rt = new RenderTexture(w, h, 24);
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevTarget;

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            string path = "D:/map_verify.png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Debug.Log($"[MapVisible] 相机渲染图已存 {path}");

            // 全图采样（地图已渲染确认——非背景[相机 clear 色]比例——
            // 陆地/海/政权色都算地图内容——60° 俯视投影/海占比使中心区
            // 判定不稳——放宽全图）
            //
            // 注意：-nographics 批处理下相机 Render 得到的是单一底色（实测 230400 像素 unique=1），
            // 原来写死背景色 (0.1,0.15,0.25) 的"非背景比例"会在无图形设备时恒真（假绿）。
            // 因此分两种模式：有图形设备才做像素多样性断言；无图形设备只证明
            // "场景加载 + 对象存在 + 渲染链不抛异常"，并明确打日志（不是"地图可见"的证明）。
            bool headless = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (headless)
            {
                Object.Destroy(tex);
                rt.Release();
                UnityEngine.Debug.Log("[MapVisible] 无图形设备（-nographics）：跳过像素可见性断言，仅验证场景/对象/渲染链");
                Assert.IsNotNull(mapRenderer, "MapRenderer 存在");
                yield break;
            }

            var uniqueColors = new System.Collections.Generic.HashSet<int>();
            float minLum = 1f, maxLum = 0f;
            float satSum = 0f;
            int nonBg = 0, total = 0;
            for (int gy = 0; gy < 8; gy++)
            for (int gx = 0; gx < 8; gx++)
            {
                var c = tex.GetPixel(w * (gx * 2 + 1) / 16, h * (gy * 2 + 1) / 16);
                total++;
                uniqueColors.Add((Mathf.RoundToInt(c.r * 255f) << 16) | (Mathf.RoundToInt(c.g * 255f) << 8) | Mathf.RoundToInt(c.b * 255f));
                float lum = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                minLum = Mathf.Min(minLum, lum);
                maxLum = Mathf.Max(maxLum, lum);
                // 相机 clear 背景：直接取相机背景色，容差收紧到 0.03（避免深海色被误判为背景）
                var bgc = cam.backgroundColor;
                bool bg = Mathf.Abs(c.r - bgc.r) < 0.03f && Mathf.Abs(c.g - bgc.g) < 0.03f
                    && Mathf.Abs(c.b - bgc.b) < 0.03f;
                if (!bg)
                {
                    nonBg++;
                    Color.RGBToHSV(c, out _, out float s, out _);
                    satSum += s;
                }
            }
            Object.Destroy(tex);
            rt.Release();
            float avgSat = nonBg > 0 ? satSum / nonBg : 0f;
            UnityEngine.Debug.Log($"[MapVisible] 全图非背景 {nonBg}/{total}，unique 颜色 {uniqueColors.Count}，亮度范围 {minLum:0.000}~{maxLum:0.000}，平均饱和度 {avgSat:0.000}");
            Assert.Greater(uniqueColors.Count, 8, "地图应渲染出多种颜色（而非单一底色——图存 D:/map_verify.png）");
            Assert.Greater(maxLum - minLum, 0.02f, "画面应有亮度层次，不是纯色");
            Assert.Greater(nonBg, total * 0.15f, "地图应渲染（非纯背景——图存 D:/map_verify.png）");
            // 地图纹理必须 Unlit 直出：Standard 材质会被方向光二次照亮，把草绿冲成发白灰绿（饱和度塌到 ~0.05）
            Assert.Greater(avgSat, 0.10f, "地图颜色不应被光照洗白（饱和度不足）");
        }
    }
}
