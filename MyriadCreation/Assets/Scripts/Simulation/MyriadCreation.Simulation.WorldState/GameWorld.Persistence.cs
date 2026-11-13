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
using MyriadCreation.Simulation.ContentHost;
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
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld : MonoBehaviour
{

	/// <summary>
	/// 读档专用初始化：补齐**不入档**的运行时定义与控制器，不重置地图/存档数据。
	///
	/// 读档路径不会走 InitializeWorld（那会重建地块/省份/聚落），因此必须在 ApplyToWorld
	/// 末尾显式补齐：unitDefs（否则军队人力/战力恒 0、战斗 CheckBattleEnd 得 NaN 永不结束）、
	/// 信仰运行态（FaithSystem 不入档，否则教化/圣战全停）、以及每政权一个 AIController
	/// （只有地形生成路径才创建，读档后否则 AI 研究/外交/意图全停）。
	/// 所有步骤幂等，重复调用安全。
	/// </summary>
	public void PrepareLoadedWorld()
	{
		if (!ContentRegistry.IsInitialized)
		{
			ContentRegistry.Initialize();
		}
		if (config == null)
		{
			config = WorldConfig.CreateRuntimeInstance();
		}
		InitializeUnitDefs();
		InitializeFaithSystems();
		EnsureAIControllers();
		BuildPreloadPlan();
		Debug.Log((object)$"[GameWorld] 读档运行时补齐完成：{unitDefs.Count} 兵种 / {_faithSystems.Count} 教统 / {_aiManager?.GetAllControllers().Count ?? 0} 个 AI");
	}

	/// <summary>
	/// 读档后让运行时自增 ID 从存档最大值之后继续，避免新建军队/战争与旧实体撞 ID。
	/// 只在比当前计数器更大时推进，不后退。
	/// </summary>
	public void AdoptRuntimeIdCounters(int nextWarId, int nextArmyId, int nextNavyId = 1)
	{
		if (nextWarId > _nextWarId)
		{
			_nextWarId = nextWarId;
		}
		if (nextArmyId > _nextArmyId)
		{
			_nextArmyId = nextArmyId;
		}
		if (nextNavyId > _nextNavyId)
		{
			_nextNavyId = nextNavyId;
		}
	}
}
}
