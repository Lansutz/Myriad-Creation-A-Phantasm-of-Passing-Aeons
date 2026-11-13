using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class WarGoal
{
	public int goalId;

	public GameEnums.WarGoalType type;

	public int attackerRealmId;

	public int defenderRealmId;

	public int targetTileIndex;

	public int targetRegionId;

	public float targetWarScore;

	public string description;

	public bool isPrimaryGoal;

	public List<TreatyClauseType> GetSupportedClauses()
	{
		return type switch
		{
			GameEnums.WarGoalType.ConquerTerritory => new List<TreatyClauseType>
			{
				TreatyClauseType.TerritoryCession,
				TreatyClauseType.WarReparations,
				TreatyClauseType.Truce
			}, 
			GameEnums.WarGoalType.ConquerRegion => new List<TreatyClauseType>
			{
				TreatyClauseType.TerritoryCession,
				TreatyClauseType.WarReparations,
				TreatyClauseType.Humiliation,
				TreatyClauseType.Truce
			}, 
			GameEnums.WarGoalType.Vassalization => new List<TreatyClauseType>
			{
				TreatyClauseType.Vassalage,
				TreatyClauseType.WarReparations,
				TreatyClauseType.Truce
			}, 
			GameEnums.WarGoalType.Indemnity => new List<TreatyClauseType>
			{
				TreatyClauseType.WarReparations,
				TreatyClauseType.TradePrivileges,
				TreatyClauseType.Truce
			}, 
			GameEnums.WarGoalType.Humiliation => new List<TreatyClauseType>
			{
				TreatyClauseType.Humiliation,
				TreatyClauseType.WarReparations,
				TreatyClauseType.Truce
			}, 
			GameEnums.WarGoalType.Annihilation => new List<TreatyClauseType>
			{
				TreatyClauseType.Annexation,
				TreatyClauseType.TerritoryCession,
				TreatyClauseType.WarCrimesTrial
			}, 
			GameEnums.WarGoalType.BorderAdjustment => new List<TreatyClauseType>
			{
				TreatyClauseType.TerritoryCession,
				TreatyClauseType.BorderDemilitarization,
				TreatyClauseType.Truce
			}, 
			GameEnums.WarGoalType.ConvertReligion => new List<TreatyClauseType>
			{
				TreatyClauseType.ReligiousFreedom,
				TreatyClauseType.WarReparations,
				TreatyClauseType.Truce
			}, 
			_ => new List<TreatyClauseType>
			{
				TreatyClauseType.WarReparations,
				TreatyClauseType.Truce
			}, 
		};
	}
}
}
