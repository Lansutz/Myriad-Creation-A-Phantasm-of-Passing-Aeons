using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// CultureStage 阈值 JSON 化（内容管线试点）测试：
    /// JSON 存在/解析、阈值与旧硬编码常量等值、给定人口档位→阶段映射回归、
    /// EvaluateEvolution 行为与旧逻辑一致（JSON 加载 vs 回退默认）、缺失/损坏回退不抛、重复加载幂等。
    /// </summary>
    public class CultureStageEvolutionDataTests
    {
        [SetUp]
        public void SetUp()
        {
            CultureStageCatalog.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            CultureStageCatalog.Reset();
        }

        private static HashSet<int> Innovations(params int[] ids)
        {
            return new HashSet<int>(ids);
        }

        private static void AssertSameEvolution(StageEvolutionResult expected, StageEvolutionResult actual, string context)
        {
            Assert.AreEqual(expected.canEvolve, actual.canEvolve, context + " canEvolve");
            Assert.AreEqual(expected.targetStage, actual.targetStage, context + " targetStage");
            Assert.AreEqual(expected.progress, actual.progress, 0.0001f, context + " progress");
            CollectionAssert.AreEqual(expected.unmetConditions, actual.unmetConditions, context + " unmetConditions");
        }

        // ===================== 1. JSON 存在且解析、字段正确 =====================

        [Test]
        public void CultureStagesJson_ExistsAndParses()
        {
            string path = CultureStageCatalog.ResolveDefaultPath();
            Assert.IsFalse(string.IsNullOrEmpty(path), "Application.streamingAssetsPath 应可用");
            Assert.IsTrue(File.Exists(path), "文化阶段定义 JSON 应存在：" + path);

            Assert.IsTrue(CultureStageCatalog.LoadFromFile(path), CultureStageCatalog.LastError);
            Assert.IsTrue(CultureStageCatalog.IsLoadedFromJson);
            Assert.AreEqual(5, CultureStageCatalog.Stages.Count, "阶段应为 0..4 共 5 个");

            Assert.IsTrue(CultureStageCatalog.TryGet(0, out CultureStageDefinition band));
            Assert.AreEqual("Band", band.key);
            Assert.AreEqual("游群", band.name);
            Assert.AreEqual(0, band.minPopulation);
            Assert.AreEqual(100, band.nextPopulation);
            Assert.AreEqual(0, band.order);
            Assert.AreEqual(0f, band.requiredSocialStratification, 0.0001f);
            Assert.IsFalse(band.requiresUrbanCenter);

            Assert.IsTrue(CultureStageCatalog.TryGet(GameEnums.CultureStage.HighCivilization, out CultureStageDefinition high));
            Assert.AreEqual(-1, high.nextPopulation, "终态 nextPopulation 应为 -1");
            Assert.AreEqual(10000, high.minPopulation);
        }

        // ===================== 2. 阈值与旧硬编码常量等值 =====================

        [Test]
        public void CultureStagesJson_ThresholdsMatchLegacyConstants()
        {
            Assert.IsTrue(CultureStageCatalog.LoadFromFile(CultureStageCatalog.ResolveDefaultPath()), CultureStageCatalog.LastError);

            Assert.AreEqual(CultureStageEvolutionSystem.POP_BAND_TO_TRIBE,
                CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Band));
            Assert.AreEqual(CultureStageEvolutionSystem.POP_TRIBE_TO_CHIEFDOM,
                CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Tribe));
            Assert.AreEqual(CultureStageEvolutionSystem.POP_CHIEFDOM_TO_ETHNIC,
                CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Chiefdom));
            Assert.AreEqual(CultureStageEvolutionSystem.POP_ETHNIC_TO_CIVILIZATION,
                CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.EthnicGroup));
            Assert.AreEqual(-1, CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.HighCivilization));

            // 代码默认（回退基准）也与旧常量一致
            List<CultureStageDefinition> defaults = CultureStageCatalog.CreateDefaultStages();
            Assert.AreEqual(5, defaults.Count);
            Assert.AreEqual(100, defaults[0].nextPopulation);
            Assert.AreEqual(1000, defaults[1].nextPopulation);
            Assert.AreEqual(5000, defaults[2].nextPopulation);
            Assert.AreEqual(10000, defaults[3].nextPopulation);
            Assert.AreEqual(-1, defaults[4].nextPopulation);
            Assert.AreEqual(100, defaults[1].minPopulation);
            Assert.AreEqual(1000, defaults[2].minPopulation);
            Assert.AreEqual(5000, defaults[3].minPopulation);
            Assert.AreEqual(10000, defaults[4].minPopulation);
        }

        // ===================== 3. 按人口求阶段 =====================

        [Test]
        public void GetStageForPopulation_MapsThresholds()
        {
            Assert.IsTrue(CultureStageCatalog.LoadFromFile(CultureStageCatalog.ResolveDefaultPath()), CultureStageCatalog.LastError);

            Assert.AreEqual(GameEnums.CultureStage.Band, CultureStageCatalog.GetStageForPopulation(0));
            Assert.AreEqual(GameEnums.CultureStage.Band, CultureStageCatalog.GetStageForPopulation(99));
            Assert.AreEqual(GameEnums.CultureStage.Tribe, CultureStageCatalog.GetStageForPopulation(100));
            Assert.AreEqual(GameEnums.CultureStage.Tribe, CultureStageCatalog.GetStageForPopulation(999));
            Assert.AreEqual(GameEnums.CultureStage.Chiefdom, CultureStageCatalog.GetStageForPopulation(1000));
            Assert.AreEqual(GameEnums.CultureStage.Chiefdom, CultureStageCatalog.GetStageForPopulation(4999));
            Assert.AreEqual(GameEnums.CultureStage.EthnicGroup, CultureStageCatalog.GetStageForPopulation(5000));
            Assert.AreEqual(GameEnums.CultureStage.EthnicGroup, CultureStageCatalog.GetStageForPopulation(9999));
            Assert.AreEqual(GameEnums.CultureStage.HighCivilization, CultureStageCatalog.GetStageForPopulation(10000));
            Assert.AreEqual(GameEnums.CultureStage.HighCivilization, CultureStageCatalog.GetStageForPopulation(123456));
        }

        // ===================== 4. 行为回归：JSON 加载 vs 回退默认 =====================

        [Test]
        public void EvaluateEvolution_BehaviorRegression_LoadedJsonAndFallbackMatch()
        {
            var cases = new[]
            {
                new { Stage = GameEnums.CultureStage.Band, Pop = 99, Inno = Innovations(900), Social = 0f, Spec = 0f, Urban = false, Evolve = false },
                new { Stage = GameEnums.CultureStage.Band, Pop = 100, Inno = Innovations(900), Social = 0f, Spec = 0f, Urban = false, Evolve = true },
                new { Stage = GameEnums.CultureStage.Tribe, Pop = 999, Inno = Innovations(100, 200), Social = 0.2f, Spec = 0f, Urban = false, Evolve = false },
                new { Stage = GameEnums.CultureStage.Tribe, Pop = 1000, Inno = Innovations(100, 200), Social = 0.2f, Spec = 0f, Urban = false, Evolve = true },
                new { Stage = GameEnums.CultureStage.Chiefdom, Pop = 5000, Inno = Innovations(1101), Social = 0.5f, Spec = 0.3f, Urban = false, Evolve = true },
                new { Stage = GameEnums.CultureStage.Chiefdom, Pop = 4999, Inno = Innovations(1101), Social = 0.5f, Spec = 0.3f, Urban = false, Evolve = false },
                new { Stage = GameEnums.CultureStage.EthnicGroup, Pop = 10000, Inno = Innovations(503), Social = 0.7f, Spec = 0.5f, Urban = true, Evolve = true },
                new { Stage = GameEnums.CultureStage.EthnicGroup, Pop = 10000, Inno = Innovations(503), Social = 0.7f, Spec = 0.5f, Urban = false, Evolve = false },

            };
            // 1) 加载 JSON
            Assert.IsTrue(CultureStageCatalog.LoadFromFile(CultureStageCatalog.ResolveDefaultPath()), CultureStageCatalog.LastError);
            var loadedResults = new List<StageEvolutionResult>();
            foreach (var c in cases)
            {
                StageEvolutionResult result = CultureStageEvolutionSystem.EvaluateEvolution(c.Stage, c.Pop, c.Inno, c.Social, c.Spec, c.Urban);
                Assert.AreEqual(c.Evolve, result.canEvolve, c.Stage + " pop=" + c.Pop);
                loadedResults.Add(result);
            }
            Assert.AreEqual(0.5f, loadedResults[0].progress, 0.0001f, "Band 只缺人口时应为 1/2");
            StringAssert.Contains("人口 99/100", loadedResults[0].unmetConditions[0]);
            Assert.AreEqual(1f, loadedResults[1].progress, 0.0001f);
            StringAssert.Contains("需要城市级定居点或固定营地体系", loadedResults[7].unmetConditions[0]);

            // 2) 回退默认（损坏 JSON）后同样输入应得到完全相同结果
            Assert.IsFalse(CultureStageCatalog.ApplyJson("{ broken json"), "损坏 JSON 应回退");
            Assert.IsFalse(CultureStageCatalog.IsLoadedFromJson);
            for (int i = 0; i < cases.Length; i++)
            {
                var c = cases[i];
                StageEvolutionResult fallback = CultureStageEvolutionSystem.EvaluateEvolution(c.Stage, c.Pop, c.Inno, c.Social, c.Spec, c.Urban);
                AssertSameEvolution(loadedResults[i], fallback, c.Stage + " pop=" + c.Pop);
            }

            // 终态不可进化
            StageEvolutionResult high = CultureStageEvolutionSystem.EvaluateEvolution(GameEnums.CultureStage.HighCivilization, 999999, Innovations(), 1f, 1f, true);
            Assert.IsFalse(high.canEvolve);
            Assert.AreEqual(1f, high.progress, 0.0001f);
        }

        // ===================== 5. 缺失/损坏回退 =====================

        [Test]
        public void MissingOrCorruptJson_FallsBackWithoutThrow()
        {
            string missing = Path.Combine(Path.GetTempPath(), "culture_stages_should_not_exist_98765.json");
            if (File.Exists(missing))
            {
                File.Delete(missing);
            }

            Assert.IsFalse(CultureStageCatalog.LoadFromFile(missing), "缺失文件应返回 false");
            Assert.IsFalse(CultureStageCatalog.IsLoadedFromJson);
            Assert.AreEqual(100, CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Band), "回退到代码默认阈值");
            Assert.IsTrue(CultureStageEvolutionSystem.EvaluateEvolution(GameEnums.CultureStage.Band, 100, Innovations(900), 0f, 0f, false).canEvolve);

            Assert.IsFalse(CultureStageCatalog.ApplyJson(""), "空 JSON 应返回 false");
            Assert.IsFalse(CultureStageCatalog.ApplyJson("{ not valid json"), "损坏 JSON 应返回 false 不抛");
            Assert.IsFalse(CultureStageCatalog.IsLoadedFromJson);
            Assert.AreEqual(1000, CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Tribe));
            Assert.AreEqual(0.2f, CultureStageCatalog.GetRequiredSocialStratification(GameEnums.CultureStage.Tribe), 0.0001f);
        }

        // ===================== 6. 幂等 =====================

        [Test]
        public void LoadFromFile_IsIdempotent()
        {
            string path = CultureStageCatalog.ResolveDefaultPath();
            Assert.IsTrue(CultureStageCatalog.LoadFromFile(path));
            int count = CultureStageCatalog.Stages.Count;
            Assert.AreEqual(100, CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Band));

            Assert.IsTrue(CultureStageCatalog.LoadFromFile(path), "重复加载应成功");
            Assert.AreEqual(count, CultureStageCatalog.Stages.Count, "重复加载不应累积阶段");
            Assert.AreEqual(5, CultureStageCatalog.Stages.Count);
            Assert.AreEqual(100, CultureStageCatalog.GetNextPopulationThreshold(GameEnums.CultureStage.Band));
            Assert.IsTrue(CultureStageCatalog.TryGet(4, out CultureStageDefinition high));
            Assert.AreEqual(-1, high.nextPopulation);
        }
    }
}
