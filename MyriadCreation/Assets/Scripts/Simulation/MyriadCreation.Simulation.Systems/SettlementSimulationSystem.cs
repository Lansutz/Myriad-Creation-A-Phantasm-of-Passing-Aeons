using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.Systems
{
public sealed class SettlementSimulationSystem
{
	private readonly Dictionary<int, SettlementData> _settlements;

	private readonly TileData[] _tiles;

	private readonly int _mapWidth;

	private readonly int _mapHeight;

	private readonly Dictionary<int, Army> _armies;

	private readonly GameWorld _world;

	private readonly MapActorManager _mapActors;

	private readonly CampManager _camps;

	private readonly SettlementEvolutionRuntime _evolutionRuntime;

	private readonly SettlementControlRuntime _controlRuntime;

	private readonly SettlementDestructionRuntime _destructionRuntime;

	private readonly LandAbandonmentRuntime _abandonmentRuntime;

	private readonly SettlementProductionRuntime _productionRuntime;

	public SettlementDestructionRuntime DestructionRuntime => _destructionRuntime;

	public LandAbandonmentRuntime AbandonmentRuntime => _abandonmentRuntime;

	public SettlementSimulationSystem(Dictionary<int, SettlementData> settlements, TileData[] tiles, int mapWidth, int mapHeight, Dictionary<int, Army> armies, GameWorld world, MapActorManager mapActors, CampManager camps)
	{
		_settlements = settlements ?? throw new ArgumentNullException("settlements");
		_tiles = tiles ?? throw new ArgumentNullException("tiles");
		_mapWidth = mapWidth;
		_mapHeight = mapHeight;
		_armies = armies ?? throw new ArgumentNullException("armies");
		_world = world ?? throw new ArgumentNullException("world");
		_mapActors = mapActors ?? throw new ArgumentNullException("mapActors");
		_camps = camps ?? throw new ArgumentNullException("camps");
		_evolutionRuntime = new SettlementEvolutionRuntime(_world);
		_controlRuntime = new SettlementControlRuntime(_settlements, _tiles, _mapWidth, _mapHeight, _armies, _world.unitDefs);
		_destructionRuntime = new SettlementDestructionRuntime(_world);
		_abandonmentRuntime = new LandAbandonmentRuntime(_world);
		_productionRuntime = new SettlementProductionRuntime(_settlements, _tiles, _world);
	}

	public void ControlDailyTick(SimulationTickContext context)
	{
		_controlRuntime.DailyTick();
	}

	public void MapActorsDailyTick(SimulationTickContext context)
	{
		_mapActors.Tick(context.deltaDays);
	}

	public void CampsDailyTick(SimulationTickContext context)
	{
		_camps.Tick(context.deltaDays);
	}

	public void EvolutionDailyTick(SimulationTickContext context)
	{
		_evolutionRuntime.DailyTick();
	}

	public void RecoveryDailyTick(SimulationTickContext context)
	{
		_destructionRuntime.DailyTickRecovery();
	}

	public void AbandonmentDailyTick(SimulationTickContext context)
	{
		_abandonmentRuntime.DailyCheckBanditSpawn();
	}

	public void ProductionDailyTick(SimulationTickContext context)
	{
		_productionRuntime.DailyTick();
	}
}
}
