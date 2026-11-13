namespace MyriadCreation.Content.Definitions
{
public static class SpecialtyTypeExtensions
{
	public static readonly SpecialtyType[] All = new SpecialtyType[6]
	{
		SpecialtyType.Social,
		SpecialtyType.Conspiracy,
		SpecialtyType.Military,
		SpecialtyType.Management,
		SpecialtyType.Scholarship,
		SpecialtyType.Prowess
	};

	public static string GetDisplayName(this SpecialtyType type)
	{
		return type switch
		{
			SpecialtyType.Social => "社交", 
			SpecialtyType.Conspiracy => "阴谋", 
			SpecialtyType.Military => "军事", 
			SpecialtyType.Management => "管理", 
			SpecialtyType.Scholarship => "学识", 
			SpecialtyType.Prowess => "勇武", 
			_ => type.ToString(), 
		};
	}

	public static string GetDescription(this SpecialtyType type)
	{
		return type switch
		{
			SpecialtyType.Social => "处理各种关系的能力，范围从国家间的正式关系到个人间的非正式关系", 
			SpecialtyType.Conspiracy => "秘密策划和政治手腕的能力", 
			SpecialtyType.Military => "军事指挥和战略能力", 
			SpecialtyType.Management => "行政管理和经济治理能力", 
			SpecialtyType.Scholarship => "知识水平和学习能力", 
			SpecialtyType.Prowess => "个人战斗能力和勇猛程度", 
			_ => "", 
		};
	}
}
}
