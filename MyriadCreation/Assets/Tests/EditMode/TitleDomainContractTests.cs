using NUnit.Framework;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>B1：行动门禁契约与派生头衔分类的纯数据测试（不接线、不落盘）。</summary>
    public class TitleDomainContractTests
    {
        private sealed class AllowAllGate : IActionGate
        {
            public GateResult Check(in ActionContext context)
            {
                return GateResult.Allow();
            }
        }

        private sealed class DenyWithoutActorGate : IActionGateQuery
        {
            public GateResult Check(in ActionContext context)
            {
                if (!context.HasActor)
                {
                    return GateResult.Deny(ActionGateLayer.Authority, "no_actor", "没有行动主体");
                }
                return GateResult.Allow(AuthoritySource.HeadOfState);
            }
        }

        [Test]
        public void GateResult_AllowHasNoFailedLayerAndCanCarryGrantedBy()
        {
            GateResult allow = GateResult.Allow(AuthoritySource.Regent);
            Assert.That(allow.Allowed, Is.True);
            Assert.That(allow.FailedLayer.HasValue, Is.False);
            Assert.That(allow.GrantedBy, Is.EqualTo(AuthoritySource.Regent));
            Assert.That(allow.ReasonCode, Is.Empty);
        }

        [Test]
        public void GateResult_DenyCarriesLayerCodeAndMessage()
        {
            GateResult deny = GateResult.Deny(ActionGateLayer.Authority, "no_authority", "无授权");
            Assert.That(deny.Allowed, Is.False);
            Assert.That(deny.FailedLayer, Is.EqualTo(ActionGateLayer.Authority));
            Assert.That(deny.ReasonCode, Is.EqualTo("no_authority"));
            Assert.That(deny.Message, Is.EqualTo("无授权"));
            Assert.That(deny.GrantedBy, Is.EqualTo(AuthoritySource.None));
        }

        [Test]
        public void ActionContext_DefaultsAndHasActor()
        {
            ActionContext context = new ActionContext(7, -1, "declare_war", targetRealmId: 9);
            Assert.That(context.IsValid, Is.True);
            Assert.That(context.HasActor, Is.True);
            Assert.That(context.ActionId, Is.EqualTo("declare_war"));
            Assert.That(context.TargetRealmId, Is.EqualTo(9));
            Assert.That(context.TargetCharacterId, Is.EqualTo(-1));
            Assert.That(context.TargetKind, Is.EqualTo(ActionTargetKind.None));

            ActionContext empty = default;
            Assert.That(empty.IsValid, Is.False);
            Assert.That(empty.HasActor, Is.False);
            Assert.That(string.IsNullOrEmpty(empty.ActionId), Is.True);
        }

        [Test]
        public void ActionContext_CanCarryLawTargetAndInstitutionActor()
        {
            ActionContext context = new ActionContext(
                actorCharacterId: -1,
                actorRealmId: 3,
                actionId: "enact_law",
                targetKind: ActionTargetKind.Law,
                targetId: "law_tax_1",
                actorInstitutionId: 12);
            Assert.That(context.HasActor, Is.True);
            Assert.That(context.TargetKind, Is.EqualTo(ActionTargetKind.Law));
            Assert.That(context.TargetId, Is.EqualTo("law_tax_1"));
            Assert.That(context.ActorInstitutionId, Is.EqualTo(12));
        }

        [Test]
        public void ActionGate_ContractIsImplementable()
        {
            IActionGate gate = new AllowAllGate();
            ActionContext context = new ActionContext(1, 2, "test_action");
            GateResult result = gate.Check(in context);
            Assert.That(result.Allowed, Is.True);
        }

        [Test]
        public void ActionGateQuery_FailsClosedWhenNoActor()
        {
            IActionGateQuery gate = new DenyWithoutActorGate();
            ActionContext noActor = new ActionContext(-1, -1, "declare_war");
            GateResult denied = gate.Check(in noActor);
            Assert.That(denied.Allowed, Is.False);
            Assert.That(denied.FailedLayer, Is.EqualTo(ActionGateLayer.Authority));
            Assert.That(denied.ReasonCode, Is.EqualTo("no_actor"));

            ActionContext withActor = new ActionContext(1, 2, "declare_war");
            GateResult allowed = gate.Check(in withActor);
            Assert.That(allowed.Allowed, Is.True);
            Assert.That(allowed.GrantedBy, Is.EqualTo(AuthoritySource.HeadOfState));
        }

        [Test]
        public void AuthoritySource_HasSevenSourcesPlusNoneAndRegent()
        {
            Assert.That(System.Enum.GetValues(typeof(AuthoritySource)).Length, Is.EqualTo(9));
        }

        [Test]
        public void ActionGateLayer_HasFourLayers()
        {
            Assert.That(System.Enum.GetValues(typeof(ActionGateLayer)).Length, Is.EqualTo(4));
        }

        [Test]
        public void TitleDerivedCategory_HasFiveDerivedValues()
        {
            Assert.That(System.Enum.GetValues(typeof(TitleDerivedCategory)).Length, Is.EqualTo(5));
        }
    }
}
