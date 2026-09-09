using System;
using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Core
{
 /// 世界生成阶段（有序依赖链，参考 Azgaar FMG 的 19 阶段管线）。 /// 每阶段只读前序输出，修改某阶段后只需重算下游。    public enum GenerationStage
    {
        None = 0,
 /// <summary>种子初始化：确定 WorldSeed，初始化随机数生成器</summary>        Seed = 1,
 /// <summary>地形生成：高度图、海陆划分、海拔/坡度</summary>        Terrain = 2,
 /// <summary>地理特征：海洋分层、湖泊、海岸、岛屿、不可通行区</summary>        Features = 3,
 /// <summary>气候：温度、降水、大气环流、洋流</summary>        Climate = 4,
 /// <summary>水文：河网追踪、水力侵蚀、流域</summary>        Hydrology = 5,
 /// <summary>群系：根据温度/降水/海拔/土壤划分生物群系</summary>        Biomes = 6,
 /// <summary>省份划分：基于地块的政治边界生成</summary>        Provinces = 7,
 /// <summary>聚落生成：城市/港口/要塞/村庄（Burg）</summary>        Burgs = 8,
 /// <summary>社会生成：文化/宗教/政权/阶层/人口</summary>        Society = 9,
 /// <summary>全部完成</summary>        Complete = 100
    }

 /// 生成管线管理器：统一管理生成阶段顺序、依赖关系、进度回调、下游重算。 /// 参考 Azgaar FMG 的有序生成管线，每阶段有明确的输入/输出，支持增量重算。    public class GenerationPipeline
    {
 /// <summary>阶段执行顺序（索引=执行顺序）</summary>        private static readonly GenerationStage[] StageOrder =
        {
            GenerationStage.Seed,
            GenerationStage.Terrain,
            GenerationStage.Features,
            GenerationStage.Climate,
            GenerationStage.Hydrology,
            GenerationStage.Biomes,
            GenerationStage.Provinces,
            GenerationStage.Burgs,
            GenerationStage.Society,
        };

 /// <summary>阶段中文名（用于进度显示）</summary>        private static readonly Dictionary<GenerationStage, string> StageNames = new()
        {
            { GenerationStage.Seed, "初始化种子" },
            { GenerationStage.Terrain, "生成地形" },
            { GenerationStage.Features, "标记地理特征" },
            { GenerationStage.Climate, "计算气候" },
            { GenerationStage.Hydrology, "重算水文" },
            { GenerationStage.Biomes, "划分群系" },
            { GenerationStage.Provinces, "划分省份" },
            { GenerationStage.Burgs, "生成聚落" },
            { GenerationStage.Society, "生成社会" },
        };

        private readonly GameWorld _world;
        private GenerationStage _currentStage = GenerationStage.None;
        private readonly HashSet<GenerationStage> _completedStages = new();
        private bool _isGenerating;

 /// <summary>进度回调：(当前阶段, 0~1 总进度)</summary>        public event Action<GenerationStage, float> OnProgress;

 /// <summary>单阶段完成回调</summary>        public event Action<GenerationStage> OnStageComplete;

 /// <summary>全部生成完成回调</summary>        public event Action OnComplete;

 /// <summary>当前执行到的阶段</summary>        public GenerationStage CurrentStage => _currentStage;

 /// <summary>是否正在生成</summary>        public bool IsGenerating => _isGenerating;

 /// <summary>已完成的阶段集合</summary>        public IReadOnlyCollection<GenerationStage> CompletedStages => _completedStages;

        public GenerationPipeline(GameWorld world)
        {
            _world = world;
        }

 /// <summary>获取阶段中文名</summary>        public static string GetStageName(GenerationStage stage) =>
            StageNames.TryGetValue(stage, out var name) ? name : stage.ToString();

 /// <summary>获取阶段在执行顺序中的索引（0-based）</summary>        public static int GetStageIndex(GenerationStage stage) =>
            Array.IndexOf(StageOrder, stage);

 /// <summary>判断 stage 是否在 afterStage 之后（下游）</summary>        public static bool IsDownstream(GenerationStage stage, GenerationStage afterStage) =>
            GetStageIndex(stage) > GetStageIndex(afterStage);

 /// <summary>某阶段是否已完成</summary>        public bool IsStageComplete(GenerationStage stage) => _completedStages.Contains(stage);

 /// 全量生成：从 Seed 到 Society 按顺序执行所有阶段。 /// 同步执行，每阶段触发进度回调。        public void GenerateAll()
        {
            if (_isGenerating)
            {
                Debug.LogWarning("[GenerationPipeline] 正在生成中，忽略重复调用");
                return;
            }

            _isGenerating = true;
            _completedStages.Clear();
            int total = StageOrder.Length;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    var stage = StageOrder[i];
                    _currentStage = stage;
                    float progress = (float)i / total;
                    OnProgress?.Invoke(stage, progress);

                    ExecuteStage(stage);
                    _completedStages.Add(stage);
                    OnStageComplete?.Invoke(stage);
                }

                _currentStage = GenerationStage.Complete;
                _completedStages.Add(GenerationStage.Complete);
                OnProgress?.Invoke(GenerationStage.Complete, 1f);
                OnComplete?.Invoke();
                Debug.Log("[GenerationPipeline] 全部生成完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GenerationPipeline] 生成在 {_currentStage} 阶段失败: {e.Message}");
                throw;
            }
            finally
            {
                _isGenerating = false;
            }
        }

 /// 从指定阶段开始重算下游（包含该阶段本身）。 /// 例如修改地形后调用 RegenerateFrom(Terrain)，会重算 Terrain→Features→Climate→...→Society。 /// 上游已完成的阶段保留不变。        public void RegenerateFrom(GenerationStage fromStage)
        {
            if (_isGenerating)
            {
                Debug.LogWarning("[GenerationPipeline] 正在生成中，忽略重复调用");
                return;
            }

            int startIdx = GetStageIndex(fromStage);
            if (startIdx < 0)
            {
                Debug.LogError($"[GenerationPipeline] 未知阶段: {fromStage}");
                return;
            }

 // 清除从 fromStage 开始的所有下游阶段的完成标记            for (int i = startIdx; i < StageOrder.Length; i++)
                _completedStages.Remove(StageOrder[i]);
            _completedStages.Remove(GenerationStage.Complete);

            _isGenerating = true;
            int total = StageOrder.Length;

            try
            {
                for (int i = startIdx; i < total; i++)
                {
                    var stage = StageOrder[i];
                    _currentStage = stage;
                    float progress = (float)i / total;
                    OnProgress?.Invoke(stage, progress);

                    ExecuteStage(stage);
                    _completedStages.Add(stage);
                    OnStageComplete?.Invoke(stage);
                }

                _currentStage = GenerationStage.Complete;
                _completedStages.Add(GenerationStage.Complete);
                OnProgress?.Invoke(GenerationStage.Complete, 1f);
                OnComplete?.Invoke();
                Debug.Log($"[GenerationPipeline] 从 {GetStageName(fromStage)} 开始重算完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GenerationPipeline] 重算在 {_currentStage} 阶段失败: {e.Message}");
                throw;
            }
            finally
            {
                _isGenerating = false;
            }
        }

 /// 只重算单个阶段（不重算下游）。 /// 用于独立参数调整，如只改气候参数时只重算 Climate。 /// 注意：调用方需确保下游数据仍有效，否则应使用 RegenerateFrom。        public void RegenerateSingle(GenerationStage stage)
        {
            if (_isGenerating) return;

            _isGenerating = true;
            try
            {
                _currentStage = stage;
                ExecuteStage(stage);
                _completedStages.Add(stage);
                OnStageComplete?.Invoke(stage);
                Debug.Log($"[GenerationPipeline] 单阶段重算: {GetStageName(stage)}");
            }
            finally
            {
                _isGenerating = false;
            }
        }

 /// <summary>重置管线（清除所有完成标记）</summary>        public void Reset()
        {
            _completedStages.Clear();
            _currentStage = GenerationStage.None;
            _isGenerating = false;
        }

 /// 执行单个阶段的实际逻辑。 /// 对接 GameWorld 的现有生成方法。        private void ExecuteStage(GenerationStage stage)
        {
            switch (stage)
            {
                case GenerationStage.Seed:
 // 种子在 GameWorld 初始化时已处理，这里确认种子已就绪                    int seed = _world.GenConfig.GetActualSeed();
                    Debug.Log($"[GenerationPipeline] 种子: {seed}");
                    break;

                case GenerationStage.Terrain:
                    _world.GenerateTerrainWithConfig();
                    break;

                case GenerationStage.Features:
 // 地理特征在地形生成时已标记（海洋/海岸/湖泊），这里确认                    Debug.Log("[GenerationPipeline] 地理特征已在地形阶段标记");
                    break;

                case GenerationStage.Climate:
                    _world.CalculateClimate();
                    break;

                case GenerationStage.Hydrology:
                    _world.RecalculateHydrology();
                    break;

                case GenerationStage.Biomes:
 // 群系在气候计算时已划分，这里确认                    Debug.Log("[GenerationPipeline] 群系已在气候阶段划分");
                    break;

                case GenerationStage.Provinces:
                    _world.GenerateProvinces();
                    break;

                case GenerationStage.Burgs:
                    _world.GenerateBurgs();
                    break;

                case GenerationStage.Society:
                    _world.InitializeDefaultRealms();
                    break;
            }
        }
    }
}
