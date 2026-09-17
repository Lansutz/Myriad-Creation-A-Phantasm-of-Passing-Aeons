using System;
using System.Collections.Generic;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Characters;

namespace CivilizationEvolution.Simulation.Innovation
{
    /// <summary>
    /// 革新知识层：把“社会已有知识”和“个人掌握程度”分开。
    /// 社会知识由 InnovationTree 的政权革新集合表示；本系统只保存个人掌握等级与实践积累。
    /// 掌握等级 1/2/3 属于同一个革新，不创建三个革新节点。
    /// </summary>
    public sealed class InnovationKnowledgeSystem
    {
        public const int MinMastery = 0;
        public const int MasteryLevel1 = 1;
        public const int MasteryLevel2 = 2;
        public const int MasteryLevel3 = 3;

        private readonly Dictionary<int, Dictionary<int, byte>> _masteryByCharacter
            = new Dictionary<int, Dictionary<int, byte>>();
        private readonly Dictionary<string, float> _practiceByCharacterInnovation
            = new Dictionary<string, float>();

        /// <summary>角色对某革新的掌握等级；未记录=0。</summary>
        public int GetMastery(int characterId, int innovationId)
        {
            if (_masteryByCharacter.TryGetValue(characterId, out var map)
                && map.TryGetValue(innovationId, out var level))
                return level;
            return 0;
        }

        /// <summary>设置掌握等级，只允许向上推进，不允许把已经掌握的知识倒退。</summary>
        public bool SetMastery(int characterId, int innovationId, int level)
        {
            if (characterId < 0 || innovationId <= 0) return false;
            level = Math.Max(MinMastery, Math.Min(MasteryLevel3, level));

            if (!_masteryByCharacter.TryGetValue(characterId, out var map))
            {
                map = new Dictionary<int, byte>();
                _masteryByCharacter[characterId] = map;
            }

            int old = map.TryGetValue(innovationId, out var value) ? value : 0;
            if (level <= old) return false;
            map[innovationId] = (byte)level;
            return true;
        }

        /// <summary>记录角色针对某革新的实践。实践是个人突破概率的重要输入，而不是抽象研究点。</summary>
        public float RecordPractice(int characterId, int innovationId, float amount)
        {
            if (characterId < 0 || innovationId <= 0 || amount <= 0f) return 0f;
            string key = MakeKey(characterId, innovationId);
            _practiceByCharacterInnovation.TryGetValue(key, out var value);
            value += amount;
            _practiceByCharacterInnovation[key] = value;
            return value;
        }

        public float GetPractice(int characterId, int innovationId)
        {
            return _practiceByCharacterInnovation.TryGetValue(MakeKey(characterId, innovationId), out var value)
                ? value : 0f;
        }

        /// <summary>
        /// 学习某个社会已经存在的革新。
        /// L1 可使用；L2 才算形成足以继续学习后续革新的稳定掌握；L3 代表工艺成熟度。
        /// </summary>
        public bool Learn(int characterId, int innovationId, int targetLevel = MasteryLevel1)
        {
            return SetMastery(characterId, innovationId, targetLevel);
        }

        /// <summary>是否具备继续学习后续革新的个人知识基础。</summary>
        public bool HasFoundation(int characterId, int innovationId)
        {
            return GetMastery(characterId, innovationId) >= MasteryLevel2;
        }

        /// <summary>
        /// 返回角色当前知识边界内可作为突破候选的革新。
        /// 候选必须属于社会可接触知识邻域：无前置革新，或至少一个直接前置已被角色掌握到 L2。
        /// </summary>
        public List<InnovationDef> GetDiscoveryCandidates(
            CharacterData character,
            InnovationTree innovations,
            int realmId)
        {
            var result = new List<InnovationDef>();
            if (character == null || innovations == null) return result;

            foreach (var def in innovations.GetAllInnovations().Values)
            {
                if (innovations.HasInnovation(realmId, def.innovationId)) continue;
                if (!IsKnowledgeNeighbor(character.characterId, def, innovations, realmId)) continue;
                result.Add(def);
            }
            return result;
        }

        private bool IsKnowledgeNeighbor(int characterId, InnovationDef def, InnovationTree innovations, int realmId)
        {
            bool hasAnyFoundation = false;

            if (def.prerequisites == null || def.prerequisites.Count == 0)
                hasAnyFoundation = true;
            else
            {
                foreach (int prereq in def.prerequisites)
                {
                    if (!innovations.HasInnovation(realmId, prereq)) return false;
                    if (GetMastery(characterId, prereq) >= MasteryLevel2)
                        hasAnyFoundation = true;
                }
            }

            if (!hasAnyFoundation && def.prerequisitesAny != null)
            {
                foreach (int prereq in def.prerequisitesAny)
                {
                    if (innovations.HasInnovation(realmId, prereq)
                        && GetMastery(characterId, prereq) >= MasteryLevel2)
                    {
                        hasAnyFoundation = true;
                        break;
                    }
                }
            }

            return hasAnyFoundation;
        }

        private static string MakeKey(int characterId, int innovationId)
            => characterId + "_" + innovationId;
    }
}
