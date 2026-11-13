using System;
using System.Collections;
using MyriadCreation.Simulation.Generation;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// B3-Step3c：世界生成管线薄服务 — 1:1 映射 GameWorld 的 internal 生成 hooks。
    /// 生成逻辑仍全部在 GameWorld；本服务只负责空世界守卫与调用转发，便于后续把实现搬出 GameWorld。
    /// 空 GameWorld（null 或尚未 InitializeWorld / tiles 未分配）时所有入口安全返回。
    /// </summary>
    public sealed class WorldGenerationService
    {
        private readonly GameWorld _world;

        public WorldGenerationService(GameWorld world)
        {
            _world = world;
        }

        public GameWorld World => _world;

        private bool CanGenerate => _world != null && _world.tiles != null;

        /// <summary>1:1 映射 GameWorld.GenerateTerrainForHost(seed)。</summary>
        public void GenerateTerrain(int seed = 42)
        {
            if (!CanGenerate) return;
            _world.GenerateTerrainForHost(seed);
        }

        /// <summary>1:1 映射 GameWorld.GenerateTerrainWithConfigForHost()。</summary>
        public void GenerateTerrainWithConfig()
        {
            if (!CanGenerate) return;
            _world.GenerateTerrainWithConfigForHost();
        }

        /// <summary>1:1 映射 GameWorld.GenerateAllForHost()。</summary>
        public void GenerateAll()
        {
            if (!CanGenerate) return;
            _world.GenerateAllForHost();
        }

        /// <summary>1:1 映射 GameWorld.GenerateTerrainAsyncForHost；空世界直接完成回调。</summary>
        public IEnumerator GenerateTerrainAsync(int seed, Action onComplete, Action<float, string> onProgress = null)
        {
            if (!CanGenerate)
            {
                onComplete?.Invoke();
                yield break;
            }
            yield return _world.GenerateTerrainAsyncForHost(seed, onComplete, onProgress);
        }

        /// <summary>1:1 映射 GameWorld.RegenerateFromPipelineForHost(fromStage)。</summary>
        public void RegenerateFromPipeline(GenerationStage fromStage)
        {
            if (!CanGenerate) return;
            _world.RegenerateFromPipelineForHost(fromStage);
        }

        /// <summary>1:1 映射 GameWorld.InitializeDefaultRealmsForHost()。</summary>
        public void InitializeDefaultRealms()
        {
            if (!CanGenerate) return;
            _world.InitializeDefaultRealmsForHost();
        }

        /// <summary>1:1 映射 GameWorld.GenerateProvincesForHost()。</summary>
        public void GenerateProvinces()
        {
            if (!CanGenerate) return;
            _world.GenerateProvincesForHost();
        }

        /// <summary>1:1 映射 GameWorld.GenerateSettlementsForHost()。</summary>
        public void GenerateSettlements()
        {
            if (!CanGenerate) return;
            _world.GenerateSettlementsForHost();
        }

        /// <summary>1:1 映射 GameWorld.GenerateInitialUnitsForHost(seed)。</summary>
        public void GenerateInitialUnits(int seed)
        {
            if (!CanGenerate) return;
            _world.GenerateInitialUnitsForHost(seed);
        }
    }
}
