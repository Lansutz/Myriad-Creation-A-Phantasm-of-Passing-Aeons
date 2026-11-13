using MyriadCreation.Simulation.ContentHost;
using UnityEngine;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld
{
	private readonly PreloadService _preloadService = new PreloadService();

	public bool IsPreloadComplete => _preloadService.IsIdle;

	public PreloadReport LastPreloadReport => _preloadService.LastReport;

	/// <summary>每帧预加载预算（毫秒）；默认 0.5ms，超出的任务留到下一帧。</summary>
	public double preloadFrameBudgetMs = 0.5;

	/// <summary>
	/// 构建预热计划：内容表 → 每政权 AI 控制器 → AI 上下文（统治者人格同步）→ 常用查询。
	/// 新局与读档都调用；任务幂等，重复构建只保证就绪。
	/// </summary>
	public void BuildPreloadPlan()
	{
		_preloadService.Clear();
		_preloadService.Enqueue("content.tables", () =>
		{
			if (!ContentRegistry.IsInitialized)
			{
				ContentRegistry.Initialize();
			}
			return true;
		});
		if (realms != null)
		{
			foreach (int realmId in realms.Keys)
			{
				int id = realmId;
				_preloadService.Enqueue("ai.controller." + id, () =>
				{
					WarmAIController(id);
					return true;
				});
			}
		}
		_preloadService.Enqueue("ai.context", () =>
		{
			if (_aiManager != null && _characterManager != null)
			{
				_aiManager.SyncRulers(_characterManager);
			}
			return true;
		});
		_preloadService.Enqueue("query.rulers", () =>
		{
			if (_characterManager != null && realms != null)
			{
				foreach (int realmId in realms.Keys)
				{
					_characterManager.FindRulerOfRealm(realmId);
				}
			}
			return true;
		});
	}

	/// <summary>测试/诊断用：不限预算跑完当前计划。</summary>
	public PreloadReport RunPreloadNow()
	{
		return _preloadService.RunBudgeted(0.0, () => Time.realtimeSinceStartupAsDouble * 1000.0);
	}

	private void TickPreload()
	{
		if (_preloadService.IsIdle)
		{
			return;
		}
		_preloadService.RunBudgeted(preloadFrameBudgetMs, () => Time.realtimeSinceStartupAsDouble * 1000.0);
	}

	private void WarmAIController(int realmId)
	{
		if (_aiManager == null || realms == null || !realms.ContainsKey(realmId))
		{
			return;
		}
		if (_aiManager.GetController(realmId) == null)
		{
			_aiManager.CreateController(realmId);
		}
	}
}
}
