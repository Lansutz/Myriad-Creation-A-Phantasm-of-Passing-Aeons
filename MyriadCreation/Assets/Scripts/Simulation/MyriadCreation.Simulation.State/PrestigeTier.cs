using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.State
{
/// <summary>
/// 威望等级：威望是声望容量/社会地位，不是可花费货币。
/// 等级决定"你能把战争目标推到多大"，而不是从账上扣掉多少点。
/// </summary>
public enum PrestigeTier
{
	Unknown,
	Local,
	Regional,
	Realm,
	Eminent
}

public static class PrestigeTiers
{
	/// <summary>等级阈值（可模组化内容，先内置；mod 覆盖走内容表后接）。</summary>
	private static readonly int[] Thresholds = { 0, 100, 300, 700, 1500 };

	public static PrestigeTier Of(float prestige)
	{
		int tier = 0;
		for (int i = 1; i < Thresholds.Length; i++)
		{
			if (prestige >= Thresholds[i])
			{
				tier = i;
			}
		}
		return (PrestigeTier)tier;
	}

	/// <summary>目标所需的最低威望等级；越是要灭国/吞并，越需要社会声望支撑。</summary>
	public static PrestigeTier RequiredFor(GameEnums.WarGoalType type)
	{
		return type switch
		{
			GameEnums.WarGoalType.Humiliation => PrestigeTier.Local,
			GameEnums.WarGoalType.Indemnity => PrestigeTier.Local,
			GameEnums.WarGoalType.BorderAdjustment => PrestigeTier.Local,
			GameEnums.WarGoalType.ConquerTerritory => PrestigeTier.Regional,
			GameEnums.WarGoalType.ConvertReligion => PrestigeTier.Regional,
			GameEnums.WarGoalType.ConquerRegion => PrestigeTier.Realm,
			GameEnums.WarGoalType.Vassalization => PrestigeTier.Realm,
			GameEnums.WarGoalType.Independence => PrestigeTier.Realm,
			GameEnums.WarGoalType.Annihilation => PrestigeTier.Eminent,
			_ => PrestigeTier.Local
		};
	}
}
}
