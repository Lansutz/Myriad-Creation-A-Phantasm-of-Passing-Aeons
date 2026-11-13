using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class PoliticalAccessAnalyzer
{
	public static float GetAccess(GameEnums.SocialClass cls, LawComposition comp)
	{
		if (comp == null)
		{
			return 0.2f;
		}
		bool flag = SupremeSuccessionLevel.IsElective((SupremeSuccession)comp.supremeSuccession.primary);
		bool flag2 = comp.supremeSuccession.primary == 1;
		int primary = comp.centralInstitution.primary;
		bool flag3 = primary == 2;
		bool flag4 = primary == 3;
		bool flag5 = primary == 5;
		bool flag6 = primary == 4;
		int primary2 = comp.localSuccession.primary;
		bool flag7 = primary2 == 2;
		bool flag8 = primary2 == 4;
		bool flag9 = primary2 == 1;
		bool flag10 = comp.supremeSuccession.primary == 0;
		bool flag11 = primary2 == 3;
		bool flag12 = flag3 && (comp.assemblyComposition == AssemblyComposition.Bicameral || comp.assemblyComposition == AssemblyComposition.Estate);
		switch (cls)
		{
		case GameEnums.SocialClass.Royalty:
			if (!flag10)
			{
				if (!flag)
				{
					return 0.6f;
				}
				return 0.35f;
			}
			return 1f;
		case GameEnums.SocialClass.NobilityClergy:
		{
			float num = 0.2f;
			if (flag10)
			{
				num += 0.3f;
			}
			if (flag11)
			{
				num += 0.25f;
			}
			if (flag4)
			{
				num += 0.3f;
			}
			if (flag3)
			{
				num += (flag12 ? 0.15f : 0.3f);
			}
			if (flag5)
			{
				num += 0.25f;
			}
			return Mathf.Clamp01(num);
		}
		case GameEnums.SocialClass.MerchantFreeman:
		{
			float num2 = 0.15f;
			if (flag)
			{
				num2 += (flag2 ? 0.35f : 0.25f);
			}
			if (flag3)
			{
				num2 += (flag12 ? 0.35f : 0.2f);
			}
			if (flag7)
			{
				num2 += 0.3f;
			}
			if (flag8)
			{
				num2 += 0.3f;
			}
			if (flag9)
			{
				num2 += 0.2f;
			}
			if (flag6 & flag7)
			{
				num2 += 0.1f;
			}
			return Mathf.Clamp01(num2);
		}
		case GameEnums.SocialClass.Peasant:
			if (!flag2)
			{
				return 0.12f;
			}
			return 0.45f;
		case GameEnums.SocialClass.Slave:
			return 0f;
		default:
			return 0.2f;
		}
	}

	public static Dictionary<GameEnums.SocialClass, float> GetAllAccess(LawComposition comp)
	{
		Dictionary<GameEnums.SocialClass, float> dictionary = new Dictionary<GameEnums.SocialClass, float>();
		foreach (GameEnums.SocialClass value in Enum.GetValues(typeof(GameEnums.SocialClass)))
		{
			dictionary[value] = GetAccess(value, comp);
		}
		return dictionary;
	}
}
}
