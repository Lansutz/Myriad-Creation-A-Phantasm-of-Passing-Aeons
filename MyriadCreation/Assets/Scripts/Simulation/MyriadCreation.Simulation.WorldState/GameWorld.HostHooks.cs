namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// B3-Step3b：组合根 1:1 host hooks。
    /// 方法体直接调用 GameWorld 现有 private/公开方法，不复制、不改变逻辑；
    /// 供同程序集内的 WorldCompositionRoot 精确映射（避免再借 PrepareLoadedWorld/ReinitializeSubsystems 的宽语义）。
    /// 不新增序列化字段，不改变场景/生成行为。
    /// </summary>
    public partial class GameWorld
    {
        /// <summary>直调现有 InitializeSubsystems()（仅子系统构造/接线，不含默认内容与读档补齐）。</summary>
        internal void InitializeSubsystemsForHost()
        {
            InitializeSubsystems();
        }

        /// <summary>直调现有 EnsureAIControllers()（按 realms 补齐 AI 控制器，空引用安全）。</summary>
        internal void EnsureAIControllersForHost()
        {
            EnsureAIControllers();
        }

        /// <summary>直调现有 InitializeFaithSystems()（按 ReligionCatalog 重建 FaithSystem 骨架，不连带 UnitDefs）。</summary>
        internal void InitializeFaithSystemsForHost()
        {
            InitializeFaithSystems();
        }

        /// <summary>直调现有 RegisterSimulationSchedules()（先 Clear 再注册，重复调用等价重建）。</summary>
        internal void RegisterSimulationSchedulesForHost()
        {
            RegisterSimulationSchedules();
        }

        /// <summary>直调现有 PrepareLoadedWorld()（读档运行时补齐：UnitDefs + FaithSystems + AI 控制器）。</summary>
        internal void PrepareLoadedWorldForHost()
        {
            PrepareLoadedWorld();
        }
    }
}
