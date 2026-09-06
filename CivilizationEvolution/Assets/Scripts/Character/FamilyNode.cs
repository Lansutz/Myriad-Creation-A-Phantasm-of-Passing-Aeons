using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Role
{
    public class FamilyNode
    {
        public int familyId;
        public string familyName;
        public int founderCharacterId;
        public int foundingYear;

        // 家族核心成员
        public List<int> memberIds = new List<int>();

        // 递归子家族（分支）
        public List<FamilyNode> branches = new List<FamilyNode>();

        // 父家族（null表示主家族）
        [NonSerialized] public FamilyNode parentFamily;

        // 家族属性
        public float familyPrestige = 0f;
        public float familyWealth = 0f;
        [NonSerialized] public Dictionary<string, float> familyTraditions = new Dictionary<string, float>();


        /// <summary>家族所属政权（-1=未知；家族传统解锁前置革新按此政权检查）</summary>
        public int holderRealmId = -1;

        /// <summary>家族故国（homeland——发源地政权；借鉴《地图上发生的事》homeland_country）</summary>
        public int homelandCountryId = -1;

        /// <summary>代数标记（generation_marks——每代的命名/标记序列，本地化键）</summary>
        public List<string> generationMarks = new List<string>();

        /// <summary>革新树引用（CreateFamily 时由管理器注入；家族传统解锁前置检查用，不入档）</summary>
        [NonSerialized] public InnovationTree Innovations;

        // 家徽/纹章
        public string coaPattern;
        public string coaColors;

        /// <summary>添加成员</summary>
        public void AddMember(int characterId)
        {
            if (!memberIds.Contains(characterId))
                memberIds.Add(characterId);
        }

        /// <summary>创建分支家族</summary>
        public FamilyNode CreateBranch(string branchName, int founderId, int year)
        {
            var branch = new FamilyNode
            {
                familyId = UnityEngine.Random.Range(10000, 99999),
                familyName = branchName,
                founderCharacterId = founderId,
                foundingYear = year,
                parentFamily = this
            };
            branches.Add(branch);
            return branch;
        }

        /// <summary>获取全家族成员（包括所有分支）</summary>
        public List<int> GetAllMembers()
        {
            var all = new List<int>(memberIds);
            foreach (var branch in branches)
                all.AddRange(branch.GetAllMembers());
            return all;
        }

        /// <summary>获取家族总人数</summary>
        public int GetTotalMemberCount()
        {
            int count = memberIds.Count;
            foreach (var branch in branches)
                count += branch.GetTotalMemberCount();
            return count;
        }

        /// <summary>获取家族代数（深度）</summary>
        public int GetGenerationDepth()
        {
            if (branches.Count == 0) return 1;
            int maxDepth = 0;
            foreach (var branch in branches)
                maxDepth = Mathf.Max(maxDepth, branch.GetGenerationDepth());
            return maxDepth + 1;
        }

        /// <summary>查找角色所在的家族节点</summary>
        public FamilyNode FindCharacterFamily(int characterId)
        {
            if (memberIds.Contains(characterId)) return this;
            foreach (var branch in branches)
            {
                var found = branch.FindCharacterFamily(characterId);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>计算家族总威望</summary>
        public float CalculateTotalPrestige()
        {
            float total = familyPrestige;
            foreach (var branch in branches)
                total += branch.CalculateTotalPrestige() * 0.5f; // 分支威望减半计入主家族
            return total;
        }

        // ===== 家族传统（企划书 9.4 家族文化偏移；定义表解释键，见 FamilyTraditionDef） =====

        /// <summary>
        /// 添加家族传统：
        /// 注册表未定义 → 拒绝并警告；与已传承传统互斥（incompatibleWith）→ 拒绝；
        /// 解锁前置革新未全部持有（requiredInnovations）→ 拒绝（革新树未注入时跳过检查）；
        /// 传承强度起点 1（代际深度由家族系统后续累积）
        /// </summary>
        public bool AddFamilyTradition(string traditionId)
        {
            if (string.IsNullOrEmpty(traditionId) || familyTraditions.ContainsKey(traditionId)) return false;
            if (!ContentRegistry.TryGetFamilyTradition(traditionId, out var def))
            {
                Debug.LogWarning($"[Family] 家族传统 {traditionId} 未在注册表定义，拒绝添加");
                return false;
            }
            if (def.incompatibleWith != null)
            {
                foreach (var existing in familyTraditions.Keys)
                {
                    if (def.incompatibleWith.Contains(existing))
                    {
                        Debug.Log($"[Family] 家族传统 {traditionId} 与既有传统 {existing} 互斥，拒绝添加");
                        return false;
                    }
                }
            }
            // 解锁前置革新检查（革新树注入且家族归属政权已知时生效；否则宽松跳过）
            if (Innovations != null && holderRealmId >= 0
                && def.requiredInnovations != null && def.requiredInnovations.Count > 0)
            {
                foreach (int reqId in def.requiredInnovations)
                {
                    if (!Innovations.HasInnovation(holderRealmId, reqId))
                    {
                        Debug.Log($"[Family] 家族传统 {traditionId} 需要革新 {reqId} 解锁，家族所在政权尚未持有，拒绝添加");
                        return false;
                    }
                }
            }
            familyTraditions[traditionId] = 1f;
            return true;
        }

        /// <summary>移除家族传统</summary>
        public bool RemoveFamilyTradition(string traditionId) => familyTraditions.Remove(traditionId);

        /// <summary>计算家族传统在指定键上的总效果（键由 FamilyTraditionDef.effects 解释，如 unity/prestige/learning）</summary>
        public float GetTraditionEffect(string key)
        {
            float total = 0f;
            foreach (var kv in familyTraditions)
            {
                if (!ContentRegistry.TryGetFamilyTradition(kv.Key, out var def) || def.effects == null) continue;
                foreach (var e in def.effects)
                {
                    if (e.key == key) total += e.value * kv.Value;
                }
            }
            return total;
        }
    }
}
