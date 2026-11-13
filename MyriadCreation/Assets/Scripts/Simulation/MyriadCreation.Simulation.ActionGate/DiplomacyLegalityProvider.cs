using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.ActionGate
{
    /// <summary>法理层（宣战）：关系存在、未交战、停战期未满。其他行动当前放行。</summary>
    public sealed class DiplomacyLegalityProvider : ILegalityProvider
    {
        private readonly DiplomacyManager _diplomacy;

        public DiplomacyLegalityProvider(DiplomacyManager diplomacy)
        {
            _diplomacy = diplomacy;
        }

        public GateResult Evaluate(in ActionContext context)
        {
            if (context.ActionId != ActionIds.DeclareWar)
            {
                return GateResult.Allow();
            }

            if (context.TargetRealmId < 0)
            {
                return GateResult.Deny(ActionGateLayer.Legality, "no_target");
            }

            DiplomaticRelation relation = _diplomacy != null ? _diplomacy.GetRelation(context.ActorRealmId, context.TargetRealmId) : null;
            if (relation == null)
            {
                return GateResult.Deny(ActionGateLayer.Legality, "no_relation");
            }

            if (relation.isAtWar)
            {
                return GateResult.Deny(ActionGateLayer.Legality, "already_at_war");
            }

            if (relation.truceUntilDay >= 0 && _diplomacy.CurrentDay < relation.truceUntilDay)
            {
                return GateResult.Deny(ActionGateLayer.Legality, "in_truce");
            }

            return GateResult.Allow();
        }
    }
}
