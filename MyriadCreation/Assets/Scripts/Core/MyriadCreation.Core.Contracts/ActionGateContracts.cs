namespace MyriadCreation.Core.Contracts
{
    /// <summary>
    /// 行动授权来源（M2 裁决）：由头衔/官职/机构/共治/特许解析得出，替代旧的 ProposalAuthority 三值。
    /// 映射（B3 在 Simulation 侧实现，避免 Core 引 Laws）：HeadOfState → HeadOfState/CoRuler/Regent；
    /// CentralInstitution → Council/Office；CharterHolders → CharterHolder。
    /// </summary>
    public enum AuthoritySource
    {
        None = 0,
        HeadOfState = 1,
        CoRuler = 2,
        Council = 3,
        Office = 4,
        CharterHolder = 5,
        HouseholdHead = 6,
        Consensus = 7,
        Regent = 8
    }

    /// <summary>行动门禁四层：能力 / 职权 / 法理 / 资源。</summary>
    public enum ActionGateLayer
    {
        Capability = 0,
        Authority = 1,
        Legality = 2,
        Resource = 3
    }

    /// <summary>行动目标类型：立法/任命/法令等动作的目标不是"人/政权"时用它表达。</summary>
    public enum ActionTargetKind
    {
        None = 0,
        Realm = 1,
        Character = 2,
        Title = 3,
        Law = 4,
        Decree = 5,
        Settlement = 6
    }

    /// <summary>
    /// 行动上下文：谁（角色/政权/机构）、做什么、对谁。
    /// 约定（实现方必须 fail-closed）：!IsValid 或 !HasActor 的上下文一律拒绝，不得放行。
    /// </summary>
    public readonly struct ActionContext(
        int actorCharacterId,
        int actorRealmId,
        string actionId,
        int targetRealmId = -1,
        int targetCharacterId = -1,
        ActionTargetKind targetKind = ActionTargetKind.None,
        string targetId = "",
        int actorInstitutionId = -1)
    {
        public readonly int ActorCharacterId = actorCharacterId;

        public readonly int ActorRealmId = actorRealmId;

        public readonly int ActorInstitutionId = actorInstitutionId;

        public readonly string ActionId = actionId ?? string.Empty;

        public readonly int TargetRealmId = targetRealmId;

        public readonly int TargetCharacterId = targetCharacterId;

        public readonly ActionTargetKind TargetKind = targetKind;

        public readonly string TargetId = targetId ?? string.Empty;

        /// <summary>有实际行动标识才算有效；default 结构（ActionId 为空）不算。</summary>
        public bool IsValid => !string.IsNullOrEmpty(ActionId);

        public bool HasActor => IsValid && (ActorCharacterId >= 0 || ActorRealmId >= 0 || ActorInstitutionId >= 0);
    }

    /// <summary>
    /// 门禁结果：拒绝时带层 + 机器码 + 可读消息；允许时可带授权来源（凭什么授权）。
    /// </summary>
    public readonly struct GateResult(bool allowed, ActionGateLayer? failedLayer, string reasonCode, string message, AuthoritySource grantedBy)
    {
        public readonly bool Allowed = allowed;

        public readonly ActionGateLayer? FailedLayer = failedLayer;

        public readonly string ReasonCode = reasonCode ?? string.Empty;

        public readonly string Message = message ?? string.Empty;

        public readonly AuthoritySource GrantedBy = grantedBy;

        public static GateResult Allow(AuthoritySource grantedBy = AuthoritySource.None)
        {
            return new GateResult(allowed: true, failedLayer: null, reasonCode: string.Empty, message: string.Empty, grantedBy);
        }

        public static GateResult Deny(ActionGateLayer layer, string reasonCode, string message = null)
        {
            return new GateResult(allowed: false, layer, reasonCode, message, AuthoritySource.None);
        }
    }

    /// <summary>行动门禁契约：能力 ∧ 职权 ∧ 法理 ∧ 资源。实现放在 Simulation，UI 不得直接引用。</summary>
    public interface IActionGate
    {
        GateResult Check(in ActionContext context);
    }

    /// <summary>只读门禁查询（供 App/UI 经契约层询问）：与 IActionGate 同一结果类型，避免两套实现漂移。</summary>
    public interface IActionGateQuery : IActionGate
    {
    }
}
