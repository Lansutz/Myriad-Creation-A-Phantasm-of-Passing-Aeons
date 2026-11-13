using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Events;
using MyriadCreation.Simulation.Scheduling;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.Planning;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Settlement;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.Warfare;
using MyriadCreation.World;
using MyriadCreation.World.Anchor;
using MyriadCreation.World.Biome;
using MyriadCreation.World.Climate;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Terrain;
using UnityEngine;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld : MonoBehaviour
{

	public void NotifyRulerTransition(int realmId, int newRulerCharId, bool disputed, int day = -1)
	{
		if (realms.TryGetValue(realmId, out RealmData primaryRealm))
		{
			if (string.IsNullOrEmpty(primaryRealm.primaryTitleKey))
			{
				primaryRealm.primaryTitleKey = "realm_" + realmId + "_primary";
			}
			TitleInstance primaryInstance = TitleInstanceRegistry.GetOrCreate(primaryRealm.primaryTitleKey);
			if (primaryInstance != null)
			{
				int transitionDay = day >= 0 ? day : currentDay;
				if (string.IsNullOrEmpty(primaryInstance.definitionId))
				{
					TitleDefinition boundDef = MyriadCreation.Simulation.Definitions.TitleCatalog.Highest(TitleDerivedCategory.Landed);
					if (boundDef != null)
					{
						primaryInstance.definitionId = boundDef.titleId;
					}
				}
				TitleDefinition titleDef = TitleManager.Get(primaryInstance.definitionId);
				TitleSuccessionMode mode = titleDef != null ? titleDef.successionMode : TitleSuccessionMode.RealmLaw;
				if (mode == TitleSuccessionMode.None)
				{
					primaryInstance.EndActiveHoldings(transitionDay);
				}
				else
				{
					primaryInstance.TransferSenior(newRulerCharId, transitionDay);
				}
			}
		}
		if (_regimeDynamics == null || !realms.ContainsKey(realmId))
		{
			return;
		}
		int num = ((day >= 0) ? day : currentDay);
		if (disputed)
		{
			float rawSeverity = Mathf.Clamp(50f + (100f - realms[realmId].stability) * 0.25f, 0f, 100f);
			_regimeDynamics.NotifyEvent(num, realmId, CriticalJunctureType.SuccessionCrisis, rawSeverity);
			return;
		}
		CharacterData characterData = _characterManager?.GetCharacter(newRulerCharId);
		if (characterData != null && realms.TryGetValue(realmId, out RealmData realmForTitle))
		{
			string rulerTitle = RealmTitleEvolution.GetRulerTitleDisplay(realmForTitle, null, realmForTitle.primaryCultureId);
			_chronicle?.Add("succession", characterData.fullName + " 即位为" + rulerTitle + "（" + realmForTitle.realmName + "）", true, realmId, newRulerCharId);
		}

		if (characterData != null)
		{
			float num2 = (characterData.social + characterData.management + characterData.conspiracy + characterData.prowess + characterData.military + characterData.scholarship) / 6f;
			if (num2 >= 70f && characterData.boldness >= 20f)
			{
				_regimeDynamics.NotifyEvent(num, realmId, CriticalJunctureType.StrongReformer, num2 * 0.8f);
			}
		}
	}

	private void CheckRulerSuccessions()
	{
		foreach (RealmData value in realms.Values)
		{
			// 摄政/空位期走独立状态机：此时 GetSupremeRulerId 可能返回摄政者，
			// 不能再拿 ExecuteSuccession 判断继承（否则会把活着的摄政者当成君主而永远不结算）。
			if (value.inInterregnum)
			{
				HandleInterregnum(value);
				continue;
			}
			SuccessionResult successionResult = SuccessionSystem.ExecuteSuccession(value, _characterManager, currentDay);
			if (!successionResult.triggered)
			{
				continue;
			}
			if (successionResult.deadRulerId >= 0)
			{
				EvaluateDeadRulerLife(successionResult.deadRulerId, value);
			}
			bool hasNewRuler = successionResult.succeeded && successionResult.newRulerId >= 0;
			if (hasNewRuler)
			{
				CharacterData characterData = _characterManager?.GetCharacter(successionResult.newRulerId);
				if (characterData != null && characterData.accessionDay < 0)
				{
					characterData.accessionDay = currentDay;
					if (characterData.age < 16)
					{
						characterData.achievements.youngAccession = true;
					}
					if (successionResult.disputed)
					{
						characterData.achievements.ruledUnderRegency = true;
					}
				}
				// 幼主也是已完成即位：先写入"新君即位"记录（争议记录紧随其后，两类信息都不丢）。
				string text = ((characterData != null) ? (characterData.firstName + " " + characterData.lastName) : "?");
				_chronicle?.Add("succession", value.realmName + " 新君即位：" + text, true, value.realmId);
			}
			if (successionResult.disputed)
			{
				RecordSuccessionCrisis(value, successionResult);
				// 绝嗣（无新君）→ 进入摄政/空位期兜底，避免政权永久无主。
				if (!successionResult.succeeded)
				{
					TryStartInterregnum(value);
				}
			}
			// 通知语义统一：有成功新君就传新君 id（disputed=true 时内部走继承危机窗口），
			// 无新君（绝嗣）传 -1；摄政期由 HandleInterregnum 单独推进。
			NotifyRulerTransition(value.realmId, (hasNewRuler ? successionResult.newRulerId : -1), successionResult.disputed, currentDay);
		}
	}

	/// <summary>摄政/空位期每日推进：加冕结束 / 重选摄政者 / 无合格者空转（编年史只记状态变化）。</summary>
	private void HandleInterregnum(RealmData realm)
	{
		SuccessionSystem.InterregnumTickResult tick = SuccessionSystem.TickInterregnum(realm, _characterManager, currentDay);
		if (tick.crownedRulerId >= 0)
		{
			CharacterData newRuler = _characterManager?.GetCharacter(tick.crownedRulerId);
			if (newRuler != null && newRuler.accessionDay < 0)
			{
				newRuler.accessionDay = currentDay;
				if (newRuler.age < 16)
				{
					// 幼主经历过摄政/空位期：同时记幼年即位与摄政期统治
					newRuler.achievements.youngAccession = true;
					newRuler.achievements.ruledUnderRegency = true;
				}
			}
			string name = ((newRuler != null) ? (newRuler.firstName + " " + newRuler.lastName) : "?");
			_chronicle?.Add("succession", realm.realmName + " 摄政结束，新君即位：" + name, true, realm.realmId);
			NotifyRulerTransition(realm.realmId, tick.crownedRulerId, disputed: false, currentDay);
			return;
		}
		if (tick.externalRulerResolved)
		{
			_chronicle?.Add("regency_end", realm.realmName + " 摄政期结束（法理君主/执政官已在位）", true, realm.realmId);
			return;
		}
		if (tick.regentChanged)
		{
			if (tick.regentCharacterId >= 0)
			{
				_chronicle?.Add("regency", realm.realmName + " 摄政者：" + GetCharacterName(tick.regentCharacterId), true, realm.realmId);
			}
			else
			{
				_chronicle?.Add("regency_vacant", realm.realmName + " 暂无合格摄政者（王国空转）", true, realm.realmId);
			}
		}
	}

	/// <summary>绝嗣后进入摄政/空位期，并写一条进入记录（含摄政者/空转）。</summary>
	private void TryStartInterregnum(RealmData realm)
	{
		if (realm == null || realm.inInterregnum)
		{
			return;
		}
		if (!SuccessionSystem.TryEnterInterregnum(realm, _characterManager, currentDay, out int regentId))
		{
			return;
		}
		string suffix = ((regentId >= 0) ? ("，推举 " + GetCharacterName(regentId) + " 摄政") : "，暂无合格摄政者（王国空转）");
		_chronicle?.Add("regency_enter", realm.realmName + " 绝嗣进入空位期" + suffix, true, realm.realmId, regentId);
	}

	/// <summary>
	/// 写入"继承危机"编年史（按死君主 id 去重）：绝嗣政权每天都会重新触发继承，
	/// 同一死君主只记录一次（新君主死亡后 deadRulerId 变化才会再记），避免每日刷条目。
	/// </summary>
	private void RecordSuccessionCrisis(RealmData realm, SuccessionResult result)
	{
		if (realm == null || result == null)
		{
			return;
		}
		if (result.deadRulerId >= 0 && HasRecordedSuccessionCrisis(result.deadRulerId))
		{
			return;
		}
		string description = realm.realmName + " 继承危机：" + result.reason;
		if (result.deadRulerId >= 0)
		{
			_chronicle?.Add("succession_crisis", description, true, realm.realmId, result.deadRulerId);
		}
		else
		{
			_chronicle?.Add("succession_crisis", description, true, realm.realmId);
		}
	}

	/// <summary>编年史里是否已记录过某位死君主的继承危机（participants 含死君主 id）。</summary>
	private bool HasRecordedSuccessionCrisis(int deadRulerId)
	{
		if (_chronicle == null || deadRulerId < 0)
		{
			return false;
		}
		foreach (ChronicleEntry entry in _chronicle.GetEntries())
		{
			if (entry.eventType == "succession_crisis" && entry.participants.Contains(deadRulerId))
			{
				return true;
			}
		}
		return false;
	}

	private void AddWarAchievement(int realmId, bool won)
	{
		CharacterData realmRuler = GetRealmRuler(realmId);
		if (realmRuler != null)
		{
			if (won)
			{
				realmRuler.achievements.warsWon++;
			}
			else
			{
				realmRuler.achievements.defeatedBattles++;
			}
		}
	}

	private void AddDefensiveWin(int realmId)
	{
		CharacterData realmRuler = GetRealmRuler(realmId);
		if (realmRuler != null)
		{
			realmRuler.achievements.defensiveWins++;
		}
	}

	private CharacterData GetRealmRuler(int realmId)
	{
		if (realmId < 0 || realmId >= realms.Count)
		{
			return null;
		}
		RealmData realmData = realms[realmId];
		if (realmData == null)
		{
			return null;
		}
		int supremeRulerId = realmData.GetSupremeRulerId();
		if (supremeRulerId < 0)
		{
			return null;
		}
		return _characterManager?.GetCharacter(supremeRulerId);
	}

	private void EvaluateDeadRulerLife(int charId, RealmData realm)
	{
		CharacterData characterData = _characterManager?.GetCharacter(charId);
		if (characterData != null && !characterData.epithetEvaluated)
		{
			characterData.epithetEvaluated = true;
			if (characterData.accessionDay >= 0 && characterData.deathDay >= 0)
			{
				int num = Mathf.Max(0, characterData.deathDay - characterData.accessionDay);
				characterData.achievements.reignYears = Mathf.Max(0f, (float)num / 365f);
			}
			float num2 = ((realm != null) ? GetRealmLandShare(realm.realmId) : 0f);
			characterData.achievements.regionalInfluence = Mathf.Clamp01(num2 * 3f);
			string text = EpithetSystem.EvaluateAndGrant(characterData, characterData.achievements);
			string text2 = EpithetSystem.DeterminePosthumousTitle(characterData, characterData.achievements.warsWon, characterData.achievements.conquests, characterData.achievements.famineUnderRule);
			if (!string.IsNullOrEmpty(text2))
			{
				characterData.posthumousTitle = text2;
			}
			string text3 = characterData.firstName + " " + characterData.lastName;
			string text4 = (string.IsNullOrEmpty(text) ? "" : ("「" + text + "」"));
			string text5 = (string.IsNullOrEmpty(text2) ? "" : ("（谥" + text2 + "）"));
			if (!string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(text2))
			{
				_chronicle?.Add("life_eval", text3 + text4 + text5 + " 逝世——一生盖棺定论", true, realm?.realmId ?? (-1));
			}
		}
	}

	private float GetRealmLandShare(int realmId)
	{
		int num = 0;
		int num2 = 0;
		TileData[] array = tiles;
		for (int i = 0; i < array.Length; i++)
		{
			TileData tileData = array[i];
			if (tileData.isLand)
			{
				num2++;
				if (tileData.ownerRealmId == realmId)
				{
					num++;
				}
			}
		}
		if (num2 <= 0)
		{
			return 0f;
		}
		return (float)num / (float)num2;
	}

	private int GetRealmDivisibleEstates(int realmId)
	{
		return 1;
	}

	private InheritanceLaw GetEffectiveLaw(int realmId)
	{
		return InheritanceLaw.Primogeniture();
	}

	private int _lastLawEffectsYear = int.MinValue;

	/// <summary>
	/// 每年（按 currentYear 变化）应用一次各政权法律/法令年度效果：daily 调度 + 年份 guard，
	/// daysPerTick&gt;1 / currentDay 跳跃时也不会漏年或重复；税收/人力乘子在各自结算点读取。
	/// </summary>
	private void TickLawEffectsYearly()
	{
		LawEffectRuntime.ApplyYearlyIfNewYear(realms?.Values, GetRealmSociety, currentYear, currentDay, ref _lastLawEffectsYear);
	}
}
}
