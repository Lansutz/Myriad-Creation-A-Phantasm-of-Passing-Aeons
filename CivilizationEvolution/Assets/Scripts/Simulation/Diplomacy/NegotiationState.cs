using System.Collections.Generic;
using System;
using UnityEngine;

namespace CivilizationEvolution.Simulation.Diplomacy
{
        [Serializable]
        public class NegotiationState
        {
            public int attackerId;
            public int defenderId;
            public float attackerWarScore;
            public float defenderWarScore;
            public List<NegotiationClause> attackerDemands = new List<NegotiationClause>();
            public List<NegotiationClause> attackerConcessions = new List<NegotiationClause>();

            public int AttackerTotalCost
            {
                get
                {
                    int total = 0;
                    foreach (var c in attackerDemands) total += c.cost;
                    foreach (var c in attackerConcessions) total -= c.cost;
                    return total;
                }
            }

            public bool IsAttackerWithinLimit => AttackerTotalCost <= Mathf.CeilToInt(attackerWarScore);
        }
}
