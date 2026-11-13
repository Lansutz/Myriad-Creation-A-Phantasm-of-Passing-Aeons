using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Settlement;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.CultureHost
{
/// <summary>
/// Culture 的 host 粘合层（B5 首步）：只放直接读/写 GameWorld/RealmData/聚落/革新的胶水；
/// 纯规则（EvaluateEvolution/CheckRegress/革新门槛）留在 MyriadCreation.Simulation.Culture。
/// 依赖方向：CultureHost → Culture/Simulation；Culture 运行时不引用 CultureHost/Simulation 类型。
/// </summary>
public static class CultureStageEvolutionHost
{
	public static int FormRealmFromActor(GameWorld world, MapActor actor, StateFormationPath path, int cultureId)
	{
		if ((Object)(object)world == (Object)null || actor == null || !actor.IsAlive)
		{
			return -1;
		}
		int num = 0;
		foreach (int key in world.realms.Keys)
		{
			if (key >= num)
			{
				num = key + 1;
			}
		}
		RealmData realmData = new RealmData
		{
			realmId = num
		};
		world.realms[num] = realmData;
		realmData.realmName = actor.actorName;
		realmData.primaryCultureId = cultureId;
		if (ContentResolvers.Cultures.TryGet(cultureId, out var value))
		{
			int mobilityType = value.data.mobilityType;
			realmData.realmForm = NomadicRealmSystem.FormFromMobility(mobilityType);
		}
		else
		{
			realmData.realmForm = ((actor.type == MapActorType.Nomad) ? RealmForm.Nomadic : RealmForm.Sedentary);
		}
		if (realmData.realmForm == RealmForm.Nomadic)
		{
			realmData.nomadicRange = new NomadicRange
			{
				centerTile = actor.currentTile,
				summerCampTile = actor.currentTile,
				winterCampTile = actor.currentTile
			};
		}
		else
		{
			TileData tileData = world.tiles[actor.currentTile];
			tileData.ownerRealmId = num;
			realmData.coreTiles.Add(actor.currentTile);
			world.tiles[actor.currentTile] = tileData;
		}
		ApplyPathBonuses(realmData, path);
		MapActorManager.AddPopulationToTile(ref world.tiles[actor.currentTile], actor.population);
		actor.population = 0;
		Debug.Log((object)$"[StageEvolution] {realmData.realmName} 通过{path}路径建国（{realmData.realmForm}），地块={actor.currentTile}");
		return num;
	}

	private static void ApplyPathBonuses(RealmData realm, StateFormationPath path)
	{
		switch (path)
		{
		case StateFormationPath.AgrarianSettlement:
			RealmStability.Set(realm, 55f);
			break;
		case StateFormationPath.NomadicConquest:
			realm.centralization = 0.7f;
			break;
		case StateFormationPath.TradeControl:
			RealmTreasury.Set(realm, 2000f);
			break;
		case StateFormationPath.ReligiousUnification:
			RealmStability.Set(realm, 60f);
			break;
		case StateFormationPath.MilitaryLeague:
			realm.centralization = 0.3f;
			break;
		}
	}

	public static void MonthlyTick(GameWorld world)
	{
		if (world == null)
		{
			return;
		}
		MonthlyTick(new CultureContextAdapter(world));
	}

	/// <summary>MonthlyTick 的 context 版本（host 读取世界事实走 ICultureContext；写入阶段也走 context）。</summary>
	public static void MonthlyTick(ICultureContext context)
	{
		if (context == null)
		{
			return;
		}
		foreach (int realmId in context.GetRealmIds())
		{
			int cultureId = context.GetRealmCultureId(realmId);
			if (cultureId < 0)
			{
				continue;
			}
			int stage = context.GetCultureStage(cultureId);
			if (stage < 0 || stage >= (int)GameEnums.CultureStage.HighCivilization)
			{
				continue;
			}
			context.GetRealmSocialState(realmId, out var socialStratification, out var specialization, out var population);
			HashSet<int> ownedInnovations = new HashSet<int>(context.GetRealmInnovationIds(realmId));
			bool hasUrbanCenter = context.HasUrbanCenter(realmId);
			StageEvolutionResult value3 = CultureStageEvolutionSystem.EvaluateEvolution((GameEnums.CultureStage)stage, population, ownedInnovations, socialStratification, specialization, hasUrbanCenter);
			if (value3.canEvolve)
			{
				int targetStage = (int)value3.targetStage;
				context.SetCultureStage(cultureId, targetStage);
				Debug.Log((object)($"[StageEvolution] 文化「{context.GetCultureName(cultureId)}」{(GameEnums.CultureStage)stage}→{(GameEnums.CultureStage)targetStage}" + $"（分层={socialStratification:F2} 专业化={specialization:F2} 人口={population}）"));
			}
		}
	}

	internal static bool HasUrbanCenter(GameWorld world, RealmData realm)
	{
		if (realm.realmForm == RealmForm.Nomadic)
		{
			if (realm.nomadicRange != null && realm.nomadicRange.winterCampTile >= 0)
			{
				return realm.nomadicRange.summerCampTile >= 0;
			}
			return false;
		}
		if (world.settlements == null)
		{
			return false;
		}
		foreach (SettlementData value in world.settlements.Values)
		{
			if (!value.IsRuined && world.tiles[value.tileIndex].ownerRealmId == realm.realmId && value.settlementForm == SettlementForm.City && value.settlementLevel >= SettlementLevel.LevelIII)
			{
				return true;
			}
		}
		return false;
	}
}
}
