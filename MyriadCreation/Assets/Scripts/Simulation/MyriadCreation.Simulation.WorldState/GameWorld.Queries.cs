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
using MyriadCreation.Simulation.Definitions;
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
using MyriadCreation.Simulation.LawHost;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld : MonoBehaviour
{

	public bool TryGetSiteByAnchor(int anchorId, out HabitationSite site)
	{
		if (settlements.TryGetValue(anchorId, out var value))
		{
			site = value;
			return true;
		}
		if (_campManager != null && _campManager.TryGetActiveSiteByAnchor(anchorId, out var site2))
		{
			site = site2;
			return true;
		}
		site = null;
		return false;
	}

	public bool TryGetSiteAtTile(int tileIndex, out HabitationSite site)
	{
		foreach (HabitationSite habitationSite in HabitationSites)
		{
			if (habitationSite.anchorId >= 0 && anchors.TryGetValue(habitationSite.anchorId, out var value) && value.tileIndex == tileIndex)
			{
				site = habitationSite;
				return true;
			}
		}
		site = null;
		return false;
	}

	public IEnumerable<HabitationSite> GetSitesByMode(SiteMode mode)
	{
		foreach (HabitationSite habitationSite in HabitationSites)
		{
			if (habitationSite.siteMode == mode)
			{
				yield return habitationSite;
			}
		}
	}

	public bool TryGetHabitationSite(string siteId, out HabitationSite site)
	{
		if (!string.IsNullOrEmpty(siteId))
		{
			foreach (HabitationSite habitationSite in HabitationSites)
			{
				if (habitationSite.siteId == siteId)
				{
					site = habitationSite;
					return true;
				}
			}
		}
		site = null;
		return false;
	}

	public FaithSystem GetFaithSystem(int faithId)
	{
		return _faithSystems.Find((FaithSystem f) => f.faithId == faithId);
	}

	/// <summary>全部教统（只读）。存档捕获用；读档时 PrepareLoadedWorld 先按内容表重建，再由 FaithPersistence overlay 运行态。</summary>
	public IReadOnlyList<FaithSystem> FaithSystems => _faithSystems;

	public IReadOnlyList<Denomination> Denominations => _denominations;

	public Denomination GetDenomination(int denominationId)
	{
		return _denominations.Find(d => d != null && d.denominationId == denominationId);
	}

	public bool RegisterDenomination(Denomination denomination)
	{
		if (denomination == null || denomination.denominationId == -1 || GetDenomination(denomination.denominationId) != null)
		{
			return false;
		}
		_denominations.Add(denomination);
		return true;
	}

	public void RebuildDenominationMembership()
	{
		foreach (Denomination denomination in _denominations)
		{
			denomination?.faithIds.Clear();
		}
		foreach (FaithSystem faith in _faithSystems)
		{
			GetDenomination(faith.denominationId)?.faithIds.Add(faith.faithId);
		}
	}

	private string GetCharacterName(int characterId)
	{
		CharacterData characterData = GetCharacterManager()?.GetCharacter(characterId);
		if (characterData == null)
		{
			return characterId.ToString();
		}
		return characterData.firstName + " " + characterData.lastName;
	}

	public bool TileExists(int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= tiles.Length)
		{
			return false;
		}
		return tiles[tileIndex].exists;
	}

	public List<int> GetValidTiles()
	{
		List<int> list = new List<int>();
		for (int i = 0; i < tiles.Length; i++)
		{
			if (tiles[i].exists)
			{
				list.Add(i);
			}
		}
		return list;
	}

	public int GetValidTileCount()
	{
		int num = 0;
		for (int i = 0; i < tiles.Length; i++)
		{
			if (tiles[i].exists)
			{
				num++;
			}
		}
		return num;
	}

	public List<int> GetNeighbours(int tileIndex)
	{
		return TileGrid.GetNeighbours(tileIndex, mapWidth, mapHeight, config.wrapX, config.wrapY);
	}

	public float GetTilePopulation(int tileIndex)
	{
		if (tileIndex < 0 || tileIndex >= tiles.Length)
		{
			return 0f;
		}
		float num = 0f;
		if (tiles[tileIndex].populationBlocks != null)
		{
			foreach (PopulationBlock populationBlock in tiles[tileIndex].populationBlocks)
			{
				num += populationBlock.count;
			}
		}
		return num;
	}

	public int GetLandTileCount()
	{
		if (_seaLandGenerator == null)
		{
			return 0;
		}
		return _seaLandGenerator.GetTotalLandTiles();
	}

	public int GetSeaTileCount()
	{
		if (_seaLandGenerator == null)
		{
			return 0;
		}
		return _seaLandGenerator.GetTotalSeaTiles();
	}

	public int GetConnectedSeaCount()
	{
		if (_seaLandGenerator == null)
		{
			return 0;
		}
		return _seaLandGenerator.GetConnectedSeaCount();
	}

	public TileData GetTile(int x, int y)
	{
		return tiles[y * mapWidth + x];
	}

	public TileData GetTile(int index)
	{
		return tiles[index];
	}

	public EconomyManager GetEconomyManager()
	{
		return _economyManager;
	}

	public MyriadCreation.World.Terrain.SeaLandGenerator GetSeaLandGenerator()
	{
		return _seaLandGenerator;
	}

	public PlanetClimateSimulator GetClimateSimulator()
	{
		return _climateSimulator;
	}

	public CombatManager GetCombatManager()
	{
		return _combatManager;
	}

	public List<WarState> GetWars()
	{
		return _wars;
	}

	public DiplomacyManager GetDiplomacyManager()
	{
		return _diplomacyManager;
	}

	public CharacterManager GetCharacterManager()
	{
		return _characterManager;
	}

	public ThoughtManager GetThoughtManager()
	{
		return _thoughtManager;
	}

	public RealmSociety GetRealmSociety(int realmId)
	{
		return _politicsSimulationSystem?.GetRealmSociety(realmId);
	}

	public JudicialSystem GetOrCreateJudicialSystem(int realmId)
	{
		if (_judicialSystems.TryGetValue(realmId, out var value))
		{
			return value;
		}
		if (realms == null || !realms.TryGetValue(realmId, out var value2))
		{
			return null;
		}
		JudicialSystem judicialSystem = new JudicialSystem
		{
			judicialSystemId = realmId + 1,
			judicialSystemName = value2.realmName + "司法",
			source = ((value2.laws != null) ? value2.laws.legalSource : LawSource.Customary),
			laws = LawCatalog.BuildCorpus(value2.laws)
		};
		_judicialSystems[realmId] = judicialSystem;
		return judicialSystem;
	}

	public DisasterSystem GetDisasterSystem()
	{
		return _disasterSystem;
	}

	public DiseaseSystem GetDiseaseSystem()
	{
		return _diseaseSystem;
	}

	public BuildingSystem GetBuildingSystem()
	{
		return _buildingSystem;
	}

	public InnovationTree GetInnovationTree()
	{
		return _innovationTree;
	}

	public Chronicle GetChronicle()
	{
		return _chronicle;
	}

	public AIManager GetAIManager()
	{
		return _aiManager;
	}

	public PoliticalManager GetPoliticalManager()
	{
		return _politicalManager;
	}
}
}
