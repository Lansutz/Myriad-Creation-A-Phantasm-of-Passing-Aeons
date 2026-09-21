using System;

namespace CivilizationEvolution.Simulation.Diplomacy
{
        [Serializable]
        public class NegotiationClause
        {
            public TreatyClauseType type;
            public string description;
            public int cost;
            public float value = 0f;
            public int durationDays = 365 * 5;
            public bool isDemanded = true;
        }
}
