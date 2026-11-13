using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.CultureHost;
using MyriadCreation.Simulation.FaithHost;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;

namespace MyriadCreation.Simulation.Hosts
{
/// <summary>P2.5 F：向 Simulation 注册适配器工厂，避免 sim 反向引用 Host 程序集。</summary>
public static class HostRegistration
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void AutoRegister()
	{
		EnsureRegistered();
	}

	public static void EnsureRegistered()
	{
		SimulationHostRegistry.FaithQueryFactory = world => new FaithContracts((GameWorld)world);
		SimulationHostRegistry.FaithCommandFactory = world => new FaithContracts((GameWorld)world);
		SimulationHostRegistry.CultureQueryFactory = world => new CultureContracts((GameWorld)world);
		SimulationHostRegistry.CultureCommandFactory = world => new CultureContracts((GameWorld)world);
	}
}
}
