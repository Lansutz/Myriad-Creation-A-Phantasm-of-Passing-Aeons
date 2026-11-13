using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.CultureHost
{
/// <summary>
/// ICultureContext 的 host 实现（B5）：把 GameWorld/CultureData/InnovationTree/SocialStratificationCalculator
/// 收束为 id + 简单值；Culture 运行时只依赖 Core.Contracts 接口。
/// 当前首步先提供适配器，后续 MonthlyTick/其它 host 胶水可直接使用它，Culture asmdef 不引用本文件。
/// </summary>
public sealed class CultureContextAdapter : ICultureContext
{
	private readonly GameWorld _world;

	public CultureContextAdapter(GameWorld world)
	{
		_world = world;
	}

	public int CurrentDay => ((_world != null) ? _world.currentDay : 0);

	public IReadOnlyList<int> GetRealmIds()
	{
		List<int> list = new List<int>();
		if (_world?.realms != null)
		{
			foreach (int key in _world.realms.Keys)
			{
				list.Add(key);
			}
		}
		return list;
	}

	public int GetRealmCultureId(int realmId)
	{
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out var realm) || realm == null)
		{
			return -1;
		}
		return realm.primaryCultureId;
	}

	public string GetCultureName(int cultureId)
	{
		if (_world?.cultures == null || !_world.cultures.TryGetValue(cultureId, out var culture) || culture == null)
		{
			return "";
		}
		return culture.cultureName ?? "";
	}

	public int GetCultureStage(int cultureId)
	{
		if (_world?.cultures == null || !_world.cultures.TryGetValue(cultureId, out var culture) || culture == null)
		{
			return -1;
		}
		return (int)culture.stage;
	}

	public void SetCultureStage(int cultureId, int stage)
	{
		if (stage < 0 || stage > 4 || _world?.cultures == null || !_world.cultures.TryGetValue(cultureId, out var culture) || culture == null)
		{
			return;
		}
		culture.stage = (GameEnums.CultureStage)stage;
	}

	public void GetRealmSocialState(int realmId, out float socialStratification, out float specialization, out int population)
	{
		socialStratification = 0f;
		specialization = 0f;
		population = 0;
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out var realm) || realm == null || _world.cultures == null || !_world.cultures.TryGetValue(realm.primaryCultureId, out var culture) || culture == null)
		{
			return;
		}
		SocialStratificationCalculator.Result result = SocialStratificationCalculator.Calculate(_world, realm.realmId, culture, _world.GetInnovationTree());
		socialStratification = result.socialStratification;
		specialization = result.specialization;
		population = UnityEngine.Mathf.RoundToInt(result.totalPopulation);
	}

	public IReadOnlyList<int> GetRealmInnovationIds(int realmId)
	{
		InnovationTree innovationTree = _world?.GetInnovationTree();
		if (innovationTree == null)
		{
			return Array.Empty<int>();
		}
		HashSet<int> owned = innovationTree.GetRealmInnovations(realmId);
		if (owned == null)
		{
			return Array.Empty<int>();
		}
		return new List<int>(owned);
	}

	public bool HasUrbanCenter(int realmId)
	{
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out var realm) || realm == null)
		{
			return false;
		}
		return CultureStageEvolutionHost.HasUrbanCenter(_world, realm);
	}
}
}
