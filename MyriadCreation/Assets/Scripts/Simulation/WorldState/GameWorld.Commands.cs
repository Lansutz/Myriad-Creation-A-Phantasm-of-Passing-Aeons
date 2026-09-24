using CivilizationEvolution.Core.Contracts;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.Simulation.Warfare;
using CivilizationEvolution.Simulation.Warfare;

namespace CivilizationEvolution.Simulation.WorldState
{
    /// <summary>
    /// 世界级 Command Handler 注册边界。
    /// GameWorld 负责组合依赖，领域 Handler 负责实际写入。
    /// </summary>
    public partial class GameWorld
    {
        private void RegisterDiplomacyCommandHandlers()
        {
            _simulationCommands.Register(
                new DeclareWarCommandHandler(DeclareWar));

            _simulationCommands.Register(
                new RaidSettlementCommandHandler(
                    _diplomacyManager,
                    tiles,
                    _characterManager));

            _simulationCommands.Register(
                new ProposeAllianceCommandHandler(_diplomacyManager));

            _simulationCommands.Register(
                new SendGiftCommandHandler(_diplomacyManager));
            _simulationCommands.Register(
                new StartInnovationResearchCommandHandler(_innovationTree));

            _simulationCommands.Register(
                new ImproveRealmEconomyCommandHandler(realms, tiles));

            _simulationCommands.Register(
                new ConsolidateRealmCommandHandler(realms, tiles));

            _simulationCommands.Register(
                new MilitaryBuildUpCommandHandler(realms));
        }

        private void RegisterSettlementCommandHandlers()
        {
            var destruction = new SettlementDestructionRuntime(this);
            var abandonment = new LandAbandonmentRuntime(this);

            _simulationCommands.Register(
                new DestroySettlementCommandHandler(
                    destruction,
                    _simulationEvents));

            _simulationCommands.Register(
                new AbandonSettlementTileCommandHandler(
                    abandonment,
                    _simulationEvents));

            _simulationCommands.Register(
                new ResettleTileCommandHandler(
                    abandonment,
                    _simulationEvents));
        }
    }
}
