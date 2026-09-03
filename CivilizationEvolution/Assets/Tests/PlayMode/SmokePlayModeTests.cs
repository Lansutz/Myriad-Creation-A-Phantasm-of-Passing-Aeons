using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// Play 模式冒烟（第二关：运行时无异常——真实生命周期/主循环）
    /// 加载 Main.unity → Bootstrap.Awake/Start 真跑 → 主循环推进 → 断言世界状态
    /// </summary>
    public class SmokePlayModeTests
    {
        [UnityTest]
        public IEnumerator GameBoots_WorldGenerates_NoRuntimeErrors()
        {
            LogAssert.ignoreFailingMessages = false;

            // 加载主场景（Bootstrap.Awake → 注册表/子系统初始化）
            SceneManager.LoadScene("Main");
            yield return null; // 场景激活

            var world = Object.FindAnyObjectByType<GameWorld>();
            Assert.IsNotNull(world, "GameWorld 应存在（场景 Bootstrap 创建）");

            // 主菜单模式适配：显式开始游戏（原依赖 Bootstrap 自动 StartNewGame——
            // startViaMenu=true 后需手动触发——模拟玩家点"开始游戏"）
            var bootstrap = Object.FindAnyObjectByType<Bootstrap>();
            if (bootstrap != null && world.GetLandTileCount() <= 0)
                bootstrap.StartNewGame();

            // 等待开局（地形生成+初始政权）
            float wait = 0f;
            while (world.GetLandTileCount() <= 0 && wait < 30f)
            {
                yield return null;
                wait += Time.deltaTime;
            }

            Assert.Greater(world.GetLandTileCount(), 0, "陆地已生成");
            Assert.Greater(world.GetSeaTileCount(), 0, "海洋已生成");
            Assert.Greater(world.realms.Count, 0, "初始政权已创建");

            // 主循环推进 20 tick（1秒=1天——验证每日系统无异常）
            for (int i = 0; i < 20; i++)
                yield return null;

            Assert.Greater(world.currentDay + world.currentYear * 365, 1, "时间推进正常");
        }
    }
}
