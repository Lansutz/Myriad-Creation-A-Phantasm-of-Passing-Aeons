using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Systems
{
/// <summary>
/// 官职运行（P3.1）：按 <see cref="OfficeCatalog"/> 的槽位/任期/候选规则补缺、罢免到期者、支付俸禄。
/// 只改“谁在位、谁有权”，不改产出百分比；职权由 <c>HasModuleHolder</c> 在各自系统里消费。
/// </summary>
public static class OfficeSystem
{
	public const string ModuleLawProposal = "law_proposal";
	public const string ModuleJudiciary = "judiciary";
	public const string ModuleTaxation = "taxation";
	public const string ModulePersonnel = "personnel";
	public const string ModuleMilitaryCommand = "military_command";
	public const string ModuleDiplomacy = "diplomacy";

	public static int AbsoluteDay(int year, int day)
	{
		return year * 365 + day;
	}

	public static bool HasModule(OfficeDef def, string module)
	{
		if (def?.modules == null || string.IsNullOrEmpty(module)) return false;
		for (int i = 0; i < def.modules.Length; i++)
		{
			if (string.Equals(def.modules[i], module, StringComparison.Ordinal)) return true;
		}
		return false;
	}

	/// <summary>候选资格：在世、非君主本人、符合出身/性别限制。</summary>
	public static bool IsEligible(CharacterData candidate, CharacterData ruler, OfficeDef def)
	{
		if (candidate == null || !candidate.isAlive || def == null) return false;
		if (ruler != null && candidate.characterId == ruler.characterId) return false;
		if (string.Equals(def.candidateGender, "male", StringComparison.Ordinal) && !candidate.isMale) return false;
		if (string.Equals(def.candidateGender, "female", StringComparison.Ordinal) && candidate.isMale) return false;
		bool sameFamily = ruler != null && candidate.familyId >= 0 && candidate.familyId == ruler.familyId;
		if (string.Equals(def.candidateRoyal, "royal", StringComparison.Ordinal) && !sameFamily) return false;
		if (string.Equals(def.candidateRoyal, "non_royal", StringComparison.Ordinal) && sameFamily) return false;
		return true;
	}

	/// <summary>到期/死亡者离任；返回离任人数。旧档没有任命日（0）视为终身，不误伤。</summary>
	public static int VacateExpired(RealmData realm, CharacterManager characters, int absoluteDay)
	{
		if (realm?.officeHolders == null) return 0;
		int vacated = 0;
		List<int> slots = new List<int>(realm.officeHolders.Keys);
		foreach (int slot in slots)
		{
			int holderId = realm.officeHolders[slot];
			if (holderId < 0) continue;
			CharacterData holder = characters?.GetCharacter(holderId);
			bool gone = holder == null || !holder.isAlive;
			bool expired = false;
			if (!gone && OfficeCatalog.TryGet(slot, out OfficeDef def) && def.termYears > 0
				&& realm.officeAppointedDays != null
				&& realm.officeAppointedDays.TryGetValue(slot, out int appointedDay) && appointedDay > 0
				&& absoluteDay - appointedDay >= def.termYears * 365)
			{
				expired = true;
			}
			if (!gone && !expired) continue;
			realm.officeHolders[slot] = -1;
			realm.officeAppointedDays?.Remove(slot);
			TitleInstanceRegistry.GetOrCreate("realm_" + realm.realmId + "_office_" + slot)?.EndActiveHoldings(absoluteDay);
			vacated++;
		}
		return vacated;
	}

	/// <summary>按内容表补缺；返回新任职人数。</summary>
	public static int FillVacancies(RealmData realm, CharacterManager characters, int absoluteDay)
	{
		if (realm == null || characters == null) return 0;
		if (realm.officeHolders == null) realm.officeHolders = new Dictionary<int, int>();
		if (realm.officeAppointedDays == null) realm.officeAppointedDays = new Dictionary<int, int>();
		CharacterData ruler = characters.GetCharacter(realm.GetSupremeRulerId());
		HashSet<int> seated = new HashSet<int>();
		foreach (int holderId in realm.officeHolders.Values)
		{
			if (holderId >= 0) seated.Add(holderId);
		}

		List<OfficeDef> defs = new List<OfficeDef>(OfficeCatalog.All.Values);
		defs.Sort((a, b) => a.slot.CompareTo(b.slot));
		int filled = 0;
		foreach (OfficeDef def in defs)
		{
			if (!def.enabled) continue;
			if (realm.officeHolders.TryGetValue(def.slot, out int current) && current >= 0)
			{
				continue;
			}
			CharacterData candidate = SelectCandidate(realm, characters, ruler, def, seated);
			if (candidate == null)
			{
				realm.officeHolders[def.slot] = -1;
				continue;
			}
			realm.officeHolders[def.slot] = candidate.characterId;
			realm.officeAppointedDays[def.slot] = absoluteDay;
			seated.Add(candidate.characterId);
			candidate.aiCategory = CharacterAICategory.Official;
			TitleInstance officeInstance = TitleInstanceRegistry.GetOrCreate("realm_" + realm.realmId + "_office_" + def.slot);
			if (officeInstance != null && (officeInstance.GetSeniorHolder(absoluteDay)?.characterId ?? -1) != candidate.characterId)
			{
				officeInstance.TransferSenior(candidate.characterId, absoluteDay);
			}
			filled++;
		}
		return filled;
	}

	private static CharacterData SelectCandidate(RealmData realm, CharacterManager characters, CharacterData ruler, OfficeDef def, HashSet<int> seated)
	{
		List<CharacterData> pool = characters.GetCharactersByRealm(realm.realmId);
		if (pool == null) return null;
		bool election = def.appointmentOrder != null && Array.IndexOf(def.appointmentOrder, "election") >= 0
			&& Array.IndexOf(def.appointmentOrder, "ruler") < 0;
		CharacterData best = null;
		float bestScore = float.MinValue;
		for (int i = 0; i < pool.Count; i++)
		{
			CharacterData candidate = pool[i];
			if (candidate == null || seated.Contains(candidate.characterId)) continue;
			if (!IsEligible(candidate, ruler, def)) continue;
			if (candidate.role == CharacterRole.Spouse) continue;
			float score = election
				? candidate.prestige
				: RolePriority(candidate.role);
			// 同分按 characterId 稳定决胜，保证同存档同结果。
			if (score > bestScore || (score == bestScore && best != null && candidate.characterId < best.characterId))
			{
				best = candidate;
				bestScore = score;
			}
		}
		return best;
	}

	private static float RolePriority(CharacterRole role)
	{
		switch (role)
		{
			case CharacterRole.Noble: return 4f;
			case CharacterRole.Military: return 3f;
			case CharacterRole.Scholar: return 2f;
			default: return 1f;
		}
	}

	/// <summary>按日折算支付在任官职俸禄；国库不足时本日不付（欠俸暂不触发离任）。返回实付额。</summary>
	public static float PaySalaries(RealmData realm, CharacterManager characters, float days)
	{
		if (realm?.officeHolders == null || characters == null || days <= 0f) return 0f;
		float due = 0f;
		foreach (KeyValuePair<int, int> pair in realm.officeHolders)
		{
			if (pair.Value < 0) continue;
			if (!OfficeCatalog.TryGet(pair.Key, out OfficeDef def) || def.salary <= 0f) continue;
			CharacterData holder = characters.GetCharacter(pair.Value);
			if (holder == null || !holder.isAlive) continue;
			due += def.salary * days / 365f;
		}
		if (due <= 0f) return 0f;
		return RealmTreasury.TrySpend(realm, due) ? due : 0f;
	}

	/// <summary>是否存在持有该职权且在职的官员。</summary>
	public static bool HasModuleHolder(RealmData realm, string module, CharacterManager characters, out CharacterData holder)
	{
		holder = null;
		if (realm?.officeHolders == null || characters == null) return false;
		foreach (KeyValuePair<int, int> pair in realm.officeHolders)
		{
			if (pair.Value < 0) continue;
			if (!OfficeCatalog.TryGet(pair.Key, out OfficeDef def) || !HasModule(def, module)) continue;
			CharacterData candidate = characters.GetCharacter(pair.Value);
			if (candidate != null && candidate.isAlive)
			{
				holder = candidate;
				return true;
			}
		}
		return false;
	}
}
}
