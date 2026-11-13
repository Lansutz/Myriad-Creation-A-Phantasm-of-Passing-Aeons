using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>法律互斥、法律前置、法令前置、法令互斥。</summary>
    public class LawAndDecreeConflictTests
    {
        private static LawDef Law(string id, string group, params string[] excludes)
        {
            return new LawDef
            {
                lawId = id, groupId = group, name = id,
                excludesLawIds = new List<string>(excludes),
                isInitiallyKnown = true,
            };
        }

        private static DecreeDef Decree(string id, params string[] requiredDecrees)
        {
            return new DecreeDef
            {
                decreeId = id, name = id,
                requirements = new LawRequirement { requiredDecreeIds = new List<string>(requiredDecrees) },
            };
        }

        [Test]
        public void Laws_CanBeMutuallyExclusive()
        {
            var state = new RealmLawState();
            state.SetActiveLaw("a", "law_a", 1);
            var b = Law("law_b", "b", "law_a");
            Assert.IsFalse(LawAndDecreeSystem.CanEnactLaw(b, state, 1000f, 100f, 80f, 0.5f, null, out var reason));
            StringAssert.Contains("互斥", reason);
        }

        [Test]
        public void Decree_RequiresPriorDecree()
        {
            var lawState = new RealmLawState();
            var decreeState = new RealmDecreeState();
            var prerequisite = Decree("decree_a");
            var target = Decree("decree_b", "decree_a");
            Assert.IsFalse(LawAndDecreeSystem.CanEnactDecree(target, lawState, decreeState, 1000f, 100f, 80f, 10, null, out var reason));
            StringAssert.Contains("前置法令", reason);
            decreeState.active.Add(new ActiveDecree { decreeId = "decree_a", startDay = 1, endDay = 100 });
            Assert.IsTrue(LawAndDecreeSystem.CanEnactDecree(target, lawState, decreeState, 1000f, 100f, 80f, 10, null, out _));
            Assert.IsNotNull(prerequisite);
        }

        [Test]
        public void Decrees_CanBeMutuallyExclusive()
        {
            var lawState = new RealmLawState();
            var decreeState = new RealmDecreeState();
            decreeState.active.Add(new ActiveDecree { decreeId = "decree_a", startDay = 1, endDay = 100 });
            var b = Decree("decree_b");
            b.excludesDecreeIds = new List<string> { "decree_a" };
            Assert.IsFalse(LawAndDecreeSystem.CanEnactDecree(b, lawState, decreeState, 1000f, 100f, 80f, 10, null, out var reason));
            StringAssert.Contains("互斥", reason);
        }
    }
}
