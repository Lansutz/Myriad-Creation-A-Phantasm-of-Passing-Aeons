using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class WarState
{
	public int warId;

	public int attackerId;

	public int defenderId;

	public float attackerScore;

	public float defenderScore;

	public int startDay;

	public int lastBattleDay = -1;

	public bool ended;

	public int winnerId = -1;

	public string outcome = "";

	public string reason = "";

	/// <summary>宣战理由真源：为什么能打。可为空（无借口突袭），但必须留在战争运行态上。</summary>
	public CasusBelli casusBelli;

	/// <summary>战争目标真源：打什么。与 <see cref="CasusBelli"/>、<see cref="Treaty"/> 分属三层，不混写。</summary>
	public List<WarGoal> warGoals = new List<WarGoal>();

	/// <summary>最近一次战争目标变更日；-1 表示从未变更。理由不可变，目标可变但有冷却与依据约束。</summary>
	public int lastWarGoalChangeDay = -1;

	public int warGoalRevisionCount;

	public WarState(int warId, int attackerId, int defenderId, int startDay)
	{
		this.warId = warId;
		this.attackerId = attackerId;
		this.defenderId = defenderId;
		this.startDay = startDay;
	}

	public float GetScore(int realmId)
	{
		if (realmId != attackerId)
		{
			return defenderScore;
		}
		return attackerScore;
	}

	public void AddScore(int realmId, float amount)
	{
		if (realmId == attackerId)
		{
			attackerScore += amount;
		}
		else if (realmId == defenderId)
		{
			defenderScore += amount;
		}
	}
}
}
