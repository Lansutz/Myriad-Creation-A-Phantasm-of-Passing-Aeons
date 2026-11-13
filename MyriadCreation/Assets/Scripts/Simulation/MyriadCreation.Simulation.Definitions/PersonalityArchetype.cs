using System.Text;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Definitions
{
public static class PersonalityArchetype
{
	private static string[] AssertiveDesc = new string[3] { "进取性偏低，性喜守成，不尚开拓", "进取性居中，能守能攻，视势而为", "进取性突出，锐意开拓，敢为天下先" };

	private static string[] BenevolentDesc = new string[3] { "宜人性偏低，心性峻刻，寡恩薄情", "宜人性适中，恩怨分明，不失仁厚", "宜人性偏高，宽厚温良，待人接物以仁" };

	private static string[] DominantDesc = new string[3] { "支配欲淡泊，恬退自守，不慕权柄", "支配欲适中，善用权柄而不滥用", "支配欲炽盛，好胜争强，志在执掌" };

	private static string[] PrudentDesc = new string[3] { "审慎性偏低，率性任情，凭好恶决断", "审慎性适中，谋定后动，不轻涉险", "审慎性突出，冷静理智，深谋远虑" };

	private static string[] DevoutDesc = new string[3] { "信仰度淡漠，礼神多出于仪节", "信仰度适中，敬神而不狂信", "信仰度笃深，虔敬事神，以教立身" };

	public static float Assertiveness(CharacterData c)
	{
		if (c != null)
		{
			return (c.boldness + 100f) / 200f;
		}
		return 0.5f;
	}

	public static float Benevolence(CharacterData c)
	{
		if (c != null)
		{
			return (c.compassion + 100f) / 200f * 0.6f + (c.honor + 100f) / 200f * 0.4f;
		}
		return 0.5f;
	}

	public static float Dominance(CharacterData c)
	{
		if (c != null)
		{
			return (c.greed + 100f) / 200f * 0.5f + (c.vengefulness + 100f) / 200f * 0.5f;
		}
		return 0.5f;
	}

	public static float Prudence(CharacterData c)
	{
		if (c != null)
		{
			return (c.rationality + 100f) / 200f;
		}
		return 0.5f;
	}

	public static float Devoutness(CharacterData c)
	{
		if (c != null)
		{
			return (c.piety + 100f) / 200f;
		}
		return 0.5f;
	}

	private static string Segment(float v)
	{
		if (!(v >= 0.65f))
		{
			if (!(v <= 0.35f))
			{
				return "1";
			}
			return "0";
		}
		return "2";
	}

	public static string Describe(CharacterData c)
	{
		if (c == null)
		{
			return "性情未明";
		}
		float num = Assertiveness(c);
		float num2 = Benevolence(c);
		float num3 = Dominance(c);
		float num4 = Prudence(c);
		float num5 = Devoutness(c);
		StringBuilder stringBuilder = new StringBuilder();
		string value = AssertiveDesc[int.Parse(Segment(num))];
		string value2 = BenevolentDesc[int.Parse(Segment(num2))];
		string value3 = DominantDesc[int.Parse(Segment(num3))];
		string value4 = PrudentDesc[int.Parse(Segment(num4))];
		string value5 = DevoutDesc[int.Parse(Segment(num5))];
		bool flag = num >= 0.5f;
		bool flag2 = num2 >= 0.5f;
		if (flag & flag2)
		{
			stringBuilder.Append("性进取而心仁厚：");
		}
		else if (flag && !flag2)
		{
			stringBuilder.Append("性雄烈而心峻刻：");
		}
		else if (!flag & flag2)
		{
			stringBuilder.Append("性宽和而好守成：");
		}
		else
		{
			stringBuilder.Append("性深沉而内敛：");
		}
		stringBuilder.Append(value).Append("；").Append(value2)
			.Append("；");
		bool flag3 = false;
		if (Mathf.Abs(num3 - 0.5f) > 0.15f)
		{
			stringBuilder.Append(value3);
			flag3 = true;
		}
		if (Mathf.Abs(num4 - 0.5f) > 0.15f)
		{
			if (flag3)
			{
				stringBuilder.Append("；");
			}
			stringBuilder.Append(value4);
			flag3 = true;
		}
		if (Mathf.Abs(num5 - 0.5f) > 0.15f)
		{
			if (flag3)
			{
				stringBuilder.Append("；");
			}
			stringBuilder.Append(value5);
		}
		stringBuilder.Append("。");
		return stringBuilder.ToString();
	}

	public static string TypeName(CharacterData c)
	{
		if (c == null)
		{
			return "未知";
		}
		bool flag = Assertiveness(c) >= 0.5f;
		bool flag2 = Benevolence(c) >= 0.5f;
		float num = Dominance(c);
		if (Devoutness(c) >= 0.65f)
		{
			if (!flag)
			{
				return "虔信守成型";
			}
			return "虔信进取型";
		}
		if (num >= 0.65f)
		{
			if (!flag)
			{
				return "阴鸷权谋型";
			}
			return "雄略支配型";
		}
		if (flag & flag2)
		{
			return "仁厚开拓型";
		}
		if (flag)
		{
			return "刚烈果决型";
		}
		if (flag2)
		{
			return "温良守成型";
		}
		return "沉静内敛型";
	}
}
}
