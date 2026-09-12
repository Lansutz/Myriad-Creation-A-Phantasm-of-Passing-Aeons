using System.Collections.Generic;
using System;
using UnityEngine;

namespace CivilizationEvolution.Thought
{
    [System.Serializable]
    public class SchoolOfThought
    {
        public int schoolId;
        public string schoolName;
        public string description;
        public int founderCharacterId = -1;
        public int foundingYear;

 // 核心经典
        public List<string> coreTexts = new List<string>();
        public List<string> coreConcepts = new List<string>();

 // 学派属性
        public float metaphysicsWeight = 0.5f;    // 形而上学倾向
        public float ethicsWeight = 0.5f;          // 伦理倾向
        public float politicsWeight = 0.5f;         // 政治倾向
        public float naturalPhilosophyWeight = 0.5f; // 自然哲学倾向

 // 政治立场
        public float authoritarianism = 0.5f;  // 权威主义-自由主义轴
        public float traditionalism = 0.5f;     // 传统主义-进步主义轴
        public float collectivism = 0.5f;       // 集体主义-个体主义轴

 // 传播
        public float spreadPower = 1f;
        public float conversionRate = 0.01f;
        public List<int> followerCharacterIds = new List<int>();
        [NonSerialized] public Dictionary<int, float> regionPenetration = new Dictionary<int, float>();


 // 学派间关系
        [NonSerialized] public Dictionary<int, float> schoolRelations = new Dictionary<int, float>();


 /// <summary>计算学派综合影响力</summary>
        public float CalculateInfluence()
        {
            float totalPenetration = 0f;
            foreach (var kv in regionPenetration)
                totalPenetration += kv.Value;
            return followerCharacterIds.Count * 0.3f + totalPenetration * 0.7f;
        }

 /// <summary>每日学派Tick</summary>
        public void DailyTick()
        {
 // 传播衰减
            spreadPower = Mathf.Lerp(spreadPower, 1f, 0.001f);
        }
    }
}
