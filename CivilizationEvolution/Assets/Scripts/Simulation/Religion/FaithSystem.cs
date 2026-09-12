using System.Collections.Generic;
using System;
using UnityEngine;

namespace CivilizationEvolution.Simulation.Religion
{
    [System.Serializable]
    public class FaithSystem
    {
        public int faithId;
        public string faithName;
        public string description;
        public FaithType type = FaithType.Polytheistic;

 // 神灵体系
        public List<Deity> deities = new List<Deity>();
        public string chiefDeity;

 // 教义
        public List<Doctrine> doctrines = new List<Doctrine>();
        public List<string> rituals = new List<string>();
        public List<string> taboos = new List<string>();

 // 组织
        public FaithOrganizationType orgType = FaithOrganizationType.Decentralized;
        public int highPriestCharacterId = -1;
        public float churchWealth = 0f;
        public float churchInfluence = 0f;

 // 圣地
        public List<int> holySiteTileIndices = new List<int>();

 // 传播与信徒
        public float missionaryZeal = 0.5f;
        public float tolerance = 0.5f;
        [NonSerialized] public Dictionary<int, float> regionAdherence = new Dictionary<int, float>();

        public List<int> followerCharacterIds = new List<int>();

 // 宗教间关系
        [NonSerialized] public Dictionary<int, float> faithRelations = new Dictionary<int, float>();


 // ===== 美德/罪行（宗教对性格的判定——引用 PersonalityTraitDatabase 基 id） =====
        public List<string> virtues = new List<string>();
        public List<string> sins = new List<string>();

 // ===== 教阶制（组织支柱机制化——教区→主教区→大主教区→枢机团/牧首会议） ===== /// <summary>教阶等级（0=无教阶[原始崇拜/多神松散] 1=教区 2=主教区
 /// 3=大主教区 4=枢机团/牧首会议——CK3 大主教区+枢机团）</summary>
        public int hierarchyLevel = 0;
 /// <summary>教阶头衔列表（头衔名/级别——如"科隆大主教"——叙任权对接）</summary>
        public List<EcclesiasticalTitle> hierarchyTitles = new List<EcclesiasticalTitle>();

 /// <summary>设立教阶（升级教阶制——组织支柱：教区→大主教区→枢机团）</summary>
        public void SetHierarchyLevel(int level)
        {
            hierarchyLevel = Mathf.Clamp(level, 0, 4);
        }

 /// <summary>任命教阶头衔（俗人任命=叙任权在君[政教冲突]/灵性任命=在教）</summary>
        public void AddHierarchyTitle(string titleName, int level, bool temporalAppointment)
        {
            hierarchyTitles.Add(new EcclesiasticalTitle
            {
                titleName = titleName,
                level = level,
                temporalAppointment = temporalAppointment
            });
            if (level > hierarchyLevel) hierarchyLevel = level;
        }

 // ===== 信仰热忱（Fervor——大圣战可用性数值） ===== /// <summary>热忱 0-100（大圣战可用条件：≥60 + 存在宗教领袖）</summary>
        public float fervor = 50f;
 /// <summary>大圣战可用阈值</summary>
        public const float GreatHolyWarThreshold = 60f;

 /// <summary>热忱变化（增长：异教冲突+25/圣地丢失+50/殉道+25/圣战胜利+15/
 /// 大公会议成功+10；下降：内部丑闻-30/圣战失败-20/长期和平冷却-10）</summary>
        public void AddFervor(float delta)
        {
            fervor = Mathf.Clamp(fervor + delta, 0f, 100f);
        }

 /// <summary>大圣战是否可用（热忱达标+有宗教领袖——教宗/哈里发）</summary>
        public bool CanDeclareGreatHolyWar() => fervor >= GreatHolyWarThreshold && highPriestCharacterId >= 0;

 /// <summary>美德/罪行得分（宗教对性格的判定——traitId 匹配基 id 前缀）</summary>
        public int GetVirtueScore(CivilizationEvolution.Simulation.Characters.CharacterData character)
        {
            if (character == null || character.traits == null) return 0;
            int score = 0;
            foreach (var t in character.traits)
                foreach (var v in virtues)
                    if (t.traitId == v || t.traitId.StartsWith(v + "_"))
                        score++;
            return score;
        }

        public int GetSinScore(CivilizationEvolution.Simulation.Characters.CharacterData character)
        {
            if (character == null || character.traits == null) return 0;
            int score = 0;
            foreach (var t in character.traits)
                foreach (var s in sins)
                    if (t.traitId == s || t.traitId.StartsWith(s + "_"))
                        score++;
            return score;
        }

 /// <summary>计算宗教权威</summary>
        public float CalculateReligiousAuthority()
        {
            float totalAdherence = 0f;
            foreach (var kv in regionAdherence)
                totalAdherence += kv.Value;
            return churchInfluence * 0.4f + totalAdherence * 0.3f + followerCharacterIds.Count * 0.3f;
        }

 /// <summary>每日信仰Tick</summary>
        public void DailyTick()
        {
            churchInfluence = Mathf.Lerp(churchInfluence, 50f, 0.001f);
        }
    }
}
