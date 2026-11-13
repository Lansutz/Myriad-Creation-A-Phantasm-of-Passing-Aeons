using System.Collections.Generic;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
public static class MentalHealthSystem
{
	public const int HighStressTriggerDays = 90;

	public const float DreadParanoiaThreshold = 80f;

	public const int LowStressRecoveryDays = 120;

	public const int DementiaAge = 70;

	public const float DementiaLearningGate = 45f;

	public static readonly Dictionary<string, MentalDisorderDef> BuiltinDefs = new Dictionary<string, MentalDisorderDef>
	{
		["depression"] = new MentalDisorderDef
		{
			id = "depression",
			name = "抑郁",
			reversible = true,
			description = "长期高压下的意志消沉，对政务与人事提不起兴致，日渐寡言",
			learningMod = -5f,
			diplomacyMod = -5f,
			charmMod = -5f,
			stressDecayMult = 0.4f
		},
		["anxiety"] = new MentalDisorderDef
		{
			id = "anxiety",
			name = "焦虑",
			reversible = true,
			description = "终日忧惧不安，寝食难安，决断迟疑，杯弓蛇影",
			stewardshipMod = -5f,
			warfareMod = -3f,
			charmMod = -10f,
			stressDecayMult = 0.3f
		},
		["paranoia"] = new MentalDisorderDef
		{
			id = "paranoia",
			name = "偏执",
			reversible = true,
			description = "怀疑身边的一切人，把善意当作阴谋，睚眦必报",
			diplomacyMod = -8f,
			intrigueMod = 5f,
			opinionDecayMult = 2f
		},
		["delirium"] = new MentalDisorderDef
		{
			id = "delirium",
			name = "谵妄",
			reversible = true,
			description = "神志昏乱，言语无状，时而清醒时而糊涂，不识昼夜",
			martialMod = -3f,
			diplomacyMod = -3f,
			warfareMod = -3f,
			stewardshipMod = -3f,
			intrigueMod = -3f,
			learningMod = -3f
		},
		["dementia"] = new MentalDisorderDef
		{
			id = "dementia",
			name = "失智",
			reversible = false,
			description = "年迈带来的心智衰退，往事如烟，认不清亲人，性情大变",
			learningMod = -15f,
			warfareMod = -5f,
			diplomacyMod = -5f,
			charmMod = -5f
		}
	};

	public static MentalDisorderDef GetDef(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		if (ContentResolvers.MentalDisorders.TryGet(id, out var value))
		{
			return value;
		}
		if (!BuiltinDefs.TryGetValue(id, out var value2))
		{
			return null;
		}
		return value2;
	}

	public static string GetDisorderName(CharacterData c)
	{
		MentalDisorderDef def = GetDef(c.mentalDisorderId);
		if (def == null)
		{
			return "";
		}
		return def.GetName();
	}

	public static void ApplyDisorderMods(CharacterData c, out float prowess, out float social, out float military, out float management, out float conspiracy, out float scholarship, out float charm)
	{
		prowess = c.prowess;
		social = c.social;
		military = c.military;
		management = c.management;
		conspiracy = c.conspiracy;
		scholarship = c.scholarship;
		charm = c.charm;
		MentalDisorderDef def = GetDef(c.mentalDisorderId);
		if (def != null)
		{
			prowess += def.martialMod;
			social += def.diplomacyMod;
			military += def.warfareMod;
			management += def.stewardshipMod;
			conspiracy += def.intrigueMod;
			scholarship += def.learningMod;
			charm += def.charmMod;
		}
	}
}
}
