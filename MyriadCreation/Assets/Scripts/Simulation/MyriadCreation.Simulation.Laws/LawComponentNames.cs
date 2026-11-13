using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
public static class LawComponentNames
{
	public static string NameSupremeSuccession(int c)
	{
		return c switch
		{
			0 => "世袭", 
			1 => "选举·直接", 
			2 => "选举·代议", 
			3 => "僭夺", 
			4 => "轮座", 
			_ => "?", 
		};
	}

	public static string NameSupremeScope(int c)
	{
		return c switch
		{
			0 => "全能", 
			1 => "法理受限", 
			2 => "惯例约束", 
			3 => "共议制约", 
			4 => "神意约束", 
			_ => "?", 
		};
	}

	public static string NameCentralSuccession(int c)
	{
		return c switch
		{
			0 => "上级决断", 
			1 => "集体选择", 
			2 => "客观标准", 
			3 => "血缘世袭", 
			_ => "?", 
		};
	}

	public static string NameCentralInstitution(int c)
	{
		return c switch
		{
			0 => "无常设", 
			1 => "王庭", 
			2 => "议会/元老院", 
			3 => "长老议事会", 
			4 => "官僚中枢", 
			5 => "宗教会议", 
			6 => "军事委员会", 
			_ => "?", 
		};
	}

	public static string NameLocalSuccession(int c)
	{
		return c switch
		{
			0 => "任命", 
			1 => "选举推举", 
			3 => "世袭领有", 
			4 => "城市特许自治", 
			_ => "?", 
		};
	}

	public static string NameLocalScope(int c)
	{
		return c switch
		{
			0 => "全权自治", 
			1 => "征税司法", 
			2 => "仅军事驻防", 
			3 => "完全直辖", 
			_ => "?", 
		};
	}

	public static string NameSpatialStructure(int c)
	{
		return c switch
		{
			0 => "单一制", 
			1 => "联邦制", 
			2 => "邦联制", 
			_ => "?", 
		};
	}
}
}
