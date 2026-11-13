namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// B3-Step3b：世界组合根（精确映射 GameWorld host hooks，1:1 委托）。
    ///   InitializeSubsystems        → GameWorld.InitializeSubsystemsForHost()
    ///   EnsureAIControllers         → GameWorld.EnsureAIControllersForHost()
    ///   RegisterSimulationSchedules → GameWorld.RegisterSimulationSchedulesForHost()
    ///   InitializeFaithSystems      → GameWorld.InitializeFaithSystemsForHost()
    ///   PrepareLoadedWorld          → GameWorld.PrepareLoadedWorldForHost()
    ///   ApplySetupOptions           → GameWorld.ApplySetupOptions()（公开方法）
    /// 空世界（null / 尚未 InitializeWorld、tiles 未分配）所有入口安全返回（读档损坏档语义：跳过重建，
    /// 由 SaveSystem 的 try/catch 负责回滚；不在此抛异常）。
    /// RegisterSimulationSchedules 内部会 Clear 后重注册，重复调用为等价重建。
    /// </summary>
    public sealed class WorldCompositionRoot
    {
        private readonly GameWorld _world;

        public WorldCompositionRoot(GameWorld world)
        {
            _world = world;
        }

        public GameWorld World => _world;

        private bool HasWorld => _world != null && _world.tiles != null;

        /// <summary>1:1 委托 GameWorld.InitializeSubsystemsForHost()。</summary>
        public void InitializeSubsystems()
        {
            if (!HasWorld) return;
            _world.InitializeSubsystemsForHost();
        }

        /// <summary>1:1 委托 GameWorld.EnsureAIControllersForHost()。</summary>
        public void EnsureAIControllers()
        {
            if (!HasWorld) return;
            _world.EnsureAIControllersForHost();
        }

        /// <summary>1:1 委托 GameWorld.RegisterSimulationSchedulesForHost()。</summary>
        public void RegisterSimulationSchedules()
        {
            if (!HasWorld) return;
            _world.RegisterSimulationSchedulesForHost();
        }

        /// <summary>1:1 委托 GameWorld.InitializeFaithSystemsForHost()。</summary>
        public void InitializeFaithSystems()
        {
            if (!HasWorld) return;
            _world.InitializeFaithSystemsForHost();
        }

        /// <summary>1:1 委托 GameWorld.PrepareLoadedWorldForHost()（读档运行时补齐）。</summary>
        public void PrepareLoadedWorld()
        {
            if (!HasWorld) return;
            _world.PrepareLoadedWorldForHost();
        }

        /// <summary>委托 GameWorld.ApplySetupOptions()。</summary>
        public void ApplySetupOptions()
        {
            if (_world == null) return;
            _world.ApplySetupOptions();
        }
    }
}
