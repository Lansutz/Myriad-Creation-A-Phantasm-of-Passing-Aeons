using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Politics
{
    /// <summary>
    /// 继位扶正系统：统治者死亡 → 确定继承人并扶正（君主制=继承法四轴判定，
    /// 共和制=资格过滤+威望选举）→ 争议判定（绝嗣/幼主）供政体变迁注入
    /// </summary>
    public static class SuccessionSystem
    {
        public class SuccessionResult
        {
            public bool triggered;      // 是否有统治者死亡需要处理
            public bool succeeded;      // 是否完成扶正
            public bool disputed;       // 争议（绝嗣/幼主）
            public int newRulerId = -1;
            public int deadRulerId = -1; // 死亡统治者（绰号/谥号评估用）
            public string reason = "";
        }

        /// <summary>执行继位（统治者死亡时调用；返回结果供编年史/政体变迁注入）</summary>
        public static SuccessionResult ExecuteSuccession(RealmData realm, CharacterManager characters, int day)
        {
            var result = new SuccessionResult();
            if (realm == null || characters == null) return result;

            int rulerId = realm.GetSupremeRulerId();
            var ruler = rulerId >= 0 ? characters.GetCharacter(rulerId) : null;
            if (ruler == null || ruler.isAlive) return result; // 无统治者或未死亡
            result.deadRulerId = ruler.characterId; // 死亡统治者（评估用）

            result.triggered = true;
            bool monarchy = SupremeSuccessionLevel.IsMonarchy(realm.composition);
            var candidates = CollectCandidates(realm, characters, ruler);

            CharacterData heir;
            if (monarchy)
            {
                var law = realm.GetEffectiveSuccessionLaw();
                heir = law.DetermineHeir(candidates, ruler);
            }
            else
            {
                heir = SelectByEligibility(realm, candidates);
            }

            if (heir == null)
            {
                result.disputed = true;
                result.reason = "绝嗣";
                return result;
            }
            if (heir.age < 16)
            {
                result.disputed = true;
                result.reason = "幼主";
            }

            // 扶正
            if (monarchy) realm.monarchId = heir.characterId;
            else realm.consulId = heir.characterId;
            realm.heirId = -1; // 待下次确定

            result.succeeded = true;
            result.newRulerId = heir.characterId;
            return result;
        }

        /// <summary>候选池：现任统治者的家族在世成员（排除自身）</summary>
        private static List<CharacterData> CollectCandidates(RealmData realm, CharacterManager characters, CharacterData ruler)
        {
            var list = new List<CharacterData>();
            if (ruler.familyId < 0) return list;

            var family = characters.GetFamily(ruler.familyId);
            if (family == null) return list;
            foreach (int id in family.memberIds)
            {
                var c = characters.GetCharacter(id);
                if (c != null && c.isAlive && c.characterId != ruler.characterId)
                    list.Add(c);
            }
            return list;
        }

        /// <summary>共和制继任：资格过滤（supremeEligibility）后按威望选举</summary>
        private static CharacterData SelectByEligibility(RealmData realm, List<CharacterData> candidates)
        {
            var eligible = realm.composition.supremeEligibility.Filter(candidates);
            if (eligible == null || eligible.Count == 0) return null;
            eligible.Sort((a, b) => b.prestige.CompareTo(a.prestige));
            return eligible[0];


        }

        // ===== 大圣战受益人资格（继承法判定） =====

        /// <summary>
        /// 继承线判定（IsInInheritanceLine——大圣战受益人资格）：
        /// 线宽 = min(继承顺位候选数, 可分的领地/头衔数)
        /// 顺位 ≤ 线宽 → 在线内（继承法下会继承——不能当受益人——哪怕他是儿子）
        /// 顺位 > 线宽 → 线外（轮不到——可以当受益人——哪怕他是儿子/女儿）
        /// 均分制领地不够分时顺位靠后照样线外（幼子靠圣战挣地=历史常态）
        /// </summary>
        public static bool IsInInheritanceLine(RealmData realm, CharacterManager characters, InheritanceLaw law, int characterId, int divisibleEstates)
        {
            if (realm == null || characters == null || law == null) return false;
            var rulerId = realm.GetSupremeRulerId();
            var ruler = rulerId >= 0 ? characters.GetCharacter(rulerId) : null;

            // 候选池=同 realm 角色（过滤+排序——复用继承法四轴）
            var candidates = characters.GetCharactersByRealm(realm.realmId);
            var ordered = law.GetHeirOrderedPool(candidates, ruler);
            if (ordered == null || ordered.Count == 0) return false;

            int position = ordered.FindIndex(c => c.characterId == characterId);
            if (position < 0) return false;

            // 线宽 = min(候选数, 领地可分数)——领地不够分→顺位靠后线外
            int lineWidth = Mathf.Min(ordered.Count, divisibleEstates);
            return position < lineWidth;
        }

        /// <summary>
        /// 大圣战受益人候选（继承线外者——可受益）：
        /// 候选=同 realm 角色中 IsInInheritanceLine=false 者——
        /// 继承与受益制度性分离（圣战土地永不回流家族）
        /// </summary>
        public static List<int> GetGreatHolyWarBeneficiaryCandidates(RealmData realm, CharacterManager characters, InheritanceLaw law, int divisibleEstates)
        {
            var result = new List<int>();
            if (realm == null || characters == null || law == null) return result;
            foreach (var c in characters.GetCharactersByRealm(realm.realmId))
            {
                if (c == null) continue;
                if (!IsInInheritanceLine(realm, characters, law, c.characterId, divisibleEstates))
                    result.Add(c.characterId);
            }
            return result;
        }
    }
}