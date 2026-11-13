using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Economy
{
public static class ClassEmergenceEvents
{
	private static readonly Dictionary<int, List<GameEnums.SocialSubclass>> EmergenceMap = new Dictionary<int, List<GameEnums.SocialSubclass>>
	{
		[952] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Serf },
		[501] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Tenant },
		[701] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Merchant },
		[600] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Scholar },
		[505] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.DebtSlave },
		[503] = new List<GameEnums.SocialSubclass>
		{
			GameEnums.SocialSubclass.Scholar,
			GameEnums.SocialSubclass.StateSlave
		},
		[502] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.StateSlave },
		[980] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Citizen },
		[500] = new List<GameEnums.SocialSubclass>
		{
			GameEnums.SocialSubclass.Citizen,
			GameEnums.SocialSubclass.DomesticSlave
		},
		[100] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Freeholder }
	};

	public static List<GameEnums.SocialSubclass> GetEmergingClasses(int completedInnovationId, CultureData culture, InnovationTree innovations, int realmId)
	{
		List<GameEnums.SocialSubclass> list = new List<GameEnums.SocialSubclass>();
		if (!EmergenceMap.TryGetValue(completedInnovationId, out var value))
		{
			return list;
		}
		foreach (GameEnums.SocialSubclass item in value)
		{
			if (SocialClassAvailability.IsSubclassAvailable(item, culture, innovations, realmId))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static string GetEventText(GameEnums.SocialSubclass subclass)
	{
		return subclass switch
		{
			GameEnums.SocialSubclass.Serf => "庄园制兴，农奴阶层出现——人身束缚于土地的新秩序。", 
			GameEnums.SocialSubclass.Tenant => "土地租佃成风，佃农阶层出现。", 
			GameEnums.SocialSubclass.Merchant => "钱币通行，商人阶层出现——市井之声渐盛。", 
			GameEnums.SocialSubclass.Scholar => "文墨传世，士人阶层出现——读书人有了自己的位置。", 
			GameEnums.SocialSubclass.DebtSlave => "律法既立，债务奴出现——欠债者以身抵偿。", 
			GameEnums.SocialSubclass.StateSlave => "官府大兴，官奴阶层出现——国有劳役的征发对象。", 
			GameEnums.SocialSubclass.Citizen => "城邦议事，市民阶层出现——公民权落地生根。", 
			GameEnums.SocialSubclass.DomesticSlave => "私有既立，家奴出现——仆役成群之家渐多。", 
			GameEnums.SocialSubclass.Freeholder => "垦荒成田，自耕农阶层出现——有地之家心安。", 
			_ => "新阶层出现。", 
		};
	}

	public static int RecordEmergence(int completedInnovationId, string realmName, CultureData culture, InnovationTree innovations, int realmId, Chronicle chronicle)
	{
		List<GameEnums.SocialSubclass> emergingClasses = GetEmergingClasses(completedInnovationId, culture, innovations, realmId);
		foreach (GameEnums.SocialSubclass item in emergingClasses)
		{
			chronicle?.Add("class_emergence", realmName + "：" + GetEventText(item), false, realmId);
		}
		return emergingClasses.Count;
	}
}
}
