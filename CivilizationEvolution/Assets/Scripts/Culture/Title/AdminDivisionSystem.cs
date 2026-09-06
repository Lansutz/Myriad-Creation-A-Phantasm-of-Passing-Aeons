using System.Collections.Generic;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 行政区划实体（政权内部治理树节点——2026-09-03 用户设计）：
    /// 政权=第 1 层（根）——层数由治理模式定：
    /// 分封[localSuccession=Hereditary]=固定 4 层（宗法树——诸侯→卿大夫→士）
    /// 郡县[Appointed/Examination]=行政容量弹性 2-5 层
    /// 每节点挂治理头衔（官僚线=行政官/分封线=领主爵位）
    /// </summary>
    public class AdminDivision
    {
        public int divisionId;          // 唯一（realmId*10000+序）
        public int realmId;
        public int level;               // 1=政权根——2..N
        public string name = "";
        public int parentDivisionId = -1; // -1=根
        public List<int> childIds = new List<int>();
        public HashSet<int> tiles = new HashSet<int>(); // 辖境地块
        /// <summary>治理头衔（官僚=titleId[郡守…]/分封=领主爵位——TitleDef 键）</summary>
        public string titleId = "";
        /// <summary>治理者（官僚=官员 charId——分封=领主 charId——-1=空缺）</summary>
        public int holderCharacterId = -1;
    }

    /// <summary>行政区划生成与查询</summary>
    public static class AdminDivisionSystem
    {
        /// <summary>区划层级名（层深→通名——文化定制后续——官僚线华夏/分封线宗法）</summary>
        public static string LevelName(int level, bool feudal)
        {
            if (level <= 1) return "政权";
            if (feudal)
            {
                switch (level)
                {
                    case 2: return "诸侯";
                    case 3: return "卿大夫";
                    case 4: return "士家";
                    default: return "封臣";
                }
            }
            switch (level)
            {
                case 2: return "郡";
                case 3: return "县";
                case 4: return "乡";
                case 5: return "亭";
                default: return "区";
            }
        }

        /// <summary>
        /// 行政深度（政权=1 层）：
        /// 分封（地方世袭领有）→ 固定 4（宗法树）
        /// 郡县（任命/考试）→ 行政容量弹性 2-5（容量 0-1 分档）
        /// 选举/特许自治 → 2（自治浅结构）
        /// </summary>
        public static int GetAdminDepth(GovernmentComposition comp, float administrativeCapacity)
        {
            if (comp == null || comp.localSuccession == null || comp.localSuccession.primary < 0)
                return 1; // 无地方结构（部落直控）
            var mode = (LocalSuccession)comp.localSuccession.primary;
            switch (mode)
            {
                case LocalSuccession.Hereditary:
                    return 4; // 分封宗法固定 4 层
                case LocalSuccession.Appointed:
                case LocalSuccession.Examination:
                    // 郡县：容量弹性——低容量 2（政权+一级）——高容量 5（上限）
                    if (administrativeCapacity < 0.2f) return 2;
                    if (administrativeCapacity < 0.4f) return 3;
                    if (administrativeCapacity < 0.65f) return 4;
                    return 5;
                case LocalSuccession.Elected:
                case LocalSuccession.CityCharter:
                    return 2; // 自治体浅结构
                default:
                    return 1;
            }
        }

        /// <summary>生成行政区划树（按治理模式深度——地块递归划分）</summary>
        public static List<AdminDivision> Generate(RealmData realm, GovernmentComposition comp,
            float administrativeCapacity, HashSet<int> territoryTiles)
        {
            var divisions = new List<AdminDivision>();
            if (realm == null) return divisions;

            int depth = GetAdminDepth(comp, administrativeCapacity);
            bool feudal = comp != null && comp.localSuccession != null
                && comp.localSuccession.primary == (int)LocalSuccession.Hereditary;

            // 根（政权=第 1 层——全领地）
            var root = new AdminDivision
            {
                divisionId = realm.realmId * 10000 + 1,
                realmId = realm.realmId,
                level = 1,
                name = realm.realmName,
                parentDivisionId = -1,
            };
            if (territoryTiles != null) root.tiles = new HashSet<int>(territoryTiles);
            divisions.Add(root);

            // 子层递归划分（层 2..depth）
            BuildLevels(root, 2, depth, feudal, divisions);
            return divisions;
        }

        private static void BuildLevels(AdminDivision parent, int level, int maxDepth,
            bool feudal, List<AdminDivision> divisions)
        {
            if (level > maxDepth || parent.tiles.Count == 0) return;
            int seq = 1;
            // 每层分块（简单均分——子节点 tile 集）
            int split = System.Math.Min(4, parent.tiles.Count); // 每层最多 4 个子区
            if (split <= 0) return;
            var tileList = new List<int>(parent.tiles);
            int perChild = tileList.Count / split;
            if (perChild == 0) { split = tileList.Count; perChild = 1; }

            for (int i = 0; i < split; i++)
            {
                var child = new AdminDivision
                {
                    divisionId = parent.realmId * 10000 + level * 100 + seq,
                    realmId = parent.realmId,
                    level = level,
                    name = $"{realmName(parent.realmId)}·{LevelName(level, feudal)}{seq}",
                    parentDivisionId = parent.divisionId,
                };
                int start = i * perChild;
                int end = (i == split - 1) ? tileList.Count : start + perChild;
                for (int t = start; t < end && t < tileList.Count; t++)
                    child.tiles.Add(tileList[t]);

                // 治理头衔绑定（官僚=层深对官僚头衔——分封=层深对贵族爵位）
                child.titleId = ResolveTitleId(level, feudal);
                parent.childIds.Add(child.divisionId);
                divisions.Add(child);
                BuildLevels(child, level + 1, maxDepth, feudal, divisions); // 递归下层
            }
        }

        private static string realmName(int realmId) => $"R{realmId}"; // 占位（调用方可用真名替换——简化）

        /// <summary>层深→头衔（官僚线按层取官僚头衔 rank——分封线取贵族/君主系）</summary>
        private static string ResolveTitleId(int level, bool feudal)
        {
            // 简化的内置映射（文化/数据驱动细化后续——TitleCatalog 按 kind 可查）
            if (feudal)
            {
                if (level == 2) return "title_zhuhou";      // 诸侯
                if (level == 3) return "title_qingdafu";    // 卿大夫
                return "title_baron";                        // 士家（低级爵）
            }
            if (level == 2) return "title_gov_general";     // 总督/郡守级
            if (level == 3) return "title_prefect";         // 郡守/州牧
            if (level == 4) return "title_magistrate";      // 县令
            return "title_magistrate";                      // 乡亭（基层）
        }

        /// <summary>查询：政权所有区划</summary>
        public static List<AdminDivision> GetDivisions(List<AdminDivision> all, int realmId)
        {
            var result = new List<AdminDivision>();
            if (all == null) return result;
            foreach (var d in all)
                if (d.realmId == realmId) result.Add(d);
            return result;
        }

        /// <summary>查询：某层全部区划</summary>
        public static List<AdminDivision> AtLevel(List<AdminDivision> all, int realmId, int level)
        {
            var result = new List<AdminDivision>();
            foreach (var d in all)
                if (d.realmId == realmId && d.level == level) result.Add(d);
            return result;
        }

        /// <summary>查询：地块所属区划（最深层）</summary>
        public static AdminDivision DivisionOfTile(List<AdminDivision> all, int realmId, int tileIndex)
        {
            AdminDivision deepest = null;
            foreach (var d in all)
            {
                if (d.realmId != realmId || !d.tiles.Contains(tileIndex)) continue;
                if (deepest == null || d.level > deepest.level) deepest = d;
            }
            return deepest;
        }
    }
}
