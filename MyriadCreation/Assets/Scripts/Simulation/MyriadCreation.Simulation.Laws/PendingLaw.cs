using System;

namespace MyriadCreation.Simulation.Laws
{
/// <summary>
/// 正在推行的法律：登记后旧法持续有效，到期（TickPendingLaws）才替换现行法律。
/// 同一 group 同时只允许存在一个 PendingLaw；可被发起人或首脑取消。
/// </summary>
[Serializable]
public class PendingLaw
{
	public string lawId = "";

	public string groupId = "";

	/// <summary>开始推行日（宿主当前日）。</summary>
	public int startDay;

	/// <summary>推行所需天数（从 startDay 起算）。</summary>
	public int daysToEnact = 30;

	/// <summary>发起人角色 id（-1 = 无发起人/系统）。</summary>
	public int proposerCharacterId = -1;

	/// <summary>推行状态（当前只有 "enacting"，保留字段供 UI/事件扩展）。</summary>
	public string status = "enacting";

	public int RemainingDays(int currentDay)
	{
		return Math.Max(0, startDay + Math.Max(1, daysToEnact) - currentDay);
	}
}
}
