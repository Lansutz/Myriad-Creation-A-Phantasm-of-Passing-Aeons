using System.Collections;
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
        private Coroutine _planRuntimeCoroutine;
        private int _lastPlanTickDay = -1;

        public Planning.PlanSystem Plans => _planSystem ??= CreatePlanSystem();
        public ResearchPlanSystem ResearchPlans => _researchPlanSystem ??= CreateResearchPlanSystem();

        public void InitializePlanSystem()
        {
            _planSystem = new Planning.PlanSystem();
            _researchPlanSystem = null;
            _lastPlanTickDay = -1;
        }

        /// <summary>手动推进计划系统；正常运行由运行时桥接器按世界日自动调用。</summary>
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

        /// <summary>
        /// 计划系统运行时桥。
        /// 不重复实现 GameWorld.Update/GameTick，避免与主循环产生两个时间源。
        /// </summary>
        private void OnEnable()
        {
            if (_planRuntimeCoroutine == null)
                _planRuntimeCoroutine = StartCoroutine(PlanRuntimeLoop());
        }

        private void OnDisable()
        {
            if (_planRuntimeCoroutine != null)
            {
                StopCoroutine(_planRuntimeCoroutine);
                _planRuntimeCoroutine = null;
            }
        }

        private IEnumerator PlanRuntimeLoop()
        {
            while (true)
            {
                // InitializeWorld 在运行时建立 InnovationTree；在此之前不创建研究系统，避免主菜单/空世界阶段提前绑定旧引用。
                if (_researchPlanSystem == null && _innovationTree != null && _characterManager != null)
                    _researchPlanSystem = CreateResearchPlanSystem();

                if (_researchPlanSystem != null && currentDay != _lastPlanTickDay)
                {
                    float deltaDays = _lastPlanTickDay < 0 ? 1f : Mathf.Max(1f, currentDay - _lastPlanTickDay);
                    _researchPlanSystem.DailyTick(deltaDays);
                    TickPlans(deltaDays);
                    _lastPlanTickDay = currentDay;
                }

                yield return null;
            }
        }

        public CharacterManager Characters => _characterManager;
        public InnovationTree Innovations => _innovationTree;
    }
}
