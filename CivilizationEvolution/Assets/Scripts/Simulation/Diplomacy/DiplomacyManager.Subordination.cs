using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;
using CivilizationEvolution.Character;

namespace CivilizationEvolution.Diplomacy
{
 /// DiplomacyManager.Subordination —— 从属关系（朝贡/附庸/附属/保护国/傀儡/共主邦联/独立）（partial class，与 DiplomacySystem.cs 共享字段）
    public partial class DiplomacyManager
    {

 // ===== 从属关系 =====
 /// <summary>建立从属关系</summary>
        public Subordination EstablishSubordination(int suzerainId, int vassalId, SubordinationType type)
        {
            var rel = GetRelation(suzerainId, vassalId);
            if (rel == null) return null;

            var sub = new Subordination
            {
                type = type,
                suzerainId = suzerainId,
                vassalId = vassalId,
                establishedDay = CurrentDay
            };

 // 设置从属条款（5种不平等从属：自治度 朝贡0.9→附庸0.65→附属0.45→保护国0.3→傀儡0.1）
            switch (type)
            {
                case SubordinationType.Tributary:
 // 朝贡：内政完全自主，象征性臣服+进贡
                    sub.tributeRatio = 0.1f;
                    sub.autonomy = 0.9f;
                    break;
                case SubordinationType.Vassal:
 // 附庸国：外交权受限，军事义务，内政基本自主
                    sub.tributeRatio = 0.15f;
                    sub.militaryObligation = true;
                    sub.foreignPolicyControl = true;
                    sub.autonomy = 0.65f;
                    break;
                case SubordinationType.Associate:
 // 附属：内政受法定监督（顾问/否决法律），外交国防全权代理
                    sub.foreignPolicyControl = true;
                    sub.militaryObligation = true;
                    sub.autonomy = 0.45f;
                    break;
                case SubordinationType.Protectorate:
 // 保护国：内政自主，外交与宣战权完全转让
                    sub.foreignPolicyControl = true;
                    sub.autonomy = 0.3f;
                    break;
                case SubordinationType.Puppet:
 // 傀儡：首脑由宗主指定，一切重大决策需批准
                    sub.foreignPolicyControl = true;
                    sub.militaryObligation = true;
                    sub.successionControl = true;
                    sub.autonomy = 0.1f;
                    break;
            }

            _subordinations.Add(sub);
            rel.subordination = sub; // 同步挂载到关系槽位1

            if (_realms.TryGetValue(vassalId, out var vassal))
                vassal.suzerainId = suzerainId;
            if (_realms.TryGetValue(suzerainId, out var suzerain))
                suzerain.vassalIds.Add(vassalId);

            return sub;
        }


 /// <summary>获取两个政权之间的从属关系</summary>
        public Subordination GetSubordination(int realmA, int realmB)
        {
            return _subordinations.Find(s => s.isActive &&
                ((s.suzerainId == realmA && s.vassalId == realmB) ||
                 (s.suzerainId == realmB && s.vassalId == realmA)));
        }


 /// <summary>解除从属关系</summary>
        public bool ReleaseSubordination(int suzerainId, int vassalId)
        {
            var sub = _subordinations.Find(s => s.suzerainId == suzerainId && s.vassalId == vassalId && s.isActive);
            if (sub == null) return false;

            sub.isActive = false;
            var rel = GetRelation(suzerainId, vassalId);
            if (rel != null) rel.subordination = null;

            ModifyRelation(suzerainId, vassalId, -15f, "解除从属关系");
            Debug.Log($"[Diplomacy] 政权 {suzerainId} 解除对 {vassalId} 的从属关系");
            return true;
        }


 /// 建立特殊纽带（谱系三：君合国/共主邦联——横向人身/王朝联合）
 /// 独立于从属与盟约：双方各自保留主权，仅共享君主
        public bool EstablishPersonalUnion(int realmA, int realmB, SpecialBondType bond)
        {
            if (realmA == realmB || bond == SpecialBondType.None) return false;
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return false;
            if (rel.subordination != null && rel.subordination.isActive)
            {
                Debug.LogWarning("[Diplomacy] 存在从属关系时不可建立君合国（主权状态与特殊纽带互斥）");
                return false;
            }
            rel.SetSpecialBond(bond);
            Debug.Log($"[Diplomacy] 政权 {realmA} 与 {realmB} 建立特殊纽带：{bond}");
            return true;
        }


 /// <summary>附庸独立</summary>
        public bool GrantIndependence(int suzerainId, int vassalId)
        {
            var sub = _subordinations.Find(s => s.suzerainId == suzerainId && s.vassalId == vassalId && s.isActive);
            if (sub == null) return false;

            sub.isActive = false;
            _subordinations.Remove(sub);

 // 清理关系槽位1
            var rel = GetRelation(suzerainId, vassalId);
            if (rel != null && rel.subordination == sub)
                rel.subordination = null;

            if (_realms.TryGetValue(vassalId, out var vassal))
                vassal.suzerainId = -1;
            if (_realms.TryGetValue(suzerainId, out var suzerain))
                suzerain.vassalIds.Remove(vassalId);

            ModifyRelation(suzerainId, vassalId, -20f, "附庸独立");
            return true;
        }

        public IReadOnlyList<Subordination> GetAllSubordinations() => _subordinations;

    }
}
