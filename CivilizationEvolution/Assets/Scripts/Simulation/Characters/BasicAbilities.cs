using System;
using UnityEngine;

namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 基本能力（底层、通用的身体素质和心智能力）
    /// 四大类，每类三个子属性，0-100
    /// 专精 = 基本能力 + 学到的技能
    /// </summary>
    [Serializable]
    public class BasicAbilities
    {
        // ===== 体魄 =====
        [Range(0f, 100f)] public float strength = 50f;     // 力量：肌肉力量和爆发力
        [Range(0f, 100f)] public float coordination = 50f; // 协调：身体协调性和灵巧度
        [Range(0f, 100f)] public float endurance = 50f;    // 坚韧：耐力和抗疲劳能力

        // ===== 心智 =====
        [Range(0f, 100f)] public float memory = 50f;       // 记忆：记忆力和知识留存
        [Range(0f, 100f)] public float logic = 50f;        // 逻辑：逻辑推理和分析能力
        [Range(0f, 100f)] public float inspiration = 50f;  // 灵感：创造力和直觉思维

        // ===== 交涉 =====
        [Range(0f, 100f)] public float eloquence = 50f;    // 口才：表达能力和说服力
        [Range(0f, 100f)] public float insight = 50f;      // 洞察：读懂他人意图和情绪的能力
        [Range(0f, 100f)] public float charisma = 50f;     // 气度：个人魅力和气场

        // ===== 感知 =====
        [Range(0f, 100f)] public float observation = 50f;  // 观察：观察力和细节捕捉
        [Range(0f, 100f)] public float intuition = 50f;    // 直觉：直觉判断和预感
        [Range(0f, 100f)] public float reaction = 50f;     // 反应：反应速度和应变能力

        /// <summary>体魄综合值</summary>
        public float Physique => (strength + coordination + endurance) / 3f;

        /// <summary>心智综合值</summary>
        public float Mind => (memory + logic + inspiration) / 3f;

        /// <summary>交涉综合值</summary>
        public float Diplomacy => (eloquence + insight + charisma) / 3f;

        /// <summary>感知综合值</summary>
        public float Perception => (observation + intuition + reaction) / 3f;

        /// <summary>全部基本能力的平均值</summary>
        public float Overall => (Physique + Mind + Diplomacy + Perception) / 4f;

        /// <summary>随机生成基本能力（用于角色创建）</summary>
        public static BasicAbilities Random(float baseValue = 50f, float range = 30f)
        {
            var ab = new BasicAbilities();
            ab.strength = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.coordination = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.endurance = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.memory = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.logic = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.inspiration = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.eloquence = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.insight = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.charisma = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.observation = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.intuition = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            ab.reaction = Mathf.Clamp(baseValue + UnityEngine.Random.Range(-range, range), 5f, 95f);
            return ab;
        }
    }
}
