using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.FaithHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld
{
	/// <summary>
	/// P1.3 教职接线：宗教领袖的资格与更替。
	/// 君权神授 → 君主兼任；其余信仰 → 按教职性别/独身档从学者、贵族中按虔信选。
	/// 每月校验一次，只有死亡或违规才换人，不做每日全量扫描。
	/// </summary>
	private void EnsureFaithLeadership()
	{
		if (_faithSystems == null || _faithSystems.Count == 0 || _characterManager == null)
		{
			return;
		}
		for (int i = 0; i < _faithSystems.Count; i++)
		{
			FaithSystem faith = _faithSystems[i];
			if (faith == null || faith.religionId < 0)
			{
				continue;
			}
			DoctrineBindings bindings = ResolveFaithDoctrine(faith);
			int currentId = faith.highPriestCharacterId;
			CharacterData current = currentId >= 0 ? _characterManager.GetCharacter(currentId) : null;
			bool valid = current != null && current.isAlive;
			if (valid)
			{
				valid = DoctrineIdentityRules.CanHoldReligiousOffice(bindings, current.isMale);
			}
			if (valid && DoctrineIdentityRules.IsSacredKingship(bindings))
			{
				valid = IsMonarchOfFaith(currentId, faith.faithId);
			}
			if (valid)
			{
				continue;
			}
			int replacement = SelectHighPriest(faith, bindings);
			if (replacement < 0 || replacement == currentId)
			{
				continue;
			}
			faith.highPriestCharacterId = replacement;
			CharacterData chosen = _characterManager.GetCharacter(replacement);
			_chronicle?.Add("religion", (chosen != null ? chosen.fullName : "某人") + " 出任" + faith.faithName + "的宗教领袖", true);
		}
	}

	/// <summary>
	/// 国教确立：政权尚无国教时，按核心领土人口的信仰多数（≥40%）选一个；
	/// 该信仰所属 religion 必须已满足革新前置，否则继续维持无国教状态。
	/// </summary>
	public bool TryAdoptMajorityFaithForRealm(int realmId)
	{
		if (realms == null || !realms.TryGetValue(realmId, out RealmData realm) || realm == null) return false;
		if (realm.stateReligionId >= 0 || tiles == null) return false;
		List<int> core = new List<int>(realm.coreTiles);
		if (core.Count == 0) return false;

		Dictionary<int, float> tally = new Dictionary<int, float>();
		float total = 0f;
		foreach (int tileIndex in core)
		{
			if (tileIndex < 0 || tileIndex >= tiles.Length) continue;
			if (tiles[tileIndex].populationBlocks == null) continue;
			foreach (PopulationBlock block in tiles[tileIndex].populationBlocks)
			{
				if (block.count <= 0f || block.faithId < 0) continue;
				tally.TryGetValue(block.faithId, out float count);
				tally[block.faithId] = count + block.count;
				total += block.count;
			}
		}
		if (total <= 0f) return false;

		int bestFaith = -1;
		float bestCount = 0f;
		foreach (KeyValuePair<int, float> pair in tally)
		{
			if (pair.Value > bestCount || (pair.Value == bestCount && pair.Key < bestFaith))
			{
				bestCount = pair.Value;
				bestFaith = pair.Key;
			}
		}
		if (bestFaith < 0 || bestCount / total < 0.4f) return false;

		FaithSystem faith = GetFaithSystem(bestFaith);
		if (faith == null) return false;
		if (faith.religionId >= 0 && !ReligionCatalog.IsAvailable(faith.religionId,
			id => _innovationTree != null && _innovationTree.HasInnovation(realmId, id)))
		{
			return false;
		}
		if (!FaithCommand.SetStateReligion(realmId, bestFaith, out _)) return false;
		_chronicle?.Add("religion", realm.realmName + " 以" + faith.faithName + "为国教", true, realmId);
		return true;
	}

	private void AdoptMajorityFaithDaily()
	{
		if (realms == null) return;
		foreach (RealmData realm in realms.Values)
		{
			if (realm == null || realm.stateReligionId >= 0) continue;
			TryAdoptMajorityFaithForRealm(realm.realmId);
		}
	}

	private static DoctrineBindings ResolveFaithDoctrine(FaithSystem faith)
	{
		if (faith != null && ContentResolvers.Religions.TryGet(faith.religionId, out ReligionDef religion))
		{
			return DoctrineBindingHost.Resolve(religion);
		}
		return new DoctrineBindings();
	}

	/// <summary>大祭司是否为该信仰所覆盖政权的君主（君权神授的结构性判据）。</summary>
	private bool IsMonarchOfFaith(int characterId, int faithId)
	{
		foreach (RealmData realm in realms.Values)
		{
			if (realm != null && realm.stateReligionId == faithId && realm.GetSupremeRulerId() == characterId)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// 遴选顺序：君权神授先取君主；否则在信奉该信仰的政权里取虔信最高者，
	/// 学者优先、贵族次之；独身档跳过已婚者。无合格候选人时回退到君主。
	/// </summary>
	private int SelectHighPriest(FaithSystem faith, DoctrineBindings bindings)
	{
		int fallback = -1;
		int best = -1;
		float bestScore = float.MinValue;
		foreach (RealmData realm in realms.Values)
		{
			if (realm == null || realm.stateReligionId != faith.faithId)
			{
				continue;
			}
			int rulerId = realm.GetSupremeRulerId();
			CharacterData ruler = rulerId >= 0 ? _characterManager.GetCharacter(rulerId) : null;
			if (ruler != null && ruler.isAlive)
			{
				if (fallback < 0)
				{
					fallback = rulerId;
				}
				if (DoctrineIdentityRules.IsSacredKingship(bindings) && DoctrineIdentityRules.CanHoldReligiousOffice(bindings, ruler.isMale))
				{
					return rulerId;
				}
			}
			List<CharacterData> candidates = _characterManager.GetCharactersByRealm(realm.realmId);
			for (int i = 0; i < candidates.Count; i++)
			{
				CharacterData candidate = candidates[i];
				if (candidate == null || !candidate.isAlive || candidate.characterId == rulerId || candidate.age < 16)
				{
					continue;
				}
				if (!DoctrineIdentityRules.CanHoldReligiousOffice(bindings, candidate.isMale))
				{
					continue;
				}
				if (DoctrineIdentityRules.RequiresCelibacy(bindings) && candidate.spouseId >= 0)
				{
					continue;
				}
				float score = candidate.piety;
				if (candidate.role == CharacterRole.Scholar)
				{
					score += 30f;
				}
				else if (candidate.role == CharacterRole.Noble)
				{
					score += 10f;
				}
				if (candidate.age >= 30)
				{
					score += 10f;
				}
				if (score > bestScore)
				{
					bestScore = score;
					best = candidate.characterId;
				}
			}
		}
		return best >= 0 ? best : fallback;
	}

	/// <summary>该人物是否为某信仰的在任宗教领袖，且该信仰要求独身。</summary>
	private bool IsCelibateReligiousLeader(int characterId)
	{
		if (_faithSystems == null || characterId < 0)
		{
			return false;
		}
		for (int i = 0; i < _faithSystems.Count; i++)
		{
			FaithSystem faith = _faithSystems[i];
			if (faith != null && faith.highPriestCharacterId == characterId)
			{
				return DoctrineIdentityRules.RequiresCelibacy(ResolveFaithDoctrine(faith));
			}
		}
		return false;
	}
}
}
