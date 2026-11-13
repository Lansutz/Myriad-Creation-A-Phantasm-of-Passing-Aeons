using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class SuccessionSystem
{
	public static SuccessionResult ExecuteSuccession(RealmData realm, CharacterManager characters, int day)
	{
		SuccessionResult successionResult = new SuccessionResult();
		if (realm == null || characters == null)
		{
			return successionResult;
		}
		int supremeRulerId = realm.GetSupremeRulerId();
		CharacterData deadRuler = ((supremeRulerId >= 0) ? characters.GetCharacter(supremeRulerId) : null);
		if (deadRuler == null || deadRuler.isAlive)
		{
			return successionResult;
		}
		successionResult.deadRulerId = deadRuler.characterId;
		successionResult.triggered = true;
		bool isMonarchy = SupremeSuccessionLevel.IsMonarchy(realm.laws);
		List<CharacterData> candidates = CollectCandidates(realm, characters, deadRuler);

		// 资格横切：最高权力资格（性别 + 阶层范围）对世袭与选举同样生效——
		// "血统合法但无资格"的候选不得进入继承线；共和路径原只做性别过滤，现在统一走这里。
		candidates = FilterByEligibility(realm.laws.supremeEligibility, candidates);

		CharacterData heir = ((!isMonarchy) ? SelectByEligibility(candidates) : InheritanceHeirSelection.DetermineHeir(realm.GetEffectiveSuccessionLaw(), candidates, deadRuler));
		if (heir == null)
		{
			// 绝嗣：明确"无继承人"语义。只清空 heirId，不清 monarchId/consulId——
			// 其它系统（GetRealmRuler/UI/外交）按 id>=0 取角色，清掉会变成空引用；
			// "无主"状态由 GameWorld 的继承危机窗口 + 编年史（按死君主 id 去重）表达，
			// 摄政/推举兜底属于后续专门系统，不在继承法内硬造君主。
			realm.heirId = -1;
			successionResult.disputed = true;
			successionResult.reason = "绝嗣";
			return successionResult;
		}
		if (heir.age < 16)
		{
			successionResult.disputed = true;
			successionResult.reason = "幼主";
		}
		if (isMonarchy)
		{
			realm.monarchId = heir.characterId;
		}
		else
		{
			realm.consulId = heir.characterId;
		}
		realm.heirId = -1;
		successionResult.succeeded = true;
		successionResult.newRulerId = heir.characterId;
		return successionResult;
	}

	/// <summary>摄政/空位期每日 tick 结果（供 GameWorld 编排编年史/通知）。</summary>
	public struct InterregnumTickResult
	{
		/// <summary>本 tick 结束后是否仍处于摄政期。</summary>
		public bool active;

		/// <summary>本次 tick 加冕的新统治者 id；-1 表示未结束摄政。</summary>
		public int crownedRulerId;

		/// <summary>摄政者是否变化（含变为 -1 空转）。</summary>
		public bool regentChanged;

		/// <summary>当前摄政者 id（-1 = 暂无合格摄政者）。</summary>
		public int regentCharacterId;

		/// <summary>法理君主/执政官已在位（外部产生了新君），摄政期被直接结束。</summary>
		public bool externalRulerResolved;
	}

	/// <summary>
	/// 绝嗣后进入摄政/空位期：设置状态并立即推举摄政者
	/// （无合格者 regentCharacterId=-1，王国空转，不硬造统治者）。
	/// 返回是否本次新进入（已处于摄政期返回 false）。
	/// </summary>
	public static bool TryEnterInterregnum(RealmData realm, CharacterManager characters, int day, out int regentCharacterId)
	{
		regentCharacterId = -1;
		if (realm == null || characters == null || realm.inInterregnum)
		{
			return false;
		}
		realm.inInterregnum = true;
		realm.interregnumStartDay = day;
		realm.interregnumMonths = 0;
		realm.regentCharacterId = SelectRegent(realm, characters);
		regentCharacterId = realm.regentCharacterId;
		return true;
	}

	/// <summary>
	/// 摄政期每日评估（GameWorld 每日调用）：
	/// 1) 法理君主/执政官已在世（外部产生新君）→ 结束摄政；
	/// 2) 出现合法继承人 → 按君主/共和写 monarchId/consulId 加冕并结束摄政；
	/// 3) 否则校验/重选摄政者；无合格者保持 -1 空转（不重复造事件）。
	/// </summary>
	public static InterregnumTickResult TickInterregnum(RealmData realm, CharacterManager characters, int day)
	{
		InterregnumTickResult result = new InterregnumTickResult { crownedRulerId = -1, regentCharacterId = -1 };
		if (realm == null || characters == null || !realm.inInterregnum)
		{
			return result;
		}
		// currentDay 每年回绕（1..365）：跨年时按回绕计算空位天数
		int elapsedDays = day - realm.interregnumStartDay;
		if (elapsedDays < 0)
		{
			elapsedDays += 365;
		}
		int elapsedMonths = Mathf.Max(0, elapsedDays / 30);
		if (elapsedMonths > realm.interregnumMonths)
		{
			realm.interregnumMonths = elapsedMonths;
		}

		int legalRulerId = realm.GetLegalRulerId();
		CharacterData legalRuler = ((legalRulerId >= 0) ? characters.GetCharacter(legalRulerId) : null);
		if (legalRuler != null && legalRuler.isAlive)
		{
			EndInterregnum(realm);
			result.externalRulerResolved = true;
			return result;
		}

		// 优先外部指定的合法继承人（heirId>=0 且在世），否则按继承法与资格重新寻找
		CharacterData heir = null;
		if (realm.heirId >= 0)
		{
			CharacterData designatedHeir = characters.GetCharacter(realm.heirId);
			if (designatedHeir != null && designatedHeir.isAlive)
			{
				heir = designatedHeir;
			}
		}
		if (heir == null)
		{
			heir = FindInterregnumHeir(realm, characters);
		}
		if (heir != null)
		{
			if (SupremeSuccessionLevel.IsMonarchy(realm.laws))
			{
				realm.monarchId = heir.characterId;
			}
			else
			{
				realm.consulId = heir.characterId;
			}
			realm.heirId = -1;
			EndInterregnum(realm);
			result.crownedRulerId = heir.characterId;
			return result;
		}

		int currentRegentId = realm.regentCharacterId;
		CharacterData currentRegent = ((currentRegentId >= 0) ? characters.GetCharacter(currentRegentId) : null);
		if (!IsValidRegent(currentRegent) || currentRegent.characterId == legalRulerId)
		{
			int newRegentId = SelectRegent(realm, characters);
			realm.regentCharacterId = newRegentId;
			result.regentChanged = newRegentId != currentRegentId;
		}
		result.active = true;
		result.regentCharacterId = realm.regentCharacterId;
		return result;
	}

	private static void EndInterregnum(RealmData realm)
	{
		realm.inInterregnum = false;
		realm.regentCharacterId = -1;
		realm.interregnumStartDay = 0;
		realm.interregnumMonths = 0;
	}

	/// <summary>摄政期寻找合法继承人（复用正常继承的候选池 + 资格过滤 + 继承法）。</summary>
	private static CharacterData FindInterregnumHeir(RealmData realm, CharacterManager characters)
	{
		int legalRulerId = realm.GetLegalRulerId();
		CharacterData deadRuler = ((legalRulerId >= 0) ? characters.GetCharacter(legalRulerId) : null);
		if (deadRuler == null)
		{
			return null;
		}
		List<CharacterData> candidates = CollectCandidates(realm, characters, deadRuler);
		candidates = FilterByEligibility(realm.laws.supremeEligibility, candidates);
		if (candidates == null || candidates.Count == 0)
		{
			return null;
		}
		if (SupremeSuccessionLevel.IsMonarchy(realm.laws))
		{
			return InheritanceHeirSelection.DetermineHeir(realm.GetEffectiveSuccessionLaw(), candidates, deadRuler);
		}
		return SelectByEligibility(candidates);
	}

	private static bool IsValidRegent(CharacterData character)
	{
		return character != null && character.isAlive && character.age >= 16;
	}

	/// <summary>
	/// 推举摄政者：优先死君主同家族成年成员，其次政权行政长官（adminDivision holder / officeHolders），
	/// 最后同政权高威望成年角色；都无 → -1（王国空转）。评分 = 威望*10 + 年龄。
	/// </summary>
	private static int SelectRegent(RealmData realm, CharacterManager characters)
	{
		if (realm == null || characters == null)
		{
			return -1;
		}
		int legalRulerId = realm.GetLegalRulerId();

		// 1) 同家族成年成员（排除死君主本人）
		CharacterData deadRuler = ((legalRulerId >= 0) ? characters.GetCharacter(legalRulerId) : null);
		if (deadRuler != null && deadRuler.familyId >= 0)
		{
			FamilyNode family = characters.GetFamily(deadRuler.familyId);
			if (family != null)
			{
				int bestId = -1;
				float bestScore = float.MinValue;
				foreach (int memberId in family.memberIds)
				{
					if (memberId == legalRulerId)
					{
						continue;
					}
					CharacterData member = characters.GetCharacter(memberId);
					if (!IsValidRegent(member))
					{
						continue;
					}
					float score = member.prestige * 10f + member.age;
					if (score > bestScore)
					{
						bestScore = score;
						bestId = memberId;
					}
				}
				if (bestId >= 0)
				{
					return bestId;
				}
			}
		}

		// 2) 政权行政长官：行政区持有者 + 官职持有者
		int bestAdministrativeId = -1;
		float bestAdministrativeScore = float.MinValue;
		if (realm.adminDivisions != null)
		{
			foreach (AdminDivision division in realm.adminDivisions)
			{
				if (division == null || division.holderCharacterId < 0)
				{
					continue;
				}
				CharacterData holder = characters.GetCharacter(division.holderCharacterId);
				if (!IsValidRegent(holder) || holder.characterId == legalRulerId)
				{
					continue;
				}
				float score = holder.prestige * 10f + holder.age;
				if (score > bestAdministrativeScore)
				{
					bestAdministrativeScore = score;
					bestAdministrativeId = holder.characterId;
				}
			}
		}
		if (realm.officeHolders != null)
		{
			foreach (KeyValuePair<int, int> office in realm.officeHolders)
			{
				if (office.Value < 0)
				{
					continue;
				}
				CharacterData holder = characters.GetCharacter(office.Value);
				if (!IsValidRegent(holder) || holder.characterId == legalRulerId)
				{
					continue;
				}
				float score = holder.prestige * 10f + holder.age;
				if (score > bestAdministrativeScore)
				{
					bestAdministrativeScore = score;
					bestAdministrativeId = holder.characterId;
				}
			}
		}
		if (bestAdministrativeId >= 0)
		{
			return bestAdministrativeId;
		}

		// 3) 同政权高威望成年角色
		int bestRealmId = -1;
		float bestRealmScore = float.MinValue;
		foreach (CharacterData candidate in characters.GetCharactersByRealm(realm.realmId))
		{
			if (!IsValidRegent(candidate) || candidate.characterId == legalRulerId)
			{
				continue;
			}
			float score = candidate.prestige * 10f + candidate.age;
			if (score > bestRealmScore)
			{
				bestRealmScore = score;
				bestRealmId = candidate.characterId;
			}
		}
		return bestRealmId;
	}

	private static List<CharacterData> CollectCandidates(RealmData realm, CharacterManager characters, CharacterData ruler)
	{
		List<CharacterData> list = new List<CharacterData>();
		if (ruler.familyId < 0)
		{
			return list;
		}
		FamilyNode family = characters.GetFamily(ruler.familyId);
		if (family == null)
		{
			return list;
		}
		foreach (int memberId in family.memberIds)
		{
			CharacterData character = characters.GetCharacter(memberId);
			if (character != null && character.isAlive && character.characterId != ruler.characterId)
			{
				list.Add(character);
			}
		}
		return list;
	}

	/// <summary>
	/// 按最高权力资格过滤候选：先性别（EligibilityRules.IsGenderEligible），再阶层范围（IsScopeEligible）。
	/// ClanOnly 的"同族"语义已由候选池（家族成员）约束，这里不重复判断。
	/// </summary>
	private static List<CharacterData> FilterByEligibility(EligibilityRules rules, List<CharacterData> candidates)
	{
		if (candidates == null || candidates.Count == 0)
		{
			return new List<CharacterData>();
		}
		if (rules == null)
		{
			return new List<CharacterData>(candidates);
		}
		List<CharacterData> list = new List<CharacterData>(candidates);
		list.RemoveAll((CharacterData c) => !rules.IsGenderEligible(c.isMale));
		if (rules.scope != EligibilityScope.ClanOnly)
		{
			list.RemoveAll((CharacterData c) => !rules.IsScopeEligible(c.socialClass));
		}
		return list;
	}

	/// <summary>共和路径：在已按资格过滤的候选里按威望（prestige）降序取第一。</summary>
	private static CharacterData SelectByEligibility(List<CharacterData> candidates)
	{
		if (candidates == null || candidates.Count == 0)
		{
			return null;
		}
		List<CharacterData> list = new List<CharacterData>(candidates);
		list.Sort((CharacterData a, CharacterData b) => b.prestige.CompareTo(a.prestige));
		return list[0];
	}

	public static bool IsInInheritanceLine(RealmData realm, CharacterManager characters, InheritanceLaw law, int characterId, int divisibleEstates)
	{
		if (realm == null || characters == null || law == null)
		{
			return false;
		}
		int supremeRulerId = realm.GetSupremeRulerId();
		CharacterData currentRuler = ((supremeRulerId >= 0) ? characters.GetCharacter(supremeRulerId) : null);
		List<CharacterData> charactersByRealm = characters.GetCharactersByRealm(realm.realmId);
		List<CharacterData> heirOrderedPool = InheritanceHeirSelection.GetHeirOrderedPool(law, charactersByRealm, currentRuler);
		// 资格不满足的候选不占继承线位（与 ExecuteSuccession 使用同一套资格横切规则）。
		heirOrderedPool = FilterByEligibility(realm.laws.supremeEligibility, heirOrderedPool);
		if (heirOrderedPool == null || heirOrderedPool.Count == 0)
		{
			return false;
		}
		int num = heirOrderedPool.FindIndex((CharacterData c) => c.characterId == characterId);
		if (num < 0)
		{
			return false;
		}
		int num2 = Mathf.Min(heirOrderedPool.Count, divisibleEstates);
		return num < num2;
	}

	public static List<int> GetGreatHolyWarBeneficiaryCandidates(RealmData realm, CharacterManager characters, InheritanceLaw law, int divisibleEstates)
	{
		List<int> list = new List<int>();
		if (realm == null || characters == null || law == null)
		{
			return list;
		}
		foreach (CharacterData item in characters.GetCharactersByRealm(realm.realmId))
		{
			if (item != null && !IsInInheritanceLine(realm, characters, law, item.characterId, divisibleEstates))
			{
				list.Add(item.characterId);
			}
		}
		return list;
	}
}
}
