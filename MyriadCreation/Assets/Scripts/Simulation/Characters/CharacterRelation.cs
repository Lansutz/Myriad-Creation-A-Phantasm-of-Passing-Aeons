using System.Collections.Generic;
using System;

namespace CivilizationEvolution.Simulation.Characters
{
    [Serializable]
    public struct CharacterRelation
    {
        public int otherCharacterId;
        [UnityEngine.Range(-200f, 200f)] public float opinion;  // 好感度（企划书：-200~200，双向不对称存储）
        public RelationshipType type;
        public List<string> history;

        public float trust;
        public float fear;
        public float romanticAttraction;
    }
}
