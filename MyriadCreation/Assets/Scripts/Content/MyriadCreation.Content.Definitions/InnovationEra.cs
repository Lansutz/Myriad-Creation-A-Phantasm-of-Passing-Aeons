namespace MyriadCreation.Content.Definitions
{
public static class InnovationEra
{
	public static readonly int[] StartYears = new int[6] { -100000, -10000, -3300, -1200, 500, 1500 };

	public static int GetEraForYear(int year)
	{
		for (int num = StartYears.Length - 1; num >= 0; num--)
		{
			if (year >= StartYears[num])
			{
				return num;
			}
		}
		return 0;
	}

	public static float GetPreferenceWeight(int innovationEra, int currentEra)
	{
		int num = currentEra - innovationEra;
		if (num >= 0)
		{
			return num switch
			{
				1 => 1.1f, 
				0 => 1.2f, 
				_ => 1f, 
			};
		}
		if (num == -1)
		{
			return 0.8f;
		}
		return 0.6f;
	}
}
}
