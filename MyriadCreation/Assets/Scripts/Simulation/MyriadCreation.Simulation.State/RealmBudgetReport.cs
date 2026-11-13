using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.State
{
/// <summary>预算报告的一条线：收入为正、支出为负，reason 是机制标签（税/建设/军费/外交/俸禄）。</summary>
[Serializable]
public class RealmBudgetLine
{
	public string reason = "";

	public float amount;

	public int count;
}

/// <summary>P1.2 只读收支报告：来自实际发生的国库流水，不预造税率/费率。</summary>
[Serializable]
public class RealmBudgetReport
{
	public int realmId;

	public int windowDays;

	public float treasury;

	public float income;

	public float expense;

	public float net;

	public List<RealmBudgetLine> lines = new List<RealmBudgetLine>();
}

/// <summary>一条国库流水（运行时，不落盘；窗口报告用）。</summary>
public readonly struct TreasuryTransaction
{
	public readonly int absoluteDay;

	public readonly float amount;

	public readonly string reason;

	public TreasuryTransaction(int absoluteDay, float amount, string reason)
	{
		this.absoluteDay = absoluteDay;
		this.amount = amount;
		this.reason = reason ?? "";
	}
}
}
