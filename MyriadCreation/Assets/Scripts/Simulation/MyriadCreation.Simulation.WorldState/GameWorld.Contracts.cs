using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.CultureHost;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.LawHost;
using MyriadCreation.Simulation.FaithHost;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// 系统契约 facade（B4/B5/B6-part1）：GameWorld 暴露 Faith/Culture/Laws 的 Core.Contracts 接口入口，
/// 消费者可改为依赖接口而不是具体静态/实例 API；旧 API 全部保留，内部适配器转调现有实现。
/// 不拆 asmdef（后续批）：Core 接口不引用 Simulation 类型，具备拆分前置条件。
/// </summary>
public partial class GameWorld
{
	private LawContracts _lawContracts;

	private FaithContracts _faithContracts;

	private CultureContracts _cultureContracts;

	/// <summary>法律查询契约（已固化/生效中/推行中/候选）。</summary>
	public ILawQuery LawQuery => _lawContracts ??= new LawContracts(this);

	/// <summary>法律命令契约（推行/取消/废除/直改）。</summary>
	public ILawCommand LawCommand => _lawContracts ??= new LawContracts(this);

	/// <summary>信仰查询契约（国教/名称/虔诚/圣地）。</summary>
	public IFaithQuery FaithQuery => _faithContracts ??= new FaithContracts(this);

	/// <summary>信仰命令契约（设国教/加虔诚）。</summary>
	public IFaithCommand FaithCommand => _faithContracts ??= new FaithContracts(this);

	/// <summary>文化查询契约（主体文化/名称/阶段）。</summary>
	public ICultureQuery CultureQuery => _cultureContracts ??= new CultureContracts(this);

	/// <summary>文化命令契约（设主体文化/阶段）。</summary>
	public ICultureCommand CultureCommand => _cultureContracts ??= new CultureContracts(this);

	public ILawQuery GetLawContracts() => LawQuery;

	public ILawCommand GetLawCommands() => LawCommand;

	public IFaithQuery GetFaithContracts() => FaithQuery;

	public IFaithCommand GetFaithCommands() => FaithCommand;

	public ICultureQuery GetCultureContracts() => CultureQuery;

	public ICultureCommand GetCultureCommands() => CultureCommand;
}
}
