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
        public Planning.PlanSystem Plans => _planSystem ??= CreatePlanSystem();
        public ResearchPlanSystem ResearchPlans => _researchPlanSystem ??= CreateResearchPlanSystem();

        public void InitializePlanSystem()
        {
            _planSystem = new Planning.PlanSystem();
            _researchPlanSystem = null;
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
            return _researchPlanSystem?.RecordPractice(characterId, innovationId, amount) ?? 0f;
        }

        /// <summary>
        /// 手动推进统一计划系统。
        /// 研究突破判定也在这里执行：PlanSystem 管生命周期，ResearchPlanSystem 管革新领域规则。
        /// </summary>
        public void TickPlans(float deltaDays = 1f)
        {
            Plans.DailyTick(currentDay, deltaDays);

            if (_researchPlanSystem == null || _characterManager == null) return;

            // 目前先按有名角色进行判定。后续接入“实践事件脏集”后，可只检查当天实际发生相关实践的角色。
            foreach (var character in _characterManager.GetAllCharacters().Values)
            {
                if (character == null || !character.isAlive || character.realmId < 0) continue;
                _researchPlanSystem.TryDailyBreakthrough(character.characterId, deltaDays);
            }
        }

        private ResearchPlanSystem CreateResearchPlanSystem()
        {
            return new ResearchPlanSystem(this, Plans);
        }

        private Planning.PlanSystem CreatePlanSystem()
        {
            return new Planning.PlanSystem();
        }

        public CharacterManager Characters => _characterManager;
        public InnovationTree Innovations => _innovationTree;
    }
}
