using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class AdminDivisionSystem
{
	public static string LevelName(int level, bool feudal)
	{
		if (level <= 1)
		{
			return "政权";
		}
		if (feudal)
		{
			return level switch
			{
				2 => "诸侯", 
				3 => "卿大夫", 
				4 => "士家", 
				_ => "封臣", 
			};
		}
		return level switch
		{
			2 => "郡", 
			3 => "县", 
			4 => "乡", 
			5 => "亭", 
			_ => "区", 
		};
	}

	public static int GetAdminDepth(LawComposition comp, float administrativeCapacity)
	{
		if (comp == null || comp.localSuccession == null || comp.localSuccession.primary < 0)
		{
			return 1;
		}
		switch ((LocalSuccession)comp.localSuccession.primary)
		{
		case LocalSuccession.Hereditary:
			return 4;
		case LocalSuccession.Appointed:
		case LocalSuccession.Examination:
			if (administrativeCapacity < 0.2f)
			{
				return 2;
			}
			if (administrativeCapacity < 0.4f)
			{
				return 3;
			}
			if (administrativeCapacity < 0.65f)
			{
				return 4;
			}
			return 5;
		case LocalSuccession.Elected:
		case LocalSuccession.CityCharter:
			return 2;
		default:
			return 1;
		}
	}

	public static List<AdminDivision> Generate(RealmData realm, LawComposition comp, float administrativeCapacity, HashSet<int> territoryTiles)
	{
		List<AdminDivision> list = new List<AdminDivision>();
		if (realm == null)
		{
			return list;
		}
		int adminDepth = GetAdminDepth(comp, administrativeCapacity);
		bool feudal = comp != null && comp.localSuccession != null && comp.localSuccession.primary == 3;
		AdminDivision adminDivision = new AdminDivision
		{
			divisionId = realm.realmId * 10000 + 1,
			realmId = realm.realmId,
			level = 1,
			name = realm.realmName,
			parentDivisionId = -1
		};
		if (territoryTiles != null)
		{
			adminDivision.tiles = new HashSet<int>(territoryTiles);
		}
		list.Add(adminDivision);
		BuildLevels(adminDivision, 2, adminDepth, feudal, list);
		return list;
	}

	private static void BuildLevels(AdminDivision parent, int level, int maxDepth, bool feudal, List<AdminDivision> divisions)
	{
		if (level > maxDepth || parent.tiles.Count == 0)
		{
			return;
		}
		int num = 1;
		int num2 = Math.Min(4, parent.tiles.Count);
		if (num2 <= 0)
		{
			return;
		}
		List<int> list = new List<int>(parent.tiles);
		int num3 = list.Count / num2;
		if (num3 == 0)
		{
			num2 = list.Count;
			num3 = 1;
		}
		for (int i = 0; i < num2; i++)
		{
			AdminDivision adminDivision = new AdminDivision
			{
				divisionId = parent.realmId * 10000 + level * 100 + num,
				realmId = parent.realmId,
				level = level,
				name = $"{realmName(parent.realmId)}·{LevelName(level, feudal)}{num}",
				parentDivisionId = parent.divisionId
			};
			int num4 = i * num3;
			int num5 = ((i == num2 - 1) ? list.Count : (num4 + num3));
			for (int j = num4; j < num5 && j < list.Count; j++)
			{
				adminDivision.tiles.Add(list[j]);
			}
			adminDivision.titleId = ResolveTitleId(level, feudal);
			parent.childIds.Add(adminDivision.divisionId);
			divisions.Add(adminDivision);
			BuildLevels(adminDivision, level + 1, maxDepth, feudal, divisions);
		}
	}

	private static string realmName(int realmId)
	{
		return $"R{realmId}";
	}

	private static string ResolveTitleId(int level, bool feudal)
	{
		if (feudal)
		{
			return level switch
			{
				2 => "title_zhuhou", 
				3 => "title_qingdafu", 
				_ => "title_baron", 
			};
		}
		switch (level)
		{
		case 2:
			return "title_gov_general";
		case 3:
			return "title_prefect";
		default:
			_ = 4;
			return "title_magistrate";
		}
	}

	public static List<AdminDivision> GetDivisions(List<AdminDivision> all, int realmId)
	{
		List<AdminDivision> list = new List<AdminDivision>();
		if (all == null)
		{
			return list;
		}
		foreach (AdminDivision item in all)
		{
			if (item.realmId == realmId)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static List<AdminDivision> AtLevel(List<AdminDivision> all, int realmId, int level)
	{
		List<AdminDivision> list = new List<AdminDivision>();
		foreach (AdminDivision item in all)
		{
			if (item.realmId == realmId && item.level == level)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static AdminDivision DivisionOfTile(List<AdminDivision> all, int realmId, int tileIndex)
	{
		AdminDivision adminDivision = null;
		foreach (AdminDivision item in all)
		{
			if (item.realmId == realmId && item.tiles.Contains(tileIndex) && (adminDivision == null || item.level > adminDivision.level))
			{
				adminDivision = item;
			}
		}
		return adminDivision;
	}
}
}
