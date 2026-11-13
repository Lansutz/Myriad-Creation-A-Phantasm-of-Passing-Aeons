using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Simulation.LawHost
{
/// <summary>一次法律效果结算的结果（年度增量已按 days/年长 折算，供测试/UI/编年史使用）。</summary>
public struct LawEffectTickResult
{
	public LawEffect effect;

	public float stabilityDelta;

	public float legitimacyDelta;

	public float unrestDelta;
}

/// <summary>
/// 法律/法令效果消费层（SumLawEffects / SumDecreeEffects 的运行时调用方，兼容层）：
/// - 乘子：<see cref="GetTaxMultiplier"/> 由每日征收结算消费（PoliticsSimulationSystem → EconomyManager.CollectTaxes）；
///         <see cref="GetManpowerMultiplier"/> 由人力入口消费（ManpowerSystem.GetRealmTotalManpower 重载 + Army.ReinforceUnit 参数）；
/// - 年度效果：<see cref="ApplyYearlyIfNewYear"/> 由 GameWorld "laws.effects.yearly" 每日 guard 调度消费，
///   stability → realm.stability，legitimacy → realm.lawState.legitimacy，
///   unrest → realm.unrest（RealmData 持久字段，随 RealmDTO 入档；若传入 RealmSociety 则同时同步其瞬时 unrestScore）。
/// 空政权 / 空目录 / 空法令一律返回中性值（乘子 1、增量 0），不抛异常。
/// catalog 默认来自 DefaultLawCatalog；mod/自定义目录可通过 <see cref="SetCatalogs"/> 注入，<see cref="ResetCatalogs"/> 还原。
/// </summary>
public static class LawEffectRuntime
{
	private static IReadOnlyDictionary<string, LawDef> _lawById;

	private static IReadOnlyDictionary<string, DecreeDef> _decreeById;

	/// <summary>当前生效的法律目录（只读；默认目录或 SetCatalogs 注入的目录）。</summary>
	public static IReadOnlyDictionary<string, LawDef> CurrentLaws => LawIndex();

	/// <summary>当前生效的法令目录（只读）。</summary>
	public static IReadOnlyDictionary<string, DecreeDef> CurrentDecrees => DecreeIndex();

	/// <summary>
	/// 切换效果结算使用的 catalog（mod 自定义法律/法令目录扩展点）；传入 null 表示该目录回退默认。
	/// 只影响 LawEffectRuntime 的乘子/年度结算，不改 RealmLawState 中已存的 lawId。
	/// </summary>
	public static void SetCatalogs(IEnumerable<LawGroupDef> groups, IEnumerable<DecreeDef> decrees)
	{
		_lawById = ((groups != null) ? DefaultLawCatalog.IndexLaws(groups) : null);
		_decreeById = ((decrees != null) ? DefaultLawCatalog.IndexDecrees(decrees) : null);
	}

	/// <summary>恢复默认 catalog 缓存。</summary>
	public static void ResetCatalogs()
	{
		_lawById = null;
		_decreeById = null;
	}

	/// <summary>合并当前生效法律与生效法令的效果（乘法乘子相乘、加法项相加）。</summary>
	public static LawEffect GetCombinedEffect(RealmData realm, int currentDay)
	{
		LawEffect first = ((realm != null) ? LawAndDecreeSystem.SumLawEffects(realm.lawState, LawIndex()) : null);
		LawEffect second = ((realm != null) ? LawAndDecreeSystem.SumDecreeEffects(realm.decreeState, currentDay, DecreeIndex()) : null);
		return Combine(first, second);
	}

	/// <summary>税收乘子（无生效法律/法令时为 1）：由每日税收结算点使用。</summary>
	public static float GetTaxMultiplier(RealmData realm, int currentDay)
	{
		return SanitizeMultiplier(GetCombinedEffect(realm, currentDay).taxMultiplier);
	}

	/// <summary>人力/补员乘子（无生效法律/法令时为 1）：由人力入口/补员计算点使用。</summary>
	public static float GetManpowerMultiplier(RealmData realm, int currentDay)
	{
		return SanitizeMultiplier(GetCombinedEffect(realm, currentDay).manpowerMultiplier);
	}

	/// <summary>
	/// 按 days 天折算并应用年度效果（stability/legitimacy/unrest）。
	/// 每年传默认年长（ApplyYearlyEffects）；周期结算传实际天数。
	/// </summary>
	public static LawEffectTickResult ApplyPeriodicEffects(RealmData realm, RealmSociety society, int currentDay, int days)
	{
		LawEffectTickResult result = default(LawEffectTickResult);
		if (realm == null || days <= 0)
		{
			return result;
		}
		LawEffect effect = GetCombinedEffect(realm, currentDay);
		result.effect = effect;
		float num = (float)days / 365f;
		result.stabilityDelta = effect.stabilityPerYear * num;
		result.legitimacyDelta = effect.legitimacyPerYear * num;
		result.unrestDelta = effect.unrestPerYear * num;
		RealmStability.Adjust(realm, result.stabilityDelta);
		realm.unrest = Mathf.Clamp(realm.unrest + result.unrestDelta, 0f, 100f);
		realm.lawState ??= new RealmLawState();
		realm.lawState.legitimacy = Mathf.Clamp(realm.lawState.legitimacy + result.legitimacyDelta, 0f, 100f);
		if (society != null)
		{
			// RealmSociety.unrestScore 每次政治日结由 SocietyManager 重建（瞬时值）；
			// 持久语义在 realm.unrest，这里同步一份给本 tick 下游使用。
			society.unrestScore = Mathf.Clamp(society.unrestScore + result.unrestDelta, 0f, 100f);
		}
		return result;
	}

	/// <summary>应用一整年的法律/法令效果（测试/年度结算用）。</summary>
	public static LawEffectTickResult ApplyYearlyEffects(RealmData realm, RealmSociety society, int currentDay)
	{
		return ApplyPeriodicEffects(realm, society, currentDay, MyriadCreation.Core.Data.GameCalendar.DefaultDaysPerYear);
	}

	/// <summary>
	/// 年份变化 guard：每个 world-year 只结算一次年度效果（daysPerTick&gt;1 时 currentDay 可能跳过
	/// 年长的整数倍，用 currentYear 变化判断可保证不漏不重）。返回本次实际结算的政权数。
	/// </summary>
	public static int ApplyYearlyIfNewYear(IEnumerable<RealmData> realms, Func<int, RealmSociety> societyProvider, int currentYear, int currentDay, ref int lastAppliedYear)
	{
		if (lastAppliedYear == currentYear)
		{
			return 0;
		}
		lastAppliedYear = currentYear;
		if (realms == null)
		{
			return 0;
		}
		int num = 0;
		foreach (RealmData realm in realms)
		{
			if (realm != null)
			{
				ApplyYearlyEffects(realm, societyProvider?.Invoke(realm.realmId), currentDay);
				num++;
			}
		}
		return num;
	}

	private static LawEffect Combine(LawEffect first, LawEffect second)
	{
		LawEffect lawEffect = new LawEffect();
		if (first != null)
		{
			lawEffect.taxMultiplier *= first.taxMultiplier;
			lawEffect.stabilityPerYear += first.stabilityPerYear;
			lawEffect.legitimacyPerYear += first.legitimacyPerYear;
			lawEffect.manpowerMultiplier *= first.manpowerMultiplier;
			lawEffect.unrestPerYear += first.unrestPerYear;
		}
		if (second != null)
		{
			lawEffect.taxMultiplier *= second.taxMultiplier;
			lawEffect.stabilityPerYear += second.stabilityPerYear;
			lawEffect.legitimacyPerYear += second.legitimacyPerYear;
			lawEffect.manpowerMultiplier *= second.manpowerMultiplier;
			lawEffect.unrestPerYear += second.unrestPerYear;
		}
		return lawEffect;
	}

	private static float SanitizeMultiplier(float value)
	{
		if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
		{
			return 1f;
		}
		return value;
	}

	private static IReadOnlyDictionary<string, LawDef> LawIndex()
	{
		if (_lawById == null)
		{
			_lawById = DefaultLawCatalog.IndexLaws(DefaultLawCatalog.CreateGroups());
		}
		return _lawById;
	}

	private static IReadOnlyDictionary<string, DecreeDef> DecreeIndex()
	{
		if (_decreeById == null)
		{
			_decreeById = DefaultLawCatalog.IndexDecrees(DefaultLawCatalog.CreateDecrees());
		}
		return _decreeById;
	}
}
}
