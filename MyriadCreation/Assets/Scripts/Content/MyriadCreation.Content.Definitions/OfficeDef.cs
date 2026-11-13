using System;

namespace MyriadCreation.Content.Definitions
{
/// <summary>
/// 官职定义（P3.1）：内容表驱动的任职规则与职权。
/// `slot` 是存档槽位（0-5 与旧 <see cref="OfficialOffice"/> 顺序对齐，保证旧档可读）；
/// `modules` 只列出“能做什么”，不给产出百分比。
/// </summary>
[Serializable]
public sealed class OfficeDef
{
	public string id = "";

	public int slot;

	public string name = "";

	/// <summary>本地化键；为空时回退 <see cref="name"/>。</summary>
	public string nameKey = "";

	/// <summary>品级：1 最高。用于议事/任免时的优先序，不直接折算数值。</summary>
	public int rank = 3;

	/// <summary>职权：law_proposal / judiciary / taxation / personnel / military_command / diplomacy。</summary>
	public string[] modules;

	/// <summary>任命方式候选（按顺序取第一个可行）：ruler / council / election。</summary>
	public string[] appointmentOrder;

	/// <summary>候选出身：any / royal / non_royal（royal = 与君主同家族）。</summary>
	public string candidateRoyal = "any";

	/// <summary>候选性别：any / male / female。</summary>
	public string candidateGender = "any";

	/// <summary>任期年数；0 = 终身或君主裁量。</summary>
	public int termYears;

	/// <summary>年薪；由国库按日折算支付。</summary>
	public float salary;

	/// <summary>罢免阻力 0-100：越高越需要议事会支持（后续接罢免动作）。</summary>
	public float dismissalResistance = 10f;

	public bool enabled = true;
}
}
