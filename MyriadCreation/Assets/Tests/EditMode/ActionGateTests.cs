using NUnit.Framework;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.ActionGate;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>B3：ActionGate 三分支（越权拒绝 / 授权通过 / 能力不影响权限）。</summary>
    public class ActionGateTests
    {
        private sealed class FakeAuthorityResolver : IAuthorityResolver
        {
            public int RulerCharacterId;
            public int RealmId;

            public bool TryResolve(int actorCharacterId, int actorRealmId, out AuthoritySource source)
            {
                if (actorCharacterId == RulerCharacterId && actorRealmId == RealmId)
                {
                    source = AuthoritySource.HeadOfState;
                    return true;
                }
                source = AuthoritySource.None;
                return false;
            }
        }

        private sealed class DenyCapability : ICapabilityProvider
        {
            public GateResult Evaluate(in ActionContext context)
            {
                return GateResult.Deny(ActionGateLayer.Capability, "incapable");
            }
        }

        private static ActionGate BuildGate(IAuthorityResolver resolver, ICapabilityProvider capability = null)
        {
            return new ActionGate(capability ?? new NoCapabilityGate(), resolver);
        }

        [Test]
        public void NonRuler_IsDeniedByAuthority()
        {
            var gate = BuildGate(new FakeAuthorityResolver { RulerCharacterId = 1, RealmId = 5 });
            ActionContext context = new ActionContext(2, 5, ActionIds.DeclareWar);

            GateResult result = gate.Check(in context);

            Assert.IsFalse(result.Allowed);
            Assert.AreEqual(ActionGateLayer.Authority, result.FailedLayer);
            Assert.AreEqual("not_authorized", result.ReasonCode);
        }

        [Test]
        public void Ruler_IsAllowedWithGrantedBy()
        {
            var gate = BuildGate(new FakeAuthorityResolver { RulerCharacterId = 1, RealmId = 5 });
            ActionContext context = new ActionContext(1, 5, ActionIds.DeclareWar);

            GateResult result = gate.Check(in context);

            Assert.IsTrue(result.Allowed);
            Assert.AreEqual(AuthoritySource.HeadOfState, result.GrantedBy);
        }

        [Test]
        public void Capability_DoesNotChangePermission_OnlyQuality()
        {
            var resolver = new FakeAuthorityResolver { RulerCharacterId = 1, RealmId = 5 };
            var capable = BuildGate(resolver, new NoCapabilityGate());
            var incapable = BuildGate(resolver, new DenyCapability());
            ActionContext context = new ActionContext(1, 5, ActionIds.DeclareWar);

            Assert.IsTrue(capable.Check(in context).Allowed, "有授权即通过，能力不参与授权");
            GateResult denied = incapable.Check(in context);
            Assert.IsFalse(denied.Allowed, "能力层可以独立拒绝（作为能力门槛，而非职权）");
            Assert.AreEqual(ActionGateLayer.Capability, denied.FailedLayer);
        }

        [Test]
        public void NoActor_FailsClosed()
        {
            var gate = BuildGate(new FakeAuthorityResolver { RulerCharacterId = 1, RealmId = 5 });
            ActionContext context = new ActionContext(-1, -1, "");

            GateResult result = gate.Check(in context);

            Assert.IsFalse(result.Allowed);
            Assert.AreEqual("no_actor", result.ReasonCode);
        }
        private sealed class DenyLegality : ILegalityProvider
        {
            public GateResult Evaluate(in ActionContext context)
            {
                return GateResult.Deny(ActionGateLayer.Legality, "no_cb");
            }
        }

        private sealed class DenyResource : IResourceProvider
        {
            public GateResult Evaluate(in ActionContext context)
            {
                return GateResult.Deny(ActionGateLayer.Resource, "no_treasury");
            }
        }

        [Test]
        public void LegalityLayer_CanDeny()
        {
            var gate = new ActionGate(new NoCapabilityGate(), new FakeAuthorityResolver { RulerCharacterId = 1, RealmId = 5 }, new DenyLegality());
            ActionContext context = new ActionContext(1, 5, ActionIds.DeclareWar);
            GateResult result = gate.Check(in context);
            Assert.IsFalse(result.Allowed);
            Assert.AreEqual(ActionGateLayer.Legality, result.FailedLayer);
            Assert.AreEqual("no_cb", result.ReasonCode);
        }

        [Test]
        public void ResourceLayer_CanDeny()
        {
            var gate = new ActionGate(new NoCapabilityGate(), new FakeAuthorityResolver { RulerCharacterId = 1, RealmId = 5 }, new AlwaysLegalProvider(), new DenyResource());
            ActionContext context = new ActionContext(1, 5, ActionIds.DeclareWar);
            GateResult result = gate.Check(in context);
            Assert.IsFalse(result.Allowed);
            Assert.AreEqual(ActionGateLayer.Resource, result.FailedLayer);
            Assert.AreEqual("no_treasury", result.ReasonCode);
        }

        [Test]
        public void RealTreasuryProvider_DeniesBelowThreshold()
        {
            var provider = new RealmTreasuryProvider((int realmId) => 100f, 200f);
            ActionContext context = new ActionContext(1, 5, ActionIds.DeclareWar);
            Assert.IsFalse(provider.Evaluate(in context).Allowed);
        }

        [Test]
        public void RealTreasuryProvider_AllowsAboveThreshold()
        {
            var provider = new RealmTreasuryProvider((int realmId) => 500f, 200f);
            ActionContext context = new ActionContext(1, 5, ActionIds.DeclareWar);
            Assert.IsTrue(provider.Evaluate(in context).Allowed);
        }

    }
}
