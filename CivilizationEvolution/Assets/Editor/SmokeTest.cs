#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.EditorTools
{
    /// <summary>
    /// 运行冒烟测试（batchmode）。结果统一写绝对路径 Temp/smoke_result.txt。
    /// RunTerrain：纯 Edit 模式驱动世界生成，验证海陆/群系。
    /// RunPlay  ：真实进入 Play 模式，延迟若干帧后读取运行时世界并捕获异常。
    /// </summary>
    public static class SmokeTest
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private static string ResultFile => Path.Combine(Directory.GetCurrentDirectory(), "Temp", "smoke_result.txt");
        private static string FlagFile => Path.Combine(Directory.GetCurrentDirectory(), "Temp", "smoke_play.flag");

        private static readonly List<string> Errors = new List<string>();
        private static int _delayFrames;

        static void WriteResult(string text)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ResultFile));
                File.WriteAllText(ResultFile, text, Encoding.UTF8);
                Debug.Log($"[SmokeTest] 结果已写入 {ResultFile}");
            }
            catch (Exception ex) { Debug.LogError($"[SmokeTest] 写结果失败: {ex}"); }
        }

        // ---------- 纯逻辑 ----------
        public static void RunTerrain()
        {
            var sb = new StringBuilder();
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var gw = UnityEngine.Object.FindAnyObjectByType<GameWorld>();
                if (gw == null) gw = new GameObject("GameWorld").AddComponent<GameWorld>();
                gw.mapWidth = 128; gw.mapHeight = 64;
                gw.InitializeWorld();
                gw.GenerateTerrain(42);

                int total = gw.mapWidth * gw.mapHeight;
                int land = gw.GetLandTileCount(), sea = gw.GetSeaTileCount();
                sb.AppendLine("OK");
                sb.AppendLine($"total={total}");
                sb.AppendLine($"land={land} ({land * 100.0 / total:F1}%)");
                sb.AppendLine($"sea={sea} ({sea * 100.0 / total:F1}%)");
                sb.AppendLine($"connectedSeaGroups={gw.GetConnectedSeaCount()}");
            }
            catch (Exception ex) { sb.AppendLine("FAIL"); sb.AppendLine(ex.ToString()); }
            WriteResult(sb.ToString());
            EditorApplication.Exit(0);
        }

        // ---------- 真实 Play 模式 ----------
        public static void RunPlay()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FlagFile));
            File.WriteAllText(FlagFile, "play");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Application.logMessageReceivedThreaded += OnLog;
            EditorApplication.playModeStateChanged += OnPlayChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void OnLog(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                if (condition.Contains("Input Button")) return;
                Errors.Add($"[{type}] {condition}");
            }
        }

        private static void OnPlayChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;

            // batchmode 进入 Play 后编辑器 update/delayCall 会被 player loop 占住而停摆，
            // 因此在 EnteredPlayMode 事件里同步完成：主动确保开局（幂等）→ 立即采样 → 退出。
            try
            {
                var gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                if (gm != null) gm.StartNewGame(128, 64, 42);

                var gw = UnityEngine.Object.FindAnyObjectByType<GameWorld>();
                int land = gw != null ? gw.GetLandTileCount() : -1;
                int sea = gw != null ? gw.GetSeaTileCount() : -1;
                var mr = UnityEngine.Object.FindAnyObjectByType<CivilizationEvolution.Render.MapRenderer>();

                var sb = new StringBuilder();
                sb.AppendLine("PLAY-OK");
                sb.AppendLine($"gameManagerFound={gm != null}");
                sb.AppendLine($"gameWorldFound={gw != null}");
                sb.AppendLine($"mapRendererFound={mr != null}");
                sb.AppendLine($"land={land}");
                sb.AppendLine($"sea={sea}");
                sb.AppendLine($"runtimeErrorCount={Errors.Count}");
                foreach (var e in Errors) sb.AppendLine(e);
                WriteResult(sb.ToString());
            }
            catch (Exception ex)
            {
                WriteResult("PLAY-FAIL\n" + ex);
            }

            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () =>
            {
                try { if (File.Exists(FlagFile)) File.Delete(FlagFile); } catch { }
                EditorApplication.Exit(0);
            };
        }
    }
}
#endif
