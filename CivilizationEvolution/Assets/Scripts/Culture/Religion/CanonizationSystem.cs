using System.Collections.Generic;
using CivilizationEvolution.Role;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 圣人定义（崇拜对象·历史出身——一神教崇拜对象的核心形态：
    /// 与多神教神祇[神话出身]并列——圣人=角色升格[封圣事件]）
    /// </summary>
    [System.Serializable]
    public class SaintDef
    {
        public int saintId;
        /// <summary>原型角色（已故美德角色——linkedCharacterId）</summary>
        public int linkedCharacterId = -1;
        public string saintName;
        /// <summary>庇护领域（战争/航海/病患/丰收——主保选择依据）</summary>
        public string domain = "";
        /// <summary>所属教统</summary>
        public int faithId;
        /// <summary>封圣时灵性满足（角色生前虔诚）</summary>
        public float canonizationPiety = 80f;
    }

    /// <summary>
    /// 封圣机制（圣人=角色升格——美德/罪行系统对接）：
    /// 已故角色（spiritualFulfillment≥80 + 美德特质≥2）→ 封圣候选
    /// → 教会批准（有宗教领袖[教宗/牧首]时）→ 成为圣人
    /// → 可被选为主保圣人（个人信条/政权主保——崇拜对象池）
    /// </summary>
    public static class CanonizationSystem
    {
        private static int _nextSaintId = 1;
        private static readonly Dictionary<int, List<SaintDef>> _saintsByFaith = new Dictionary<int, List<SaintDef>>();

        /// <summary>封圣候选判定（死亡+灵性满足≥80+美德≥2——历史：圣徒生前极度虔诚）</summary>
        public static bool IsCanonizationCandidate(FaithSystem faith, CharacterData c)
        {
            if (faith == null || c == null) return false;
            if (c.isAlive) return false; // 死后才可封圣
            if (c.spiritualFulfillment < 80f) return false;
            return faith.GetVirtueScore(c) >= 2;
        }

        /// <summary>
        /// 执行封圣（候选+教会批准[有领袖或教统权威]→成为圣人——加入崇拜对象池）
        /// 返回圣人（失败 null）
        /// </summary>
        public static SaintDef Canonize(FaithSystem faith, CharacterData c, string domain)
        {
            if (!IsCanonizationCandidate(faith, c)) return null;
            // 教会批准：教统有宗教领袖（教宗/牧首/大祭司）或默认权威
            if (faith.highPriestCharacterId < 0) return null;

            if (!_saintsByFaith.TryGetValue(faith.faithId, out var saints))
                _saintsByFaith[faith.faithId] = saints = new List<SaintDef>();

            // 防重复封圣
            foreach (var s in saints)
                if (s.linkedCharacterId == c.characterId) return s;

            var saint = new SaintDef
            {
                saintId = _nextSaintId++,
                linkedCharacterId = c.characterId,
                saintName = c.firstName + " " + c.lastName,
                domain = string.IsNullOrEmpty(domain) ? "通用" : domain,
                faithId = faith.faithId,
                canonizationPiety = c.spiritualFulfillment
            };
            saints.Add(saint);
            return saint;
        }

        /// <summary>某教统的圣人列表（主保候选池）</summary>
        public static List<SaintDef> GetSaints(int faithId)
            => _saintsByFaith.TryGetValue(faithId, out var saints) ? saints : new List<SaintDef>();

        /// <summary>重置（测试用）</summary>
        public static void Reset()
        {
            _saintsByFaith.Clear();
        }
    }
}
