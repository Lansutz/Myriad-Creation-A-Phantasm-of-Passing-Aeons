using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.LawHost;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P9/M2：法律与法令必须由革新解锁；门槛 id 必须能在革新目录里解析。
    /// JSON 是接线的唯一来源，测试侧不再维护一份副本。
    /// </summary>
    public class LawInnovationGateTests
    {
        [SetUp]
        public void Setup()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            LawContentLoader.Reset();
            string path = LawContentLoader.ResolveDefaultPath();
            Assert.IsTrue(LawContentLoader.LoadFromFile(path), LawContentLoader.LastError);
        }

        private static IEnumerable<LawDef> AllLaws()
        {
            foreach (LawGroupDef group in LawContentLoader.Groups)
            {
                if (group?.laws == null) continue;
                foreach (LawDef law in group.laws)
                {
                    if (law != null) yield return law;
                }
            }
        }

        [Test]
        public void EveryLaw_HasInnovationGate()
        {
            var problems = new List<string>();
            foreach (LawDef law in AllLaws())
            {
                if (law.requirements == null || law.requirements.requiredInnovationIds == null
                    || law.requirements.requiredInnovationIds.Count == 0)
                {
                    problems.Add(law.lawId + " 没有 requiredInnovationIds");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void EveryDecree_HasInnovationGate()
        {
            var problems = new List<string>();
            foreach (DecreeDef decree in LawContentLoader.Decrees)
            {
                if (decree?.requirements == null || decree.requirements.requiredInnovationIds == null
                    || decree.requirements.requiredInnovationIds.Count == 0)
                {
                    problems.Add(decree?.decreeId + " 没有 requiredInnovationIds");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void InnovationGateIds_ResolveInCatalog()
        {
            var problems = new List<string>();
            foreach (LawDef law in AllLaws())
            {
                foreach (int id in law.requirements.requiredInnovationIds)
                {
                    if (!ContentRegistry.Innovations.ContainsKey(id))
                    {
                        problems.Add(law.lawId + " 引用不存在的革新 " + id);
                    }
                }
            }
            foreach (DecreeDef decree in LawContentLoader.Decrees)
            {
                foreach (int id in decree.requirements.requiredInnovationIds)
                {
                    if (!ContentRegistry.Innovations.ContainsKey(id))
                    {
                        problems.Add(decree.decreeId + " 引用不存在的革新 " + id);
                    }
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void TierTwoAndAbove_HaveAtLeastOneGate()
        {
            var problems = new List<string>();
            foreach (LawDef law in AllLaws())
            {
                if (law.tier >= 2 && law.requirements.requiredInnovationIds.Count < 1)
                {
                    problems.Add(law.lawId + " tier" + law.tier + " 至少需要一条革新前置");
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void FoundingKnowledge_ResolvesInCatalog()
        {
            foreach (int id in MyriadCreation.Simulation.Systems.StateFormationSystem.FoundingKnowledge)
            {
                Assert.IsTrue(ContentRegistry.Innovations.ContainsKey(id), "开国基础革新不存在：" + id);
            }
        }
    }
}
