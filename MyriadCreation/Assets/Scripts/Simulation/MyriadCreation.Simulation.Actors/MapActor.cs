using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyriadCreation.Simulation.Actors
{
public class MapActor
{
	public int actorId;

	public MapActorType type;

	public string actorName = "";

	public UnitCategory category;

	public UnitSubcategory subcategory;

	public int currentTile;

	public int targetTile = -1;

	public int currentSettlementAnchorId = -1;

	public int targetSettlementAnchorId = -1;

	public int leaderCharacterId = -1;

	public CharacterAICategory leaderAICategory;

	public float movementProgress;

	public float moveSpeed = 1f;

	[NonSerialized]
	public List<int> movePath = new List<int>();

	[NonSerialized]
	private int _pathGoalTile = -1;

	[NonSerialized]
	private int _pathRetryDay = -1;

	public int population;

	public float health = 100f;

	public float morale = 80f;

	public float supplies = 100f;

	public float lowHealthDays;

	private float _casualtyPulse;

	public int ownerRealmId = -1;

	public int factionId = -1;

	public bool isHostile;

	public Dictionary<string, float> attributes = new Dictionary<string, float>();

	[NonSerialized]
	public TransportUnit transport;

	private IMapActorAI _ai;

	private CharacterAICategory _boundLeaderCategory;

	private bool _boundToLeader;

	private int _boundTargetTile = -1;

	public IMapActorAI AI
	{
		get
		{
			return _ai;
		}
		set
		{
			_ai = value;
		}
	}

	public bool IsInsideSettlement => currentSettlementAnchorId >= 0;

	public bool IsAlive
	{
		get
		{
			if (health > 0f)
			{
				return population > 0;
			}
			return false;
		}
	}

	public virtual void Tick(GameWorld world, float deltaDays)
	{
		CharacterData leader = ResolveLeader(world);
		SyncLeaderAI(leader);
		if (targetTile >= 0 && targetTile != currentTile)
		{
			EnsureMovePath(world);
			if (movePath != null && movePath.Count > 0)
			{
				float num = ((transport != null) ? transport.MoveSpeedMultiplier(TerrainConditions.FromTile(((Object)(object)world != (Object)null && world.tiles != null && currentTile >= 0 && currentTile < world.tiles.Length) ? world.tiles[currentTile] : default(TileData))) : 1f);
				movementProgress += moveSpeed * deltaDays * GetTerrainMoveModifier(world) * num * LeaderMarchMultiplier(leader);
				if (movementProgress >= 1f)
				{
					movementProgress = 0f;
					currentTile = movePath[0];
					movePath.RemoveAt(0);
					UpdateMemberLocations(world);
					if (currentTile == targetTile)
					{
						if (targetSettlementAnchorId >= 0)
						{
							currentSettlementAnchorId = targetSettlementAnchorId;
							targetSettlementAnchorId = -1;
						}
						else
						{
							currentSettlementAnchorId = -1;
						}
						OnArriveTile(world);
					}
				}
			}
		}
		_ai?.Think(world, this);
		_ai?.Act(world, this);
		supplies -= deltaDays * 0.5f;
		if (transport != null)
		{
			float num2 = transport.ConsumeFood(world?.goodsDefs, deltaDays, out var _);
			if (num2 > 0f)
			{
				health -= num2 * 1.5f;
				morale -= num2 * 0.8f;
			}
		}
		if (supplies <= 0f)
		{
			health -= deltaDays * 2f;
			morale -= deltaDays * 1f;
		}
		if (health < 30f)
		{
			lowHealthDays += deltaDays;
			_casualtyPulse += deltaDays;
			if (_casualtyPulse >= 2f)
			{
				_casualtyPulse = 0f;
				int num3 = Mathf.Max(1, Mathf.CeilToInt((float)population * 0.05f));
				population = Mathf.Max(0, population - num3);
				morale = Mathf.Max(0f, morale - 2f);
			}
		}
		else
		{
			lowHealthDays = 0f;
			_casualtyPulse = 0f;
		}
		TickNamedMembers(world, deltaDays);
		if (health <= 0f || population <= 0)
		{
			OnDeath(world);
		}
	}

	protected virtual float GetTerrainMoveModifier(GameWorld world)
	{
		if ((Object)(object)world == (Object)null || currentTile < 0 || currentTile >= world.tiles.Length)
		{
			return 1f;
		}
		float num = world.tiles[currentTile].movementCost;
		if (num <= 0f)
		{
			num = 1f;
		}
		return Mathf.Clamp(1f / num, 0.02f, 1f);
	}

	protected virtual void OnArriveTile(GameWorld world)
	{
	}

	protected virtual void OnDeath(GameWorld world)
	{
		Debug.Log((object)$"[MapActor] {actorName}({type}) 已消亡，地块={currentTile}");
	}

	private void EnsureMovePath(GameWorld world)
	{
		if ((Object)(object)world == (Object)null)
		{
			return;
		}
		if (movePath == null)
		{
			movePath = new List<int>();
		}
		if ((movePath.Count <= 0 || _pathGoalTile != targetTile) && (_pathGoalTile != targetTile || world.currentDay >= _pathRetryDay))
		{
			List<int> list = UnitPathfinder.FindPath(world, currentTile, targetTile);
			movePath.Clear();
			if (list != null)
			{
				movePath.AddRange(list);
			}
			_pathGoalTile = targetTile;
			_pathRetryDay = ((movePath.Count == 0) ? (world.currentDay + 5) : (-1));
		}
	}

	public void SetTarget(int tileIndex)
	{
		bool num = targetTile != tileIndex || targetSettlementAnchorId >= 0;
		targetTile = tileIndex;
		targetSettlementAnchorId = -1;
		if (num)
		{
			movePath?.Clear();
			_pathGoalTile = -1;
			_pathRetryDay = -1;
			movementProgress = 0f;
		}
	}

	public void SetTargetSettlement(int anchorId, int tileIndex)
	{
		bool num = targetSettlementAnchorId != anchorId || targetTile != tileIndex;
		targetSettlementAnchorId = anchorId;
		targetTile = tileIndex;
		if (anchorId != currentSettlementAnchorId)
		{
			currentSettlementAnchorId = -1;
		}
		if (num)
		{
			movePath?.Clear();
			_pathGoalTile = -1;
			_pathRetryDay = -1;
			movementProgress = 0f;
		}
	}

	public bool SetLeader(int characterId)
	{
		if (transport == null || !transport.TrySetLeader(characterId))
		{
			return false;
		}
		leaderCharacterId = characterId;
		leaderAICategory = CharacterAICategory.None;
		return true;
	}

	public bool SetLeader(CharacterData leader)
	{
		if (leader == null || !SetLeader(leader.characterId))
		{
			return false;
		}
		leaderAICategory = leader.aiCategory;
		RebindAIForLeader();
		return true;
	}

	private void RebindAIForLeader()
	{
		AI = UnitAIFactory.CreateForActor(this);
		_boundToLeader = leaderAICategory != CharacterAICategory.None;
		_boundLeaderCategory = leaderAICategory;
		_boundTargetTile = targetTile;
	}

	public CharacterData ResolveLeader(GameWorld world)
	{
		int num = ((transport != null) ? transport.leaderCharacterId : leaderCharacterId);
		if (num < 0)
		{
			return null;
		}
		CharacterData characterData = (world?.GetCharacterManager())?.GetCharacter(num);
		if (characterData == null || !characterData.isAlive)
		{
			return null;
		}
		return characterData;
	}

	private void SyncLeaderAI(CharacterData leader)
	{
		bool flag = leader != null && leader.aiCategory != CharacterAICategory.None;
		if (leader != null)
		{
			leaderCharacterId = leader.characterId;
			leaderAICategory = leader.aiCategory;
		}
		else
		{
			leaderAICategory = CharacterAICategory.None;
		}
		bool flag2 = leaderAICategory == CharacterAICategory.Courier && targetTile != _boundTargetTile;
		bool num = flag != _boundToLeader || leaderAICategory != _boundLeaderCategory;
		bool flag3 = !flag && (_boundToLeader || _ai == null);
		if (num || flag2 || flag3)
		{
			AI = UnitAIFactory.CreateForActor(this);
			_boundToLeader = flag;
			_boundLeaderCategory = leaderAICategory;
			_boundTargetTile = targetTile;
		}
	}

	private static float LeaderMarchMultiplier(CharacterData leader)
	{
		if (leader == null)
		{
			return 1f;
		}
		float ep;
		float es;
		float em;
		float eman;
		float ec;
		float esch;
		float ech;
		leader.GetEffectiveStats(out ep, out es, out em, out eman, out ec, out esch, out ech);
		float num = (Mathf.Clamp(em, 0f, 100f) + Mathf.Clamp(ep, 0f, 100f)) / 200f;
		float num2 = Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(leader.health / 100f));
		return Mathf.Clamp(1f + num * 0.25f, 0.9f, 1.3f) * num2;
	}

	private void UpdateMemberLocations(GameWorld world)
	{
		if (transport?.members == null)
		{
			return;
		}
		CharacterManager characterManager = world?.GetCharacterManager();
		if (characterManager == null)
		{
			return;
		}
		foreach (MemberSlot member in transport.members)
		{
			CharacterData character = characterManager.GetCharacter(member.characterId);
			if (character != null)
			{
				character.currentSettlementAnchorId = currentSettlementAnchorId;
				character.currentTile = currentTile;
			}
		}
	}

	private void TickNamedMembers(GameWorld world, float deltaDays)
	{
		if (transport?.members == null || transport.members.Count == 0)
		{
			return;
		}
		CharacterManager characterManager = world?.GetCharacterManager();
		if (characterManager == null)
		{
			return;
		}
		for (int num = transport.members.Count - 1; num >= 0; num--)
		{
			int characterId = transport.members[num].characterId;
			CharacterData character = characterManager.GetCharacter(characterId);
			if (character == null || !character.isAlive)
			{
				transport.RemoveMember(characterId);
			}
			else if (!(character.health >= 30f))
			{
				float num2 = Mathf.Clamp01((30f - character.health) / 100f) * deltaDays;
				character.health = Mathf.Max(0f, character.health - num2 * 4f);
				if (character.health <= 0f)
				{
					character.Die(world.currentDay, world.currentYear, "随队伤病");
					transport.RemoveMember(characterId);
				}
			}
		}
	}
}
}
