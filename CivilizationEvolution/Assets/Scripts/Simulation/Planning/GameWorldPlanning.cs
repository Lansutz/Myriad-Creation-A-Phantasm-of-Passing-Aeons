using CivilizationEvolution.Simulation.WorldState;

namespace CivilizationEvolution.Simulation.Planning
{
    /// <summary>
    /// GameWorld 与计划系统的接入层。
    /// 先独立存在，避免在计划框架尚未完成时侵入 GameWorld 主循环。
    /// </summary>
    public partial class GameWorld
    {
        private PlanSystem _planSystem;

        /// <summary>统一计划系统。由世界生命周期持有一个实例。</summary>
        public PlanSystem Plans => _planSystem ??= new PlanSystem();

        /// <summary>重新初始化计划系统（新世界/读档重建时使用）。</summary>
        public void InitializePlanSystem()
        {
            _planSystem = new PlanSystem();
        }

        /// <summary>推进世界中的所有计划。</summary>
        public void TickPlans(float deltaDays = 1f)
        {
            _planSystem?.DailyTick(currentDay, deltaDays);
        }
    }
}
