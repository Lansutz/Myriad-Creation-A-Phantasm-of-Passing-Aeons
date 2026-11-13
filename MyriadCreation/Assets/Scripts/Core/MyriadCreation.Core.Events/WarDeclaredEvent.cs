namespace MyriadCreation.Core.Events
{
public readonly struct WarDeclaredEvent(int warId, int attackerId, int defenderId, int day)
{
	public readonly int WarId = warId;

	public readonly int AttackerId = attackerId;

	public readonly int DefenderId = defenderId;

	public readonly int Day = day;
}
}
