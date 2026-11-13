using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Hydrology;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Generation
{
public class GenerationPipeline
{
	private static readonly GenerationStage[] StageOrder = new GenerationStage[9]
	{
		GenerationStage.Seed,
		GenerationStage.Terrain,
		GenerationStage.Features,
		GenerationStage.Climate,
		GenerationStage.Hydrology,
		GenerationStage.Biomes,
		GenerationStage.Provinces,
		GenerationStage.Settlements,
		GenerationStage.Society
	};

	private static readonly Dictionary<GenerationStage, string> StageNames = new Dictionary<GenerationStage, string>
	{
		{
			GenerationStage.Seed,
			"初始化种子"
		},
		{
			GenerationStage.Terrain,
			"生成地形"
		},
		{
			GenerationStage.Features,
			"标记地理特征"
		},
		{
			GenerationStage.Climate,
			"计算气候"
		},
		{
			GenerationStage.Hydrology,
			"重算水文"
		},
		{
			GenerationStage.Biomes,
			"划分群系"
		},
		{
			GenerationStage.Provinces,
			"划分省份"
		},
		{
			GenerationStage.Settlements,
			"生成聚落"
		},
		{
			GenerationStage.Society,
			"生成社会"
		}
	};

	private readonly GameWorld _world;

	private GenerationStage _currentStage;

	private readonly HashSet<GenerationStage> _completedStages = new HashSet<GenerationStage>();

	private bool _isGenerating;

	public GenerationStage CurrentStage => _currentStage;

	public bool IsGenerating => _isGenerating;

	public IReadOnlyCollection<GenerationStage> CompletedStages => _completedStages;

	public event Action<GenerationStage, float> OnProgress;

	public event Action<GenerationStage> OnStageComplete;

	public event Action OnComplete;

	public GenerationPipeline(GameWorld world)
	{
		_world = world;
	}

	public static string GetStageName(GenerationStage stage)
	{
		if (!StageNames.TryGetValue(stage, out var value))
		{
			return stage.ToString();
		}
		return value;
	}

	public static int GetStageIndex(GenerationStage stage)
	{
		return Array.IndexOf(StageOrder, stage);
	}

	public static bool IsDownstream(GenerationStage stage, GenerationStage afterStage)
	{
		return GetStageIndex(stage) > GetStageIndex(afterStage);
	}

	public bool IsStageComplete(GenerationStage stage)
	{
		return _completedStages.Contains(stage);
	}

	public void GenerateAll()
	{
		if (_isGenerating)
		{
			Debug.LogWarning((object)"[GenerationPipeline] 正在生成中，忽略重复调用");
			return;
		}
		_isGenerating = true;
		_completedStages.Clear();
		int num = StageOrder.Length;
		try
		{
			for (int i = 0; i < num; i++)
			{
				GenerationStage generationStage = (_currentStage = StageOrder[i]);
				float arg = (float)i / (float)num;
				OnProgress?.Invoke(generationStage, arg);
				ExecuteStage(generationStage);
				_completedStages.Add(generationStage);
				OnStageComplete?.Invoke(generationStage);
			}
			_currentStage = GenerationStage.Complete;
			_completedStages.Add(GenerationStage.Complete);
			OnProgress?.Invoke(GenerationStage.Complete, 1f);
			OnComplete?.Invoke();
			Debug.Log((object)"[GenerationPipeline] 全部生成完成");
		}
		catch (Exception ex)
		{
			Debug.LogError((object)$"[GenerationPipeline] 生成在 {_currentStage} 阶段失败: {ex.Message}");
			throw;
		}
		finally
		{
			_isGenerating = false;
		}
	}

	public void RegenerateFrom(GenerationStage fromStage)
	{
		if (_isGenerating)
		{
			Debug.LogWarning((object)"[GenerationPipeline] 正在生成中，忽略重复调用");
			return;
		}
		int stageIndex = GetStageIndex(fromStage);
		if (stageIndex < 0)
		{
			Debug.LogError((object)$"[GenerationPipeline] 未知阶段: {fromStage}");
			return;
		}
		for (int i = stageIndex; i < StageOrder.Length; i++)
		{
			_completedStages.Remove(StageOrder[i]);
		}
		_completedStages.Remove(GenerationStage.Complete);
		_isGenerating = true;
		int num = StageOrder.Length;
		try
		{
			for (int j = stageIndex; j < num; j++)
			{
				GenerationStage generationStage = (_currentStage = StageOrder[j]);
				float arg = (float)j / (float)num;
				OnProgress?.Invoke(generationStage, arg);
				ExecuteStage(generationStage);
				_completedStages.Add(generationStage);
				OnStageComplete?.Invoke(generationStage);
			}
			_currentStage = GenerationStage.Complete;
			_completedStages.Add(GenerationStage.Complete);
			OnProgress?.Invoke(GenerationStage.Complete, 1f);
			OnComplete?.Invoke();
			Debug.Log((object)("[GenerationPipeline] 从 " + GetStageName(fromStage) + " 开始重算完成"));
		}
		catch (Exception ex)
		{
			Debug.LogError((object)$"[GenerationPipeline] 重算在 {_currentStage} 阶段失败: {ex.Message}");
			throw;
		}
		finally
		{
			_isGenerating = false;
		}
	}

	public void RegenerateSingle(GenerationStage stage)
	{
		if (_isGenerating)
		{
			return;
		}
		_isGenerating = true;
		try
		{
			_currentStage = stage;
			ExecuteStage(stage);
			_completedStages.Add(stage);
			OnStageComplete?.Invoke(stage);
			Debug.Log((object)("[GenerationPipeline] 单阶段重算: " + GetStageName(stage)));
		}
		finally
		{
			_isGenerating = false;
		}
	}

	public void Reset()
	{
		_completedStages.Clear();
		_currentStage = GenerationStage.None;
		_isGenerating = false;
	}

	private void ExecuteStage(GenerationStage stage)
	{
		switch (stage)
		{
		case GenerationStage.Seed:
		{
			int actualSeed = _world.GenConfig.GetActualSeed();
			Debug.Log((object)$"[GenerationPipeline] 种子: {actualSeed}");
			break;
		}
		case GenerationStage.Terrain:
			_world.GenerateTerrainWithConfig();
			break;
		case GenerationStage.Features:
			OceanZoning.ApplyToTiles(_world.tiles, _world.mapWidth, _world.mapHeight, _world.config.wrapX);
			break;
		case GenerationStage.Climate:
			_world.CalculateClimate();
			break;
		case GenerationStage.Hydrology:
			_world.RecalculateHydrology();
			break;
		case GenerationStage.Biomes:
			Debug.Log((object)"[GenerationPipeline] 群系已在气候阶段划分");
			break;
		case GenerationStage.Provinces:
			_world.GenerateProvinces();
			break;
		case GenerationStage.Settlements:
			if (_world.SetupOptions.generateSettlements)
			{
				_world.GenerateSettlements();
			}
			break;
		case GenerationStage.Society:
			if (_world.SetupOptions.generatePolities)
			{
				_world.InitializeDefaultRealms();
			}
			break;
		}
	}
}
}
