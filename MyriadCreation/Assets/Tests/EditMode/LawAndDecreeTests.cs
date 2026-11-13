using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>法律组唯一 active、成本/条件、法令时限/冷却。</summary>
    public class LawAndDecreeTests
    {
        private static LawDef FindLaw(string id)
        {
            var groups = DefaultLawCatalog.CreateGroups();
            return DefaultLawCatalog.IndexLaws(groups)[id];
        }

        private static DecreeDef FindDecree(string id)
        {
            return DefaultLawCatalog.IndexDecrees(DefaultLawCatalog.CreateDecrees())[id];
        }

        [Test]
        public void EnactLaw_DeductsCostAndKeepsOnePerGroup()
        {
            var state = new RealmLawState();
            float treasury = 500f, prestige = 100f;
            var light = FindLaw("tax_light");
            var heavy = FindLaw("tax_heavy");

            Assert.IsNotNull(LawAndDecreeSystem.EnactLaw(light, state, ref treasury, ref prestige, 1,
                60f, 0.5f, null, out string reason), reason);
            Assert.AreEqual("tax_light", state.GetActiveLaw("taxation"));
            Assert.Less(treasury, 500f);

            Assert.IsNotNull(LawAndDecreeSystem.EnactLaw(heavy, state, ref treasury, ref prestige, 2,
                60f, 0.5f, null, out reason), reason);
            Assert.AreEqual("tax_heavy", state.GetActiveLaw("taxation"), "同组只能留一个 active");
        }

        [Test]
        public void EnactLaw_RejectsInsufficientResources()
        {
            var state = new RealmLawState();
            float treasury = 1f, prestige = 100f;
            bool ok = LawAndDecreeSystem.CanEnactLaw(FindLaw("tax_heavy"), state, treasury, prestige,
                60f, 0.5f, null, out string reason);
            Assert.IsFalse(ok);
            StringAssert.Contains("财政", reason);
        }

        [Test]
        public void Decree_ExpiresAndCoolsDown()
        {
            var lawState = new RealmLawState();
            var decreeState = new RealmDecreeState();
            float treasury = 1000f, authority = 100f;
            var decree = FindDecree("decree_tax_relief");
            var active = LawAndDecreeSystem.EnactDecree(decree, lawState, decreeState,
                ref treasury, ref authority, 60f, 1, null, out string reason);
            Assert.IsNotNull(active, reason);
            Assert.AreEqual(1, decreeState.active.Count);
            Assert.IsFalse(LawAndDecreeSystem.CanEnactDecree(decree, lawState, decreeState,
                treasury, authority, 60f, 2, null, out _));

            LawAndDecreeSystem.TickDecreeExpiry(decreeState, active.endDay + 1);
            Assert.AreEqual(0, decreeState.active.Count);
            // 仍处于冷却
            Assert.IsFalse(LawAndDecreeSystem.CanEnactDecree(decree, lawState, decreeState,
                treasury, authority, 60f, active.endDay + 2, null, out string cooldownReason));
            StringAssert.Contains("冷却", cooldownReason);
        }

        [Test]
        public void Decree_RequiresPrerequisiteLaw()
        {
            var lawState = new RealmLawState();
            var decreeState = new RealmDecreeState();
            bool ok = LawAndDecreeSystem.CanEnactDecree(FindDecree("decree_levy"), lawState, decreeState,
                1000f, 100f, 60f, 1, null, out string reason);
            Assert.IsFalse(ok);
            StringAssert.Contains("前置法律", reason);
        }

        [Test]
        public void Decree_CanBeRevoked()
        {
            var lawState = new RealmLawState();
            var decreeState = new RealmDecreeState();
            float treasury = 1000f, authority = 100f;
            var active = LawAndDecreeSystem.EnactDecree(FindDecree("decree_tax_relief"),
                lawState, decreeState, ref treasury, ref authority, 60f, 1, null, out _);
            Assert.IsNotNull(active);
            Assert.IsTrue(LawAndDecreeSystem.RevokeDecree(decreeState, active.decreeId, out var revoked));
            Assert.AreEqual(active.decreeId, revoked.decreeId);
            Assert.AreEqual(0, decreeState.active.Count);
        }

        [Test]
        public void Effects_SumOnlyActiveLaws()
        {
            var state = new RealmLawState();
            var law = FindLaw("tax_heavy");
            state.SetActiveLaw(law.groupId, law.lawId, 1);
            var index = DefaultLawCatalog.IndexLaws(DefaultLawCatalog.CreateGroups());
            var effects = LawAndDecreeSystem.SumLawEffects(state, index);
            Assert.AreEqual(1.4f, effects.taxMultiplier, 0.001f);
            Assert.Less(effects.stabilityPerYear, 0f);
        }
    }
}
