using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Thought;
using CivilizationEvolution.War;

namespace CivilizationEvolution.Character
{
 /// CharacterManager.Faith —— 个人信仰（私人信仰/个人信条/信仰分歧/虔诚更新）（partial class，与 CharacterSystem.cs 共享字段）
    public partial class CharacterManager
    {
 // ===== 三信仰形态（社会/私人/秘密） =====
 /// <summary>初始化私人信仰（角色创建时=社会信仰一致）</summary>
        public void InitPrivateFaith(CharacterData c)
        {
            if (c == null || c.privateFaithId == -1)
            {
                if (c != null) c.privateFaithId = c.faithId;
            }
        }


 /// 添加个人信条（Personal Tenet——本人信条）
 /// 来源：借其他信仰（个人融合）/组合原有/自创——与官方教义冲突→偏离度
        public bool AddPersonalTenet(CharacterData c, string optionId)
        {
            if (c == null || string.IsNullOrEmpty(optionId)) return false;
            if (c.personalTenets.Contains(optionId)) return false;
            c.personalTenets.Add(optionId);
 // 私人信仰偏离社会信仰 → 可能触发秘密信仰（由外部判定——此处只记录）
            return true;
        }


 /// <summary>移除个人信条</summary>
        public bool RemovePersonalTenet(CharacterData c, string optionId)
        {
            if (c == null) return false;
            return c.personalTenets.Remove(optionId);
        }


 /// <summary>私人信仰是否偏离社会信仰（信条差异——偏离度>0）</summary>
        public bool HasFaithDivergence(CharacterData c)
        {
            if (c == null) return false;
            if (c.privateFaithId != c.faithId) return true;
            return c.personalTenets.Count > 0;
        }


 /// <summary>秘密信仰状态更新（私人≠社会且被禁止时=true——暴露风险）</summary>
        public void UpdateSecretBelief(CharacterData c)
        {
            if (c == null) return;
            c.isSecretBeliever = HasFaithDivergence(c);
        }


 /// 灵性满足每日更新（角色虔诚——体验支柱动态化）：
 /// 美德特质 +0.01/日（等级制——3 级美德 +0.03）｜罪行特质 -0.02/日｜
 /// 秘密信徒（私人≠社会）额外 -0.01（信仰撕裂——灵性煎熬）
 /// 虔诚上限型：0-100——高虔诚=圣人候选资格/统治合法性加成
        public void UpdatePiety(CharacterData c, FaithSystem faith)
        {
            if (c == null) return;
            float delta = 0f;
            if (faith != null)
            {
                delta += faith.GetVirtueScore(c) * 0.01f;
                delta -= faith.GetSinScore(c) * 0.02f;
            }
            if (c.isSecretBeliever) delta -= 0.01f;
            c.spiritualFulfillment = Mathf.Clamp(c.spiritualFulfillment + delta, 0f, 100f);
        }

    }
}
