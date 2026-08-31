using System.Collections.Generic;
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
    }
}
