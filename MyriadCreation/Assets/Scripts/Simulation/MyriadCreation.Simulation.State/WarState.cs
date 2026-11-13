namespace MyriadCreation.Simulation.State
{
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
