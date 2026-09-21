using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Innovation;
using UnityEngine;

namespace CivilizationEvolution.Simulation.WorldState
{
    /// <summary>
    /// GameWorld 与统一计划系统的接入层。
    /// 计划系统不拥有自己的世界时间；只在 GameWorld 的 currentDay 发生推进后运行一次。
    /// </summary>
    public partial class GameWorld
    {
        private Planning.PlanSystem _planSystem;
        private ResearchPlanSystem _researchPlanSystem;
        private Planning.ConstructionPlanSystem _constructionPlanSystem;
        private readonly System.Collections.Generic.List<int> _practiceDirtyCharacters
            = new System.Collections.Generic.List<int>();

        public Planning.PlanSystem Plans => _planSystem ??= CreatePlanSystem();
        public ResearchPlanSystem ResearchPlans => _researchPlanSystem ??= CreateResearchPlanSystem();
        public Planning.ConstructionPlanSystem ConstructionPlans
            => _constructionPlanSystem ??= CreateConstructionPlanSystem();

        public void InitializePlanSystem()
        {
            _researchPlanSystem?.Dispose();
            _planSystem = new Planning.PlanSystem();
            _researchPlanSystem = CreateResearchPlanSystem();
            _constructionPlanSystem = null;
        }

        /// <summary>
        /// 记录一次真实实践。
        /// 生产、建造、采掘、工艺、军事或其他领域系统只需要提交“角色 + 革新 + 实践量”，
        /// 不需要直接依赖个人知识数据结构。
        /// </summary>
        public float RecordInnovationPractice(int characterId, int innovationId, float amount)
        {
            if (amount <= 0f) return 0f;
            if (_researchPlanSystem == null && _innovationTree != null && _characterManager != null)
                _researchPlanSystem = CreateResearchPlanSystem();

            SimulationEvents.Publish(
                new CivilizationEvolution.Core.Events.PracticeRecordedEvent(
                    characterId, innovationId, amount));

            return amount;
        }

        /// <summary>统一计划推进入口：先推进计划，再处理本轮真实实践产生的个人突破。</summary>
        public void TickPlans(float deltaDays = 1f)
        {
            Plans.DailyTick(currentDay, deltaDays);

            if (_researchPlanSystem == null || _characterManager == null) return;

            _practiceDirtyCharacters.Clear();
            _researchPlanSystem.DrainPracticeDirtyCharacters(_practiceDirtyCharacters);
            for (int i = 0; i < _practiceDirtyCharacters.Count; i++)
            {
                int characterId = _practiceDirtyCharacters[i];
                var character = _characterManager.GetCharacter(characterId);
                if (character == null || !character.isAlive || character.realmId < 0) continue;
                _researchPlanSystem.TryDailyBreakthrough(characterId, deltaDays);
            }
        }

        private ResearchPlanSystem CreateResearchPlanSystem()
            => new ResearchPlanSystem(this, Plans);

        private Planning.ConstructionPlanSystem CreateConstructionPlanSystem()
            => new Planning.ConstructionPlanSystem(this, Plans, _buildingSystem);

        private Planning.PlanSystem CreatePlanSystem()
            => new Planning.PlanSystem();

        /// <summary>
        /// 统一建造入口：工程不再直接绕过 PlanSystem。
        /// </summary>
        public Planning.Plan CreateConstructionPlan(
            int realmId,
            int initiatorId,
            int tileIndex,
            int buildingId,
            int builderCharacterId = -1,
            int innovationId = -1)
        {
            if (_buildingSystem == null || !realms.TryGetValue(realmId, out var realm))
                return null;

            return ConstructionPlans.CreateConstructionPlan(
                realmId, initiatorId, tileIndex, buildingId, realm,
                builderCharacterId, innovationId);
        }

        public CharacterManager Characters => _characterManager;
        public InnovationTree Innovations => _innovationTree;
    }
}
