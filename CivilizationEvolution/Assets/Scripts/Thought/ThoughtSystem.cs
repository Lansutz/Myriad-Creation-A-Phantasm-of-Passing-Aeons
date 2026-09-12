using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Thought
{
 /// 学派系统
 /// 前现代思想学派，有核心经典、代表人物、传播机制

 /// 信仰系统
 /// 宗教信仰，有神灵体系、仪式、教义、组织

 /// <summary>神灵</summary>

 /// <summary>教义</summary>

 /// 法律与罪行系统（简化版）

 /// 思潮系统（高阶解锁机制）
 /// 大规模思想运动，有起源、传播、高潮、衰退周期
    [System.Serializable]


 /// 思想与规范管理器
 /// 协调学派、信仰、法律、思潮系统
    public class ThoughtManager
    {
        private readonly Dictionary<int, SchoolOfThought> _schools = new Dictionary<int, SchoolOfThought>();
        private readonly Dictionary<int, FaithSystem> _faiths = new Dictionary<int, FaithSystem>();
        private readonly Dictionary<int, LawSystem> _lawSystems = new Dictionary<int, LawSystem>();
        private readonly List<IdeologyMovement> _movements = new List<IdeologyMovement>();
        private int _nextSchoolId = 1;
        private int _nextFaithId = 1;
        private int _nextLawId = 1;
        private int _nextMovementId = 1;

 /// <summary>创建学派</summary>
        public SchoolOfThought CreateSchool(string name, int founderId, int year)
        {
            var school = new SchoolOfThought
            {
                schoolId = _nextSchoolId++,
                schoolName = name,
                founderCharacterId = founderId,
                foundingYear = year
            };
            _schools[school.schoolId] = school;
            return school;
        }

 /// <summary>创建信仰</summary>
        public FaithSystem CreateFaith(string name, FaithType type)
        {
            var faith = new FaithSystem
            {
                faithId = _nextFaithId++,
                faithName = name,
                type = type
            };
            _faiths[faith.faithId] = faith;
            return faith;
        }

 /// <summary>创建法律体系</summary>
        public LawSystem CreateLawSystem(string name, LawSource source)
        {
            var law = new LawSystem
            {
                lawSystemId = _nextLawId++,
                lawSystemName = name,
                source = source
            };
            _lawSystems[law.lawSystemId] = law;
            return law;
        }

 /// <summary>创建思潮运动</summary>
        public IdeologyMovement CreateMovement(string name, int originRegionId, int startYear)
        {
            var movement = new IdeologyMovement
            {
                movementId = _nextMovementId++,
                movementName = name,
                originRegionId = originRegionId,
                startYear = startYear
            };
            _movements.Add(movement);
            return movement;
        }

 /// <summary>每日思想Tick</summary>
        public void DailyTick(int currentYear)
        {
            foreach (var school in _schools.Values)
                school.DailyTick();

            foreach (var faith in _faiths.Values)
                faith.DailyTick();

            foreach (var law in _lawSystems.Values)
                law.DailyTick();

            for (int i = _movements.Count - 1; i >= 0; i--)
            {
                _movements[i].DailyTick(currentYear);
                if (_movements[i].phase == MovementPhase.Extinct)
                    _movements.RemoveAt(i);
            }
        }

 // ===== 查询接口 =====
        public SchoolOfThought GetSchool(int id) => _schools.TryGetValue(id, out var s) ? s : null;
        public FaithSystem GetFaith(int id) => _faiths.TryGetValue(id, out var f) ? f : null;
        public LawSystem GetLawSystem(int id) => _lawSystems.TryGetValue(id, out var l) ? l : null;
        public IdeologyMovement GetMovement(int id) => _movements.Find(m => m.movementId == id);

        public IReadOnlyDictionary<int, SchoolOfThought> GetAllSchools() => _schools;
        public IReadOnlyDictionary<int, FaithSystem> GetAllFaiths() => _faiths;
        public IReadOnlyList<IdeologyMovement> GetAllMovements() => _movements;

 /// <summary>获取地区最主流信仰</summary>
        public FaithSystem GetDominantFaith(int regionId)
        {
            FaithSystem dominant = null;
            float maxAdherence = 0f;
            foreach (var faith in _faiths.Values)
            {
                if (faith.regionAdherence.TryGetValue(regionId, out var adherence) && adherence > maxAdherence)
                {
                    maxAdherence = adherence;
                    dominant = faith;
                }
            }
            return dominant;
        }

 /// <summary>获取地区最有影响力学派</summary>
        public SchoolOfThought GetDominantSchool(int regionId)
        {
            SchoolOfThought dominant = null;
            float maxPenetration = 0f;
            foreach (var school in _schools.Values)
            {
                if (school.regionPenetration.TryGetValue(regionId, out var penetration) && penetration > maxPenetration)
                {
                    maxPenetration = penetration;
                    dominant = school;
                }
            }
            return dominant;
        }
    }
}
