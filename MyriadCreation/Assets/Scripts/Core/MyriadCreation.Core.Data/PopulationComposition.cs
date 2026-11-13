using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Data
{
[Serializable]
public class PopulationComposition
{
	public List<PopulationGroup> groups = new List<PopulationGroup>();

	public float Total
	{
		get
		{
			float num = 0f;
			foreach (PopulationGroup group in groups)
			{
				num += group.count;
			}
			return num;
		}
	}

	public void Clear()
	{
		groups.Clear();
	}

	public void Add(float count, int raceId, int cultureId, int faithId, EconomicClass economicClass = EconomicClass.Freeholder, SocialStratum stratum = SocialStratum.Middle, OccupationFlags occupations = OccupationFlags.None, int religionTraditionId = -1)
	{
		if (!(count <= 0f))
		{
			groups.Add(new PopulationGroup
			{
				count = count,
				raceId = raceId,
				cultureId = cultureId,
				faithId = faithId,
				religionTraditionId = religionTraditionId,
				economicClass = economicClass,
				stratum = stratum,
				occupations = occupations
			});
		}
	}

	public void AddBlocks(IEnumerable<PopulationBlock> blocks)
	{
		if (blocks == null)
		{
			return;
		}
		foreach (PopulationBlock block in blocks)
		{
			if (!(block.count <= 0f))
			{
				groups.Add(new PopulationGroup
				{
					count = block.count,
					raceId = block.raceId,
					cultureId = block.cultureId,
					faithId = block.faithId,
					religionTraditionId = -1,
					economicClass = block.economicClass,
					stratum = block.stratum,
					occupations = block.occupations
				});
			}
		}
	}

	public Dictionary<int, float> ByCulture()
	{
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		foreach (PopulationGroup group in groups)
		{
			dictionary[group.cultureId] = Get(dictionary, group.cultureId) + group.count;
		}
		return dictionary;
	}

	public Dictionary<int, float> ByFaith()
	{
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		foreach (PopulationGroup group in groups)
		{
			dictionary[group.faithId] = Get(dictionary, group.faithId) + group.count;
		}
		return dictionary;
	}

	public Dictionary<int, float> ByTradition(Func<int, int> traditionResolver = null)
	{
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		foreach (PopulationGroup group in groups)
		{
			int num = group.religionTraditionId;
			if (num < 0 && traditionResolver != null)
			{
				num = traditionResolver(group.faithId);
			}
			if (num >= 0)
			{
				dictionary[num] = Get(dictionary, num) + group.count;
			}
		}
		return dictionary;
	}

	public Dictionary<int, float> ByRace()
	{
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		foreach (PopulationGroup group in groups)
		{
			dictionary[group.raceId] = Get(dictionary, group.raceId) + group.count;
		}
		return dictionary;
	}

	public Dictionary<EconomicClass, float> ByEconomicClass()
	{
		Dictionary<EconomicClass, float> dictionary = new Dictionary<EconomicClass, float>();
		foreach (PopulationGroup group in groups)
		{
			dictionary[group.economicClass] = Get(dictionary, group.economicClass) + group.count;
		}
		return dictionary;
	}

	public Dictionary<SocialStratum, float> ByStratum()
	{
		Dictionary<SocialStratum, float> dictionary = new Dictionary<SocialStratum, float>();
		foreach (PopulationGroup group in groups)
		{
			dictionary[group.stratum] = Get(dictionary, group.stratum) + group.count;
		}
		return dictionary;
	}

	public Dictionary<OccupationFlags, float> ByOccupation()
	{
		Dictionary<OccupationFlags, float> dictionary = new Dictionary<OccupationFlags, float>();
		foreach (PopulationGroup group in groups)
		{
			if (group.occupations == OccupationFlags.None)
			{
				continue;
			}
			foreach (OccupationFlags value in Enum.GetValues(typeof(OccupationFlags)))
			{
				if (value != OccupationFlags.None && (group.occupations & value) != 0)
				{
					dictionary[value] = Get(dictionary, value) + group.count;
				}
			}
		}
		return dictionary;
	}

	public int DominantCulture(int fallback = -1)
	{
		return Dominant(ByCulture(), fallback);
	}

	public int DominantFaith(int fallback = -1)
	{
		return Dominant(ByFaith(), fallback);
	}

	public int DominantTradition(Func<int, int> traditionResolver = null, int fallback = -1)
	{
		return Dominant(ByTradition(traditionResolver), fallback);
	}

	public int DominantRace(int fallback = -1)
	{
		return Dominant(ByRace(), fallback);
	}

	private static T Dominant<T>(Dictionary<T, float> values, T fallback)
	{
		T result = fallback;
		float num = 0f;
		foreach (KeyValuePair<T, float> value in values)
		{
			if (value.Value > num)
			{
				num = value.Value;
				result = value.Key;
			}
		}
		return result;
	}

	private static float Get<T>(Dictionary<T, float> dict, T key)
	{
		if (!dict.TryGetValue(key, out var value))
		{
			return 0f;
		}
		return value;
	}
}
}
