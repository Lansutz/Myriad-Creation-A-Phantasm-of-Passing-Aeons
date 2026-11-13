using System;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Simulation.ActionGate
{
    /// <summary>资源层（宣战）：发起方国库需达到最低门槛。其他行动当前放行。</summary>
    public sealed class RealmTreasuryProvider : IResourceProvider
    {
        private readonly Func<int, float> _treasuryLookup;
        private readonly float _minTreasury;

        public RealmTreasuryProvider(Func<int, float> treasuryLookup, float minTreasury = 0f)
        {
            _treasuryLookup = treasuryLookup;
            _minTreasury = minTreasury;
        }

        public GateResult Evaluate(in ActionContext context)
        {
            if (context.ActionId != ActionIds.DeclareWar)
            {
                return GateResult.Allow();
            }

            if (_treasuryLookup == null || context.ActorRealmId < 0)
            {
                return GateResult.Deny(ActionGateLayer.Resource, "no_treasury_source");
            }

            if (_treasuryLookup(context.ActorRealmId) < _minTreasury)
            {
                return GateResult.Deny(ActionGateLayer.Resource, "insufficient_treasury");
            }

            return GateResult.Allow();
        }
    }
}
