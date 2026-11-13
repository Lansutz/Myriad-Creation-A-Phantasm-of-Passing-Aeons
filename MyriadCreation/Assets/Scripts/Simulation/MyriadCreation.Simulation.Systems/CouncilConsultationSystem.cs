using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.Systems
{
public enum WarDecisionMode
{
	/// <summary>先协商；多数反对就作罢。</summary>
	ConsultAndAbide,
	/// <summary>先协商；多数反对仍强推，代价落在廷臣关系与政权稳定上。</summary>
	ConsultAndOverride,
	/// <summary>不协商直接做；没有议事背书，同样有政治代价。</summary>
	SkipConsultation
}

/// <summary>一次战争议事的结果：谁支持、谁反对、谁弃权。不做抽象点数，只记录人的立场。</summary>
public struct CouncilConsultationResult
{
	public bool hasCouncil;
	public int members;
	public int supporters;
	public int opponents;
	public int abstainers;
	public float approval;
	public List<int> memberIds;
	public List<int> supporterIds;
	public List<int> opponentIds;

	public bool MajoritySupport => supporters > opponents;
}

/// <summary>
/// 战争议事：宣战/改目标前问一句"朝堂怎么看"。
/// 立场来自人物性格（胆量/贪婪/同情/荣誉）、对统治者的好感、目标激进程度与政权现状；
/// 协商通过不额外收费，绕过议事才产生关系与稳定度代价。
/// </summary>
public static class CouncilConsultationSystem
{
	public static CouncilConsultationResult Consult(GameWorld world, int realmId, WarGoal goal)
	{
		CouncilConsultationResult result = new CouncilConsultationResult
		{
			memberIds = new List<int>(),
			supporterIds = new List<int>(),
			opponentIds = new List<int>()
		};
		if (world == null || world.realms == null || !world.realms.TryGetValue(realmId, out RealmData realm) || realm == null)
		{
			return result;
		}
		List<CharacterData> members = GetCouncilMembers(world, realm);
		result.members = members.Count;
		result.hasCouncil = members.Count > 0;
		if (members.Count == 0)
		{
			return result;
		}
		CharacterData ruler = world.GetCharacterManager()?.GetCharacter(realm.GetSupremeRulerId());
		foreach (CharacterData member in members)
		{
			result.memberIds.Add(member.characterId);
			float score = EvaluateStance(member, ruler, realm, goal);
			if (score > 0.1f)
			{
				result.supporters++;
				result.supporterIds.Add(member.characterId);
			}
			else if (score < -0.1f)
			{
				result.opponents++;
				result.opponentIds.Add(member.characterId);
			}
			else
			{
				result.abstainers++;
			}
		}
		result.approval = (float)(result.supporters - result.opponents) / result.members;
		return result;
	}

	/// <summary>议事成员：优先在职官员；早期没有官职时由同政权有身份的人物充任，避免"无人可问"。</summary>
	private static List<CharacterData> GetCouncilMembers(GameWorld world, RealmData realm)
	{
		List<CharacterData> result = new List<CharacterData>();
		HashSet<int> seen = new HashSet<int>();
		int rulerId = realm.GetSupremeRulerId();
		if (realm.officeHolders != null)
		{
			foreach (int holderId in realm.officeHolders.Values)
			{
				CharacterData holder = world.GetCharacterManager()?.GetCharacter(holderId);
				if (holder != null && holder.isAlive && holder.characterId != rulerId && seen.Add(holder.characterId))
				{
					result.Add(holder);
				}
			}
		}
		if (result.Count == 0)
		{
			foreach (CharacterData character in world.GetCharacterManager().GetCharactersByRealm(realm.realmId))
			{
				if (character == null || !character.isAlive || character.characterId == rulerId || character.role == CharacterRole.Spouse || !seen.Add(character.characterId))
				{
					continue;
				}
				result.Add(character);
				if (result.Count >= 6)
				{
					break;
				}
			}
		}
		return result;
	}

	private static float EvaluateStance(CharacterData member, CharacterData ruler, RealmData realm, WarGoal goal)
	{
		float aggression = Aggressiveness(goal);
		float boldness = member.boldness / 100f;
		float greed = member.greed / 100f;
		float compassion = member.compassion / 100f;
		float honor = member.honor / 100f;
		float opinion = 0f;
		if (ruler != null && member.relations != null && member.relations.TryGetValue(ruler.characterId, out CharacterRelation relation))
		{
			opinion = relation.opinion / 200f;
		}
		float score = aggression * (boldness * 0.8f + greed * 0.4f - compassion * 0.7f - honor * 0.3f) + opinion * 0.6f;
		if (aggression >= 0.5f && (realm.stability < 40f || realm.unrest > 60f))
		{
			score -= 0.25f;
		}
		return score;
	}

	private static float Aggressiveness(WarGoal goal)
	{
		if (goal == null)
		{
			return 0.2f;
		}
		return goal.type switch
		{
			GameEnums.WarGoalType.Annihilation => 1f,
			GameEnums.WarGoalType.ConquerRegion => 0.85f,
			GameEnums.WarGoalType.Vassalization => 0.85f,
			GameEnums.WarGoalType.ConvertReligion => 0.7f,
			GameEnums.WarGoalType.ConquerTerritory => 0.6f,
			GameEnums.WarGoalType.Independence => 0.5f,
			GameEnums.WarGoalType.BorderAdjustment => 0.35f,
			GameEnums.WarGoalType.Humiliation => 0.3f,
			GameEnums.WarGoalType.Indemnity => 0.2f,
			_ => 0.3f
		};
	}
}
}
