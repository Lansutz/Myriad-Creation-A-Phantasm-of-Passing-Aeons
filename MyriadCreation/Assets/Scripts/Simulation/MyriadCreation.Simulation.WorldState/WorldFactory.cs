using UnityEngine;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// B3-Step2：世界创建/销毁薄工厂（委托现有 GameWorld，供 App/Save 迁移）。
    ///   Create(host, options[, mapWidth, mapHeight]) → host 上 AddComponent&lt;GameWorld&gt; + 设置尺寸/SetupOptions + InitializeWorld；
    ///   CreateEmpty(host)                            → 仅 AddComponent/复用 + 基础字段，不 InitializeWorld（读档填充用）；
    ///   Destroy(world)                               → Play 模式 Destroy，Edit 模式 DestroyImmediate（测试友好）。
    /// </summary>
    public static class WorldFactory
    {
        /// <summary>在 host 上创建并初始化 GameWorld；host 为 null 时新建 "GameWorld" 对象。mapWidth/mapHeight&lt;=0 表示不覆盖。</summary>
        public static GameWorld Create(GameObject host, WorldSetupOptions options,
            int mapWidth = -1, int mapHeight = -1)
        {
            if (host == null)
            {
                host = new GameObject("GameWorld");
            }

            GameWorld world = host.GetComponent<GameWorld>();
            if (world == null)
            {
                world = host.AddComponent<GameWorld>();
            }

            if (mapWidth > 0)
            {
                world.mapWidth = mapWidth;
            }
            if (mapHeight > 0)
            {
                world.mapHeight = mapHeight;
            }
            if (options != null)
            {
                world.SetupOptions = options;
            }
            world.InitializeWorld();
            return world;
        }

        /// <summary>便捷重载：新建 "GameWorld" 对象并初始化。</summary>
        public static GameWorld Create(WorldSetupOptions options = null)
        {
            return Create(null, options);
        }

        /// <summary>
        /// 创建空世界（读档用）：host 上 AddComponent/复用 GameWorld，仅保基础字段，不 InitializeWorld。
        /// 读档流程随后由 SaveSystem 填充数据并走 WorldCompositionRoot 重建子系统。
        /// </summary>
        public static GameWorld CreateEmpty(GameObject host)
        {
            if (host == null)
            {
                host = new GameObject("GameWorld");
            }

            GameWorld world = host.GetComponent<GameWorld>();
            if (world == null)
            {
                world = host.AddComponent<GameWorld>();
            }
            return world;
        }

        /// <summary>便捷重载：新建 "GameWorld" 对象（不初始化）。</summary>
        public static GameWorld CreateEmpty()
        {
            return CreateEmpty(null);
        }

        /// <summary>销毁世界对象；Play 模式延迟 Destroy，Edit 模式 DestroyImmediate。</summary>
        public static void Destroy(GameWorld world)
        {
            if (world == null)
            {
                return;
            }

            GameObject go = world.gameObject;
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
