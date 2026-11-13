using System;
using System.Collections.Generic;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class TaxSystemDTO
{
	public float agriculturalTax = 0.1f;

	public float headTax = 0.05f;

	public float tradeTax = 0.1f;

	public float miningTax = 0.15f;

	public float craftTax = 0.1f;

	public float livestockTax = 0.08f;

	public float luxuryTax = 0.3f;

	public float saltMonopolyTax = 0.5f;

	public float wartimeSpecialTax;

	public List<IntBoolEntry> taxExemptions = new List<IntBoolEntry>();

	public static TaxSystemDTO FromTaxSystem(TaxSystem t)
	{
		TaxSystemDTO taxSystemDTO = new TaxSystemDTO
		{
			agriculturalTax = t.agriculturalTax,
			headTax = t.headTax,
			tradeTax = t.tradeTax,
			miningTax = t.miningTax,
			craftTax = t.craftTax,
			livestockTax = t.livestockTax,
			luxuryTax = t.luxuryTax,
			saltMonopolyTax = t.saltMonopolyTax,
			wartimeSpecialTax = t.wartimeSpecialTax
		};
		if (t.taxExemptions != null)
		{
			foreach (KeyValuePair<GameEnums.SocialClass, bool> taxExemption in t.taxExemptions)
			{
				taxSystemDTO.taxExemptions.Add(new IntBoolEntry((int)taxExemption.Key, taxExemption.Value));
			}
		}
		return taxSystemDTO;
	}

	public TaxSystem ToTaxSystem()
	{
		TaxSystem taxSystem = new TaxSystem
		{
			agriculturalTax = agriculturalTax,
			headTax = headTax,
			tradeTax = tradeTax,
			miningTax = miningTax,
			craftTax = craftTax,
			livestockTax = livestockTax,
			luxuryTax = luxuryTax,
			saltMonopolyTax = saltMonopolyTax,
			wartimeSpecialTax = wartimeSpecialTax
		};
		foreach (IntBoolEntry taxExemption in taxExemptions)
		{
			taxSystem.taxExemptions[(GameEnums.SocialClass)taxExemption.key] = taxExemption.value;
		}
		return taxSystem;
	}
}
}
