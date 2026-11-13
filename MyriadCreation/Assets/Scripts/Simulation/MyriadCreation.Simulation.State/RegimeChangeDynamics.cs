using System.Collections.Generic;
using MyriadCreation.Simulation.Systems;
using UnityEngine;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Simulation.State
{
public class RegimeChangeDynamics
{
	private readonly Dictionary<int, RegimeChangeState> _states = new Dictionary<int, RegimeChangeState>();

	private InnovationTree _innovations;

	private Chronicle _chronicle;

	private const int WindowDays = 120;

	private const float OpenSeverityBase = 45f;

	private const float InertiaPerYear = 1.2f;

	private const float InertiaMax = 40f;

	private const float AutoUprisingTension = 65f;

	private const float AutoFiscalTreasury = -200f;

	public RegimeChangeDynamics(InnovationTree innovations = null, Chronicle chronicle = null)
	{
		_innovations = innovations;
		_chronicle = chronicle;
	}

	public void SetInnovationTree(InnovationTree t)
	{
		_innovations = t;
	}

	public RegimeChangeState GetState(int realmId)
	{
		if (!_states.TryGetValue(realmId, out var value))
		{
			return null;
		}
		return value;
	}

	private RegimeChangeState Ensure(int realmId, int day)
	{
		if (!_states.TryGetValue(realmId, out var value))
		{
			value = new RegimeChangeState
			{
				realmId = realmId,
				compositionEstablishedDay = day
			};
			_states[realmId] = value;
		}
		return value;
	}

	public float GetRegimeAgeYears(int realmId, int currentDay)
	{
		RegimeChangeState state = GetState(realmId);
		if (state != null)
		{
			return Mathf.Max(0f, (float)(currentDay - state.compositionEstablishedDay)) / 365f;
		}
		return 0f;
	}

	public void Tick(int currentDay, RealmData realm, RealmSociety society, RealmSituation sit, FactionManager factions)
	{
		RegimeChangeState regimeChangeState = Ensure(realm.realmId, currentDay);
		regimeChangeState.tension.Recalculate(society, sit, realm);
		float num = Mathf.Max(0f, (float)(currentDay - regimeChangeState.compositionEstablishedDay)) / 365f;
		regimeChangeState.institutionalInertia = Mathf.Clamp(num * 1.2f, 0f, 40f);
		if (regimeChangeState.IsWindowOpen)
		{
			regimeChangeState.activeJuncture.remainingDays--;
			if (regimeChangeState.activeJuncture.remainingDays <= 0)
			{
				ResolveJuncture(currentDay, realm, society, sit, factions, regimeChangeState);
			}
		}
		else
		{
			DetectAutoJuncture(currentDay, realm, society, sit, factions, regimeChangeState);
		}
	}

	private void DetectAutoJuncture(int day, RealmData realm, RealmSociety society, RealmSituation sit, FactionManager factions, RegimeChangeState state)
	{
		if (realm.treasury < -200f && state.tension.fiscalMilitary > 50f)
		{
			TryOpen(day, state, CriticalJunctureType.FiscalCollapse, 60f);
			return;
		}
		if (sit.warOnHomeSoil && realm.stability < 30f)
		{
			TryOpen(day, state, CriticalJunctureType.WarDefeat, 55f + (30f - realm.stability) * 0.5f);
			return;
		}
		ClassProfile classProfile = society.Get(society.mostRestlessClass);
		if (society.unrestScore > 65f && classProfile != null && classProfile.unrest > 25f)
		{
			TryOpen(day, state, CriticalJunctureType.PopularUprising, society.unrestScore * 0.8f);
			return;
		}
		factions.GetChangeVsStatusQuo(realm.realmId, out var changePower, out var statusQuoPower);
		if (changePower > 30f && statusQuoPower > 30f && Mathf.Abs(changePower - statusQuoPower) < 8f && state.tension.total > 55f)
		{
			TryOpen(day, state, CriticalJunctureType.EliteSplit, 50f + state.tension.total * 0.2f);
		}
	}

	public bool NotifyEvent(int currentDay, int realmId, CriticalJunctureType type, float rawSeverity)
	{
		RegimeChangeState regimeChangeState = Ensure(realmId, currentDay);
		if (regimeChangeState.IsWindowOpen)
		{
			return false;
		}
		return TryOpen(currentDay, regimeChangeState, type, rawSeverity);
	}

	private bool TryOpen(int day, RegimeChangeState state, CriticalJunctureType type, float severity)
	{
		float num = 45f + state.institutionalInertia;
		if (severity < num)
		{
			return false;
		}
		state.activeJuncture = new ActiveJuncture
		{
			type = type,
			startDay = day,
			remainingDays = 120,
			severity = severity
		};
		state.history.Add($"第{day}日 关键节点开启：{TypeName(type)}（烈度{severity:F0}）");
		_chronicle?.Add("juncture_open", TypeName(type) + "引发制度危机窗口", true, state.realmId);
		return true;
	}

	private void ResolveJuncture(int day, RealmData realm, RealmSociety society, RealmSituation sit, FactionManager factions, RegimeChangeState state)
	{
		ActiveJuncture activeJuncture = state.activeJuncture;
		IReadOnlyList<Faction> factions2 = factions.GetFactions(realm.realmId);
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		foreach (Faction item in factions2)
		{
			switch (item.stance)
			{
			case FactionStance.Reformist:
				num += item.power;
				break;
			case FactionStance.Radical:
				num2 += item.power;
				break;
			case FactionStance.Reactionary:
				num3 += item.power;
				break;
			case FactionStance.Conservative:
				num4 += item.power;
				break;
			}
		}
		float num5 = num + num2;
		bool flag = society.unrestScore > 82f && num5 < num4 * 1.2f;
		if (flag)
		{
			CriticalJunctureType type = activeJuncture.type;
			bool flag2 = (uint)(type - 4) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			Finish(state, activeJuncture, JunctureOutcomeType.Collapse, "各方失控，国家权威崩溃");
			realm.stability = Mathf.Max(0f, realm.stability - 30f);
			return;
		}
		if (num4 >= num5 && num4 >= num3)
		{
			if (num4 > num5 * 1.25f)
			{
				Finish(state, activeJuncture, JunctureOutcomeType.Reaction, "保守派反扑，旧制强化");
				realm.stability = Mathf.Clamp(realm.stability + 8f, 0f, 100f);
			}
			else
			{
				Finish(state, activeJuncture, JunctureOutcomeType.Stalemate, "僵持不下，窗口关闭而制度未变");
			}
			return;
		}
		bool flag3 = num2 > num;
		int maxChanges = ((!flag3) ? 1 : 2);
		FactionPlatform platform = SelectWinningPlatform(factions2, flag3);
		int num6 = ApplyChangesTowardPlatform(day, realm, platform, maxChanges, state);
		if (num6 == 0)
		{
			Finish(state, activeJuncture, JunctureOutcomeType.Stalemate, "变革诉求缺乏支撑革新，无从落地，窗口空转");
			return;
		}
		state.compositionEstablishedDay = day;
		JunctureOutcomeType outcome = (((num6 >= 2) | flag3) ? JunctureOutcomeType.Reform : JunctureOutcomeType.Compromise);
		Finish(state, activeJuncture, outcome, $"按胜派政纲调整 {num6} 个法律维度");
	}

	private FactionPlatform SelectWinningPlatform(IReadOnlyList<Faction> factions, bool radicalLed)
	{
		Faction faction = null;
		float num = -1f;
		foreach (Faction faction2 in factions)
		{
			bool flag;
			if (radicalLed)
			{
				flag = faction2.stance == FactionStance.Radical;
			}
			else
			{
				FactionStance stance = faction2.stance;
				bool flag2 = (uint)(stance - 1) <= 1u;
				flag = flag2;
			}
			if (flag && faction2.power > num)
			{
				num = faction2.power;
				faction = faction2;
			}
		}
		return faction?.platform ?? new FactionPlatform
		{
			openness = 0.5f,
			centralization = 0f
		};
	}

	private int ApplyChangesTowardPlatform(int day, RealmData realm, FactionPlatform platform, int maxChanges, RegimeChangeState state)
	{
		List<(PolityComponentInnovations.PolityDimension, int, float)> list = new List<(PolityComponentInnovations.PolityDimension, int, float)>();
		AddDimensionCandidate(list, realm, PolityComponentInnovations.PolityDimension.SupremeSuccession, realm.laws.supremeSuccession.primary, platform.openness, new int[3] { 2, 1, 4 }, new int[2] { 0, 5 });
		AddDimensionCandidate(list, realm, PolityComponentInnovations.PolityDimension.CentralInstitution, realm.laws.centralInstitution.primary, platform.openness, new int[2] { 2, 4 }, new int[2] { 1, 3 });
		AddDimensionCandidate(list, realm, PolityComponentInnovations.PolityDimension.LocalSuccession, realm.laws.localSuccession.primary, platform.openness, new int[3] { 2, 1, 4 }, new int[1] { 3 });
		AddDimensionCandidate(list, realm, PolityComponentInnovations.PolityDimension.SpatialStructure, realm.laws.spatialStructure.primary, platform.centralization, new int[1], new int[2] { 1, 2 });
		AddDimensionCandidate(list, realm, PolityComponentInnovations.PolityDimension.LocalScope, realm.laws.localScope.primary, platform.centralization, new int[2] { 3, 1 }, new int[1]);
		list.Sort(((PolityComponentInnovations.PolityDimension dim, int target, float gap) a, (PolityComponentInnovations.PolityDimension dim, int target, float gap) b) => b.gap.CompareTo(a.gap));
		int num = 0;
		foreach (var item in list)
		{
			if (num >= maxChanges)
			{
				break;
			}
			if (item.Item2 != GetCurrentComponent(realm, item.Item1) && PolityComponentInnovations.IsComponentAvailable(item.Item1, item.Item2, _innovations, realm.realmId) && LawReform.Reform(realm, item.Item1, item.Item2, _innovations, _chronicle, day))
			{
				num++;
			}
		}
		return num;
	}

	private void AddDimensionCandidate(List<(PolityComponentInnovations.PolityDimension, int, float)> list, RealmData realm, PolityComponentInnovations.PolityDimension dim, int current, float tendency, int[] openPositive, int[] openNegative)
	{
		if (Mathf.Abs(tendency) < 0.15f)
		{
			return;
		}
		int[] array = ((tendency > 0f) ? openPositive : openNegative);
		foreach (int num in array)
		{
			if (num != current)
			{
				list.Add((dim, num, Mathf.Abs(tendency)));
				break;
			}
		}
	}

	private static int GetCurrentComponent(RealmData realm, PolityComponentInnovations.PolityDimension d)
	{
		return d switch
		{
			PolityComponentInnovations.PolityDimension.SupremeSuccession => realm.laws.supremeSuccession.primary, 
			PolityComponentInnovations.PolityDimension.SupremeScope => realm.laws.supremeScope.primary, 
			PolityComponentInnovations.PolityDimension.CentralSuccession => realm.laws.centralSuccession.primary, 
			PolityComponentInnovations.PolityDimension.CentralInstitution => realm.laws.centralInstitution.primary, 
			PolityComponentInnovations.PolityDimension.LocalSuccession => realm.laws.localSuccession.primary, 
			PolityComponentInnovations.PolityDimension.LocalScope => realm.laws.localScope.primary, 
			PolityComponentInnovations.PolityDimension.SpatialStructure => realm.laws.spatialStructure.primary, 
			_ => -1, 
		};
	}

	private void Finish(RegimeChangeState state, ActiveJuncture j, JunctureOutcomeType outcome, string note)
	{
		j.resolved = true;
		j.outcome = outcome;
		j.note = note;
		string text = "关键节点结束：" + TypeName(j.type) + " → " + OutcomeName(outcome) + "（" + note + "）";
		state.history.Add(text);
		_chronicle?.Add("juncture_resolve", text, true, state.realmId);
		state.activeJuncture = null;
	}

	public static string TypeName(CriticalJunctureType t)
	{
		return t switch
		{
			CriticalJunctureType.SuccessionCrisis => "继承危机", 
			CriticalJunctureType.WarDefeat => "战争失败", 
			CriticalJunctureType.FiscalCollapse => "财政破产", 
			CriticalJunctureType.EliteSplit => "精英分裂", 
			CriticalJunctureType.PopularUprising => "民众起义", 
			CriticalJunctureType.ForeignConquest => "外部征服", 
			CriticalJunctureType.StrongReformer => "强势改革者", 
			_ => "关键节点", 
		};
	}

	public static string OutcomeName(JunctureOutcomeType o)
	{
		return o switch
		{
			JunctureOutcomeType.Reform => "政体改革", 
			JunctureOutcomeType.Compromise => "妥协微调", 
			JunctureOutcomeType.Reaction => "保守反扑", 
			JunctureOutcomeType.Stalemate => "停滞未变", 
			JunctureOutcomeType.Collapse => "统治崩溃", 
			_ => "无", 
		};
	}
}
}
