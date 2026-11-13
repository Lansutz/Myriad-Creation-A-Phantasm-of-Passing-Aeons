using System;

namespace MyriadCreation.Core.Contracts
{
/// <summary>
/// Simulation 与 Host 适配层之间的反转点（P2.5 F）：Simulation 只引用本契约，
/// Host 程序集在模块初始化时注册工厂，不形成 asmdef 循环。
/// </summary>
public static class SimulationHostRegistry
{
	public static Func<object, IFaithQuery> FaithQueryFactory { get; set; }

	public static Func<object, IFaithCommand> FaithCommandFactory { get; set; }

	public static Func<object, ICultureQuery> CultureQueryFactory { get; set; }

	public static Func<object, ICultureCommand> CultureCommandFactory { get; set; }

	public static IFaithQuery CreateFaithQuery(object world)
	{
		return FaithQueryFactory?.Invoke(world);
	}

	public static IFaithCommand CreateFaithCommand(object world)
	{
		return FaithCommandFactory?.Invoke(world);
	}

	public static ICultureQuery CreateCultureQuery(object world)
	{
		return CultureQueryFactory?.Invoke(world);
	}

	public static ICultureCommand CreateCultureCommand(object world)
	{
		return CultureCommandFactory?.Invoke(world);
	}
}
}
