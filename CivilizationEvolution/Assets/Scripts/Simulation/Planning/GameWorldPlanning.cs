using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Innovation;

namespace CivilizationEvolution.Simulation.WorldState
{
    /// <summary>GameWorld 与统一计划系统的接入层。</summary>
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

        public void TickPlans(float deltaDays = 1f)
        {
            Plans.DailyTick(currentDay, deltaDays);
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
