using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.UI.Presentation
{
public sealed class PopulationCompositionPanelModel
{
	public string title = "";

	public float total;

	public PopulationCompositionCategory category;

	public List<PopulationCompositionRow> rows = new List<PopulationCompositionRow>();

	public static PopulationCompositionPanelModel Build(PopulationComposition composition, PopulationCompositionCategory category, Func<int, string> nameResolver, Func<int, string> colorResolver, string title = "", Func<int, int> traditionResolver = null)
	{
		PopulationCompositionPanelModel populationCompositionPanelModel = new PopulationCompositionPanelModel
		{
			title = title,
			category = category,
			total = (composition?.Total ?? 0f)
		};
		if (composition == null || composition.Total <= 0f)
		{
			return populationCompositionPanelModel;
		}
		Dictionary<int, float> dictionary = Values(composition, category, traditionResolver);
		float num = 0f;
		int num2 = -1;
		foreach (KeyValuePair<int, float> item in dictionary)
		{
			if (item.Value > num)
			{
				num = item.Value;
				num2 = item.Key;
			}
		}
		foreach (KeyValuePair<int, float> item2 in dictionary)
		{
			if (!(item2.Value <= 0f))
			{
				populationCompositionPanelModel.rows.Add(new PopulationCompositionRow
				{
					id = item2.Key,
					name = (nameResolver?.Invoke(item2.Key) ?? item2.Key.ToString()),
					colorHex = (colorResolver?.Invoke(item2.Key) ?? "#808080"),
					count = item2.Value,
					percent = ((populationCompositionPanelModel.total > 0f) ? (item2.Value / populationCompositionPanelModel.total * 100f) : 0f),
					isDominant = (item2.Key == num2)
				});
			}
		}
		populationCompositionPanelModel.rows.Sort((PopulationCompositionRow a, PopulationCompositionRow b) => b.count.CompareTo(a.count));
		return populationCompositionPanelModel;
	}

	private static Dictionary<int, float> Values(PopulationComposition composition, PopulationCompositionCategory category, Func<int, int> traditionResolver)
	{
		return category switch
		{
			PopulationCompositionCategory.Culture => composition.ByCulture(), 
			PopulationCompositionCategory.Religion => composition.ByFaith(), 
			PopulationCompositionCategory.ReligionTradition => composition.ByTradition(traditionResolver), 
			PopulationCompositionCategory.Race => composition.ByRace(), 
			PopulationCompositionCategory.EconomicClass => ToInt(composition.ByEconomicClass()), 
			PopulationCompositionCategory.Stratum => ToInt(composition.ByStratum()), 
			PopulationCompositionCategory.Occupation => ToInt(composition.ByOccupation()), 
			_ => new Dictionary<int, float>(), 
		};
	}

	private static Dictionary<int, float> ToInt<T>(Dictionary<T, float> source)
	{
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		foreach (KeyValuePair<T, float> item in source)
		{
			dictionary[Convert.ToInt32(item.Key)] = item.Value;
		}
		return dictionary;
	}

	public static PopulationCompositionCategory ToCategory(int index)
	{
		return (PopulationCompositionCategory)Math.Max(0, Math.Min(6, index));
	}
}
}
