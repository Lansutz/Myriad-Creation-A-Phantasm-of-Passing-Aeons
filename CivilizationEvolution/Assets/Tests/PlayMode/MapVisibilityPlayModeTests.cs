using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CivilizationEvolution.Core;
using CivilizationEvolution.Render;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 地图视觉链路验证（用户核心诉求：点开始游戏后地图必须真实加载并
    /// 可见）——生成后截图保存 + 中心像素采样（确认非纯背景色——
    /// 相机对准了地图）
    /// </summary>
    public class MapVisibilityPlayModeTests
    {
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

            // 视觉验证（batchmode 无帧尾——相机手动 Render 到 RenderTexture）
            var cam = Camera.main;
            if (cam == null)
            {
                var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cams != null && cams.Length > 0) cam = cams[0];
            }
            UnityEngine.Debug.Log($"[MapVisible] 相机: {(cam != null ? cam.name : "null")}——MapRenderer: {mapRenderer.name}");
            Assert.IsNotNull(cam, "相机存在");
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

            // 中心像素采样（16 宫格——地图占屏中心——非背景色[0.1,0.15,0.25]）
            int nonBg = 0, total = 0;
            for (int gx = 2; gx <= 5; gx++)
            for (int gy = 2; gy <= 5; gy++)
            {
                var c = tex.GetPixel(w * gx / 8, h * gy / 8);
                total++;
                bool bg = Mathf.Abs(c.r - 0.1f) < 0.06f && Mathf.Abs(c.g - 0.15f) < 0.06f
                    && Mathf.Abs(c.b - 0.25f) < 0.06f;
                if (!bg) nonBg++;
            }
            Object.Destroy(tex);
            rt.Release();
            UnityEngine.Debug.Log($"[MapVisible] 中心非背景 {nonBg}/{total}");
            Assert.Greater(nonBg, total * 0.3f, "相机视野应见地图（非纯背景——图存 D:/map_verify.png）");
        }
    }
}
