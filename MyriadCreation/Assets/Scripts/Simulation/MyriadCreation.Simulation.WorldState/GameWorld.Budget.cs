using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld
{
	/// <summary>
	/// P1.2 只读收支报告：只汇总实际发生的国库流水（目前主税收入；支出随命令/军费逐步接入）。
	/// 不预造费率，不改任何模拟状态。
	/// </summary>
	public RealmBudgetReport GetRealmBudgetReport(int realmId, int windowDays = MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear)
	{
		RealmBudgetReport report = new RealmBudgetReport
		{
			realmId = realmId,
			windowDays = windowDays
		};
		if (realms == null || !realms.TryGetValue(realmId, out RealmData realm) || realm == null)
		{
			return report;
		}
		report.treasury = realm.treasury;
		int today = calendar.AbsoluteDay(currentYear, currentDay);
		List<TreasuryTransaction> transactions = TreasuryLedger.GetHistory(realmId, today - windowDays);
		Dictionary<string, RealmBudgetLine> lines = new Dictionary<string, RealmBudgetLine>();
		foreach (TreasuryTransaction transaction in transactions)
		{
			if (!lines.TryGetValue(transaction.reason, out RealmBudgetLine line))
			{
				line = new RealmBudgetLine { reason = transaction.reason };
				lines[transaction.reason] = line;
			}
			line.amount += transaction.amount;
			line.count++;
			if (transaction.amount >= 0f)
			{
				report.income += transaction.amount;
			}
			else
			{
				report.expense += -transaction.amount;
			}
		}
		report.net = report.income - report.expense;
		report.lines.AddRange(lines.Values);
		report.lines.Sort((a, b) => System.Math.Abs(b.amount).CompareTo(System.Math.Abs(a.amount)));
		return report;
	}
}
}
