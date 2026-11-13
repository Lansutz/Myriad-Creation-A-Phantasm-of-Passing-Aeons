using System;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class InheritanceLaw
{
	public InheritanceScope scope = InheritanceScope.CognaticKin;

	public InheritanceBranch branch;

	public InheritanceGender gender;

	public InheritanceAge age;

	public TitleInheritanceMode titleMode;

	public LandInheritanceMode landMode;

	public InheritanceLaw()
	{
	}

	public InheritanceLaw(InheritanceScope scope, InheritanceBranch branch, InheritanceGender gender, InheritanceAge age, TitleInheritanceMode titleMode = TitleInheritanceMode.SoleHeir, LandInheritanceMode landMode = LandInheritanceMode.Primogeniture)
	{
		this.scope = scope;
		this.branch = branch;
		this.gender = gender;
		this.age = age;
		this.titleMode = titleMode;
		this.landMode = landMode;
	}

	public static InheritanceLaw Primogeniture()
	{
		return new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine, InheritanceGender.MalePreference, InheritanceAge.Seniority);
	}

	public static InheritanceLaw Ultimogeniture()
	{
		return new InheritanceLaw(InheritanceScope.ClanOnly, InheritanceBranch.EldestLine, InheritanceGender.MalePreference, InheritanceAge.Juniority, TitleInheritanceMode.SoleHeir, LandInheritanceMode.Ultimogeniture);
	}

	public static InheritanceLaw Tanistry()
	{
		return new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.Collateral, InheritanceGender.MaleOnly, InheritanceAge.Seniority, TitleInheritanceMode.SoleHeir, LandInheritanceMode.Seniority);
	}

	public static InheritanceLaw Matrilineal()
	{
		return new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine, InheritanceGender.FemaleOnly, InheritanceAge.Seniority);
	}

	public static InheritanceLaw FrankishPartible()
	{
		return new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine, InheritanceGender.MaleOnly, InheritanceAge.Seniority, TitleInheritanceMode.FamilyShared, LandInheritanceMode.Partible);
	}

	public static InheritanceLaw Salic()
	{
		return new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine, InheritanceGender.MaleOnly, InheritanceAge.Seniority, TitleInheritanceMode.SoleHeir, LandInheritanceMode.Partible);
	}

	public static InheritanceLaw ChinesePartible()
	{
		return new InheritanceLaw(InheritanceScope.ClanOnly, InheritanceBranch.EldestLine, InheritanceGender.MalePreference, InheritanceAge.Seniority, TitleInheritanceMode.SoleHeir, LandInheritanceMode.Partible);
	}

	[Obsolete("选举是产生方式（A1 交接方式），继承法不含选举——使用 LawComposition 的 supremeSuccession 表达")]
	public static InheritanceLaw ElectiveMonarchy()
	{
		return new InheritanceLaw(InheritanceScope.CognaticKin, InheritanceBranch.EldestLine, InheritanceGender.Equal, InheritanceAge.Seniority);
	}

	public string GetName()
	{
		string text = ((scope == InheritanceScope.ClanOnly) ? "宗族" : "血亲");
		string text2 = ((branch == InheritanceBranch.EldestLine) ? "长支" : "兄终弟及");
		string text3 = gender switch
		{
			InheritanceGender.MalePreference => "男子优先", 
			InheritanceGender.MaleOnly => "男子专属", 
			InheritanceGender.Equal => "男女平等", 
			InheritanceGender.FemalePreference => "女子优先", 
			InheritanceGender.FemaleOnly => "女子专属", 
			_ => "男子优先", 
		};
		string text4 = ((age == InheritanceAge.Seniority) ? "年长先" : "年幼先");
		string text5 = titleMode switch
		{
			TitleInheritanceMode.SoleHeir => "头衔唯一", 
			TitleInheritanceMode.FamilyShared => "头衔家族共享", 
			_ => "头衔唯一", 
		};
		string text6 = landMode switch
		{
			LandInheritanceMode.Primogeniture => "领地长子独得", 
			LandInheritanceMode.Partible => "领地均分", 
			LandInheritanceMode.Ultimogeniture => "领地幼子守灶", 
			LandInheritanceMode.Seniority => "领地轮序", 
			LandInheritanceMode.Entail => "领地限定", 
			LandInheritanceMode.Collective => "领地共有", 
			_ => "领地长子独得", 
		};
		return text + "·" + text2 + "·" + text3 + "·" + text4 + "｜" + text5 + "·" + text6;
	}

	public int[] DistributeLand(int heirCount, int landCount)
	{
		if (heirCount <= 0 || landCount <= 0)
		{
			return new int[0];
		}
		int[] array = new int[heirCount];
		switch (landMode)
		{
		case LandInheritanceMode.Partible:
		{
			int num = landCount / heirCount;
			int num2 = landCount % heirCount;
			for (int i = 0; i < heirCount; i++)
			{
				array[i] = num + ((i < num2) ? 1 : 0);
			}
			break;
		}
		case LandInheritanceMode.Ultimogeniture:
			array[heirCount - 1] = landCount;
			break;
		case LandInheritanceMode.Collective:
			array[0] = landCount;
			break;
		default:
			array[0] = landCount;
			break;
		}
		return array;
	}

}
}
