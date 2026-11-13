using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.ActionGate
{
    /// <summary>能力层提供者：回答"这个人/机构做不做得到"；不参与授权，不解锁政权行为。</summary>
    public interface ICapabilityProvider
    {
        GateResult Evaluate(in ActionContext context);
    }

    /// <summary>职权层解析器：把 actor 解析成授权来源；解析失败即无权。</summary>
    public interface IAuthorityResolver
    {
        bool TryResolve(int actorCharacterId, int actorRealmId, out AuthoritySource source);
    }

    /// <summary>法理层：CB/停战/条约/法律是否允许该行动。</summary>
    public interface ILegalityProvider
    {
        GateResult Evaluate(in ActionContext context);
    }

    /// <summary>资源层：国库/兵员/威望/合法性是否付得起。</summary>
    public interface IResourceProvider
    {
        GateResult Evaluate(in ActionContext context);
    }

    public sealed class AlwaysLegalProvider : ILegalityProvider
    {
        public GateResult Evaluate(in ActionContext context) => GateResult.Allow();
    }

    public sealed class AlwaysFundedProvider : IResourceProvider
    {
        public GateResult Evaluate(in ActionContext context) => GateResult.Allow();
    }

    /// <summary>
    /// 行动门禁：能力 ∧ 职权 ∧ 法理 ∧ 资源。当前实现能力 + 职权两层；
    /// 法理/资源层在 B3.1 接入（法律域/国库），接口保持同一 GateResult。
    /// 约定：非法上下文/无主体一律拒绝（fail-closed）。
    /// </summary>
    public sealed class ActionGate : IActionGate
    {
        private readonly ICapabilityProvider _capability;
        private readonly IAuthorityResolver _authority;
        private readonly ILegalityProvider _legality;
        private readonly IResourceProvider _resource;

        public ActionGate(ICapabilityProvider capability, IAuthorityResolver authority, ILegalityProvider legality = null, IResourceProvider resource = null)
        {
            _capability = capability;
            _authority = authority;
            _legality = legality;
            _resource = resource;
        }

        public GateResult Check(in ActionContext context)
        {
            if (!context.IsValid || !context.HasActor)
            {
                return GateResult.Deny(ActionGateLayer.Authority, "no_actor");
            }

            GateResult capability = _capability.Evaluate(in context);
            if (!capability.Allowed)
            {
                return capability;
            }

            if (!_authority.TryResolve(context.ActorCharacterId, context.ActorRealmId, out AuthoritySource source))
            {
                return GateResult.Deny(ActionGateLayer.Authority, "not_authorized");
            }

            GateResult legality = _legality != null ? _legality.Evaluate(in context) : GateResult.Allow();
            if (!legality.Allowed)
            {
                return legality;
            }

            GateResult resource = _resource != null ? _resource.Evaluate(in context) : GateResult.Allow();
            if (!resource.Allowed)
            {
                return resource;
            }

            return GateResult.Allow(source);
        }
    }

    /// <summary>默认能力层：不设能力门槛（能力只影响执行质量，不影响权限）。</summary>
    public sealed class NoCapabilityGate : ICapabilityProvider
    {
        public GateResult Evaluate(in ActionContext context)
        {
            return GateResult.Allow();
        }
    }
}
