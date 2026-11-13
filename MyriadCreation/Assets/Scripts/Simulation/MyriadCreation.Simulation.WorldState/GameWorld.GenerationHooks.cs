using System;
using System.Collections;
using MyriadCreation.Simulation.Generation;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// B3-Step3c：生成管线 1:1 host hooks。
    /// 方法体直接调用 GameWorld 现有生成方法，不复制、不改变生成逻辑；
    /// 供同程序集内的 WorldGenerationService 精确映射。
    /// 不新增序列化字段，不改变场景/生成行为。
    /// </summary>
    public partial class GameWorld
    {
        /// <summary>直调现有 GenerateTerrain(seed)。</summary>
        internal void GenerateTerrainForHost(int seed)
        {
            GenerateTerrain(seed);
        }

        /// <summary>直调现有 GenerateTerrainWithConfig()。</summary>
        internal void GenerateTerrainWithConfigForHost()
        {
            GenerateTerrainWithConfig();
        }

        /// <summary>直调现有 GenerateAll()。</summary>
        internal void GenerateAllForHost()
        {
            GenerateAll();
        }

        /// <summary>直调现有 GenerateTerrainAsync(seed, onComplete, onProgress)。</summary>
        internal IEnumerator GenerateTerrainAsyncForHost(int seed, Action onComplete, Action<float, string> onProgress)
        {
            return GenerateTerrainAsync(seed, onComplete, onProgress);
        }

        /// <summary>直调现有 RegenerateFromPipeline(fromStage)。</summary>
        internal void RegenerateFromPipelineForHost(GenerationStage fromStage)
        {
            RegenerateFromPipeline(fromStage);
        }

        /// <summary>直调现有 InitializeDefaultRealms()（internal 生成方法）。</summary>
        internal void InitializeDefaultRealmsForHost()
        {
            InitializeDefaultRealms();
        }

        /// <summary>直调现有 GenerateProvinces()。</summary>
        internal void GenerateProvincesForHost()
        {
            GenerateProvinces();
        }

        /// <summary>直调现有 GenerateSettlements()。</summary>
        internal void GenerateSettlementsForHost()
        {
            GenerateSettlements();
        }

        /// <summary>直调现有 GenerateInitialUnits(seed)。</summary>
        internal void GenerateInitialUnitsForHost(int seed)
        {
            GenerateInitialUnits(seed);
        }
    }
}
