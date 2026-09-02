using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Role;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.War
{
    /// <summary>
    /// 大圣战状态（特殊机制——区别于寻常圣战）：
    /// 号召制（教宗/哈里发号召——该教统/宗教的其他政权参战）
    /// 战争金库（贡献——简化：参与者列表）
    /// 土地归属：打下的土地不直接归参战者——结束特殊谈判→所选受益人
    /// （受益人=继承法判定继承线外者——继承与受益制度性分离）
    /// </summary>
    [System.Serializable]
    public class GreatHolyWarState
    {
        public int warId;
        public int faithId;          // 发起的教统
        public int callerRealmId;    // 号召者（教宗国/哈里发政权）
        public int targetRealmId;    // 目标政权
        public int targetTile = -1;  // 目标地块（圣地/异教领地——-1=无特定）
        public int startDay;
        public bool ended;
        public bool holySideWon;     // 圣战方胜
        public int beneficiaryId = -1; // 受益人（谈判后选定——继承法线外者）
        public List<int> participants = new List<int>(); // 参战政权（号召加入）
        public List<int> contributors = new List<int>(); // 有贡献的政权（分战利品资格）
        /// <summary>关联的 WarState（战争结算——分数制——圣战方胜→受益人谈判）</summary>
        public int linkedWarId = -1;
    }

    /// <summary>大圣战系统（号召制+受益人谈判——CK3 great_holy_wars.info 同构）</summary>
    public static class GreatHolyWarSystem
    {
        private static int _nextWarId = 1;
        public static readonly List<GreatHolyWarState> ActiveWars = new List<GreatHolyWarState>();

        /// <summary>
        /// 发起大圣战（条件：热忱≥60 + 存在宗教领袖[教宗/哈里发] +
        /// 目标合理[异教政权/圣地被占]）——号召者=教统领袖所在政权
        /// </summary>
        public static GreatHolyWarState Declare(int faithId, int callerRealmId, int targetRealmId,
            int targetTile, int day, bool hasLeader, float fervor)
        {
            if (!hasLeader) return null;
            if (fervor < 60f) return null;
            if (targetRealmId < 0) return null;

            var war = new GreatHolyWarState
            {
                warId = _nextWarId++,
                faithId = faithId,
                callerRealmId = callerRealmId,
                targetRealmId = targetRealmId,
                targetTile = targetTile,
                startDay = day,
                linkedWarId = -1,
            };
            war.participants.Add(callerRealmId);
            ActiveWars.Add(war);
            return war;
        }

        /// <summary>绑定战争结算（GHW 发起后由 GameWorld 创建 WarState 并绑定——
        /// 战争正常分数制结算——结束→圣战方胜→Resolve 受益人谈判）</summary>
        public static void BindWar(GreatHolyWarState war, int warId)
        {
            if (war == null) return;
            war.linkedWarId = warId;
        }

        /// <summary>战争结算钩子（GameWorld.UpdateWarOutcomes 后调用：
        /// 关联战争结束→圣战方胜→Resolve；防御方胜→结束无受益人）</summary>
        public static void OnLinkedWarEnded(GreatHolyWarState war, bool attackerWon,
            CharacterManager characters, RealmData callerRealm, int divisibleEstates, InheritanceLaw law)
        {
            if (war == null || war.ended) return;
            war.holySideWon = attackerWon; // 圣战方=攻击方（号召者发起）
            Resolve(war, characters, callerRealm, divisibleEstates, law);
        }

        /// <summary>号召（该教统/宗教的其他政权响应参战——强制加入[信仰义务]）</summary>
        public static void Rally(GreatHolyWarState war, IEnumerable<RealmData> realms)
        {
            if (war == null || war.ended) return;
            foreach (var r in realms)
            {
                if (r == null || r.realmId == war.callerRealmId) continue;
                if (r.realmId == war.targetRealmId) continue;
                if (r.stateReligionId == war.faithId && !war.participants.Contains(r.realmId))
                    war.participants.Add(r.realmId);
            }
        }

        /// <summary>
        /// 结算（圣战方胜利时）——受益人谈判：
        /// 候选人=参战政权中继承法线外者（IsInInheritanceLine=false——
        /// 次子/幼子/女儿可受益）+ 教会——继承与受益制度性分离
        /// 选中受益人 → 目标地块归受益人（新政权/附庸——简化为地块归属）
        /// </summary>
        public static int Resolve(GreatHolyWarState war, CharacterManager characters,
            RealmData callerRealm, int divisibleEstates, InheritanceLaw law)
        {
            if (war == null || war.ended) return -1;
            war.ended = true;

            // 防御方胜利 → 无受益人
            if (!war.holySideWon) return -1;

            // 受益人候选=号召者政权中继承线外者（历史：十字军把地给次子/教会）
            if (characters == null || callerRealm == null) return -1;
            var candidates = SuccessionSystem.GetGreatHolyWarBeneficiaryCandidates(
                callerRealm, characters, law ?? InheritanceLaw.Primogeniture(), divisibleEstates);
            if (candidates.Count == 0) return -1;

            war.beneficiaryId = candidates[0]; // 简化：第一位（谈判选择——后续可扩展权重）
            return war.beneficiaryId;
        }

        /// <summary>清理已结束的战争</summary>
        public static void Cleanup()
        {
            ActiveWars.RemoveAll(w => w.ended);
        }
    }
}
