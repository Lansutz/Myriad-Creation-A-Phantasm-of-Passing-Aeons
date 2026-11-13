using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Culture
{
/// <summary>
/// 文化阶段定义目录（"定义是定义，脚本是脚本"内容管线试点）：
/// 优先从 StreamingAssets/Base/Culture/CultureStages.json 读取；首次访问时懒加载一次；
/// 文件缺失/损坏时使用代码默认（与 CultureStageEvolutionSystem 的旧人口常量一对一相等），保证行为不变。
/// 只读取定义数据，不改变文化逻辑/存档/UI；mod 可通过同路径 JSON 覆盖阶段阈值与条件。
/// </summary>
public static class CultureStageCatalog
{
	/// <summary>相对 StreamingAssets 的默认文化阶段定义路径。</summary>
	public const string RelativePath = ContentPaths.Base + "/Culture/CultureStages.json";

	private static bool _attempted;

	private static bool _loadedFromJson;

	private static string _lastError = "";

	private static List<CultureStageDefinition> _stages = CreateDefaultStages();

	/// <summary>是否已从 JSON 成功加载（false = 使用代码默认回退）。</summary>
	public static bool IsLoadedFromJson => _loadedFromJson;

	/// <summary>最近一次加载失败原因（成功后为空）。</summary>
	public static string LastError => _lastError;

	/// <summary>当前生效的阶段定义（按 stage 升序；未加载 JSON 时为代码默认）。</summary>
	public static IReadOnlyList<CultureStageDefinition> Stages => _stages;

	/// <summary>启动/首次访问懒加载：只尝试一次，返回是否成功读到 JSON。</summary>
	public static bool EnsureLoaded()
	{
		if (!_attempted)
		{
			LoadFromStreamingAssets();
		}
		return _loadedFromJson;
	}

	/// <summary>默认路径（Editor/PC：Assets/StreamingAssets/Base/Culture/CultureStages.json）。</summary>
	public static string ResolveDefaultPath()
	{
		string root = Application.streamingAssetsPath;
		return (string.IsNullOrEmpty(root) ? "" : Path.Combine(root, RelativePath));
	}

	public static bool LoadFromStreamingAssets()
	{
		return LoadFromFile(ResolveDefaultPath());
	}

	/// <summary>从指定 JSON 文件加载；失败时回退默认定义并返回 false（不抛异常）。</summary>
	public static bool LoadFromFile(string path)
	{
		_attempted = true;
		try
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				return Fail("找不到文化阶段定义文件：" + path);
			}
			return ApplyJson(File.ReadAllText(path));
		}
		catch (Exception ex)
		{
			return Fail("读取文化阶段定义失败：" + ex.Message);
		}
	}

	/// <summary>解析并应用 JSON 字符串（测试/mod 注入用）；失败时回退默认并返回 false。</summary>
	public static bool ApplyJson(string json)
	{
		_attempted = true;
		try
		{
			if (string.IsNullOrEmpty(json))
			{
				return Fail("文化阶段定义 JSON 为空");
			}
			CultureStagesWrapper cultureStagesWrapper = JsonUtility.FromJson<CultureStagesWrapper>(json);
			if (cultureStagesWrapper?.stages == null || cultureStagesWrapper.stages.Count == 0)
			{
				return Fail("文化阶段定义 JSON 缺少 stages");
			}
			List<CultureStageDefinition> list = new List<CultureStageDefinition>();
			bool[] array = new bool[5];
			foreach (CultureStageDefinition stage in cultureStagesWrapper.stages)
			{
				if (stage == null || stage.stage < 0 || stage.stage >= 5 || array[stage.stage])
				{
					return Fail("文化阶段定义 JSON 存在空项/越界/重复 stage");
				}
				array[stage.stage] = true;
				list.Add(stage);
			}
			for (int i = 0; i < 5; i++)
			{
				if (!array[i])
				{
					return Fail("文化阶段定义 JSON stage 必须 0..4 连续（缺少 " + i + "）");
				}
			}
			list.Sort((CultureStageDefinition a, CultureStageDefinition b) => a.order.CompareTo(b.order));
			foreach (CultureStageDefinition item in list)
			{
				if (item.stage >= 4 && item.nextPopulation != -1)
				{
					return Fail("终态文化阶段（stage=4）nextPopulation 必须为 -1");
				}
				if (item.stage < 4 && item.nextPopulation <= 0)
				{
					return Fail("非终态文化阶段 nextPopulation 必须为正数（stage=" + item.stage + "）");
				}
			}
			_stages = list;
			_loadedFromJson = true;
			_lastError = "";
			return true;
		}
		catch (Exception ex)
		{
			return Fail("解析文化阶段定义失败：" + ex.Message);
		}
	}

	/// <summary>清空加载状态并恢复代码默认（测试隔离/热重载用）。</summary>
	public static void Reset()
	{
		_attempted = false;
		_loadedFromJson = false;
		_lastError = "";
		_stages = CreateDefaultStages();
	}

	/// <summary>按 stage 取定义（未找到返回 false，调用方回退旧逻辑/常量）。</summary>
	public static bool TryGet(int stage, out CultureStageDefinition definition)
	{
		definition = null;
		if (_stages == null)
		{
			return false;
		}
		foreach (CultureStageDefinition stageDefinition in _stages)
		{
			if (stageDefinition != null && stageDefinition.stage == stage)
			{
				definition = stageDefinition;
				return true;
			}
		}
		return false;
	}

	public static bool TryGet(GameEnums.CultureStage stage, out CultureStageDefinition definition)
	{
		return TryGet((int)stage, out definition);
	}

	/// <summary>从当前阶段进化到下一阶段所需人口；未找到定义时回退旧硬编码常量，终态返回 -1。</summary>
	public static int GetNextPopulationThreshold(GameEnums.CultureStage stage)
	{
		if (TryGet(stage, out CultureStageDefinition definition))
		{
			return definition.nextPopulation;
		}
		return stage switch
		{
			GameEnums.CultureStage.Band => CultureStageEvolutionSystem.POP_BAND_TO_TRIBE, 
			GameEnums.CultureStage.Tribe => CultureStageEvolutionSystem.POP_TRIBE_TO_CHIEFDOM, 
			GameEnums.CultureStage.Chiefdom => CultureStageEvolutionSystem.POP_CHIEFDOM_TO_ETHNIC, 
			GameEnums.CultureStage.EthnicGroup => CultureStageEvolutionSystem.POP_ETHNIC_TO_CIVILIZATION, 
			_ => -1, 
		};
	}

	/// <summary>该阶段进化所需的初步社会分层；未找到定义时回退旧硬编码阈值。</summary>
	public static float GetRequiredSocialStratification(GameEnums.CultureStage stage)
	{
		if (TryGet(stage, out CultureStageDefinition definition))
		{
			return definition.requiredSocialStratification;
		}
		return stage switch
		{
			GameEnums.CultureStage.Tribe => 0.2f, 
			GameEnums.CultureStage.Chiefdom => 0.5f, 
			GameEnums.CultureStage.EthnicGroup => 0.7f, 
			_ => 0f, 
		};
	}

	/// <summary>该阶段进化所需的专业化分工；未找到定义时回退旧硬编码阈值。</summary>
	public static float GetRequiredSpecialization(GameEnums.CultureStage stage)
	{
		if (TryGet(stage, out CultureStageDefinition definition))
		{
			return definition.requiredSpecialization;
		}
		return stage switch
		{
			GameEnums.CultureStage.Chiefdom => 0.3f, 
			GameEnums.CultureStage.EthnicGroup => 0.5f, 
			_ => 0f, 
		};
	}

	/// <summary>该阶段进化是否要求城市级定居点；未找到定义时回退旧硬编码（仅 EthnicGroup 要求）。</summary>
	public static bool RequiresUrbanCenter(GameEnums.CultureStage stage)
	{
		if (TryGet(stage, out CultureStageDefinition definition))
		{
			return definition.requiresUrbanCenter;
		}
		return stage == GameEnums.CultureStage.EthnicGroup;
	}

	/// <summary>按人口求"已进入的最高阶段"（取 minPopulation &lt;= population 的最大 stage；默认 Band）。</summary>
	public static GameEnums.CultureStage GetStageForPopulation(int population)
	{
		EnsureLoaded();
		GameEnums.CultureStage cultureStage = GameEnums.CultureStage.Band;
		if (_stages == null)
		{
			return cultureStage;
		}
		foreach (CultureStageDefinition stage in _stages)
		{
			if (stage != null && population >= stage.minPopulation && stage.stage >= (int)cultureStage)
			{
				cultureStage = (GameEnums.CultureStage)stage.stage;
			}
		}
		return cultureStage;
	}

	/// <summary>代码默认定义（与旧硬编码常量一对一；JSON 缺失/损坏时的回退基准）。</summary>
	public static List<CultureStageDefinition> CreateDefaultStages()
	{
		return new List<CultureStageDefinition>
		{
			new CultureStageDefinition
			{
				stage = 0,
				key = "Band",
				name = "游群",
				minPopulation = 0,
				nextPopulation = CultureStageEvolutionSystem.POP_BAND_TO_TRIBE,
				order = 0,
				requiredSocialStratification = 0f,
				requiredSpecialization = 0f,
				requiresUrbanCenter = false
			},
			new CultureStageDefinition
			{
				stage = 1,
				key = "Tribe",
				name = "部落",
				minPopulation = CultureStageEvolutionSystem.POP_BAND_TO_TRIBE,
				nextPopulation = CultureStageEvolutionSystem.POP_TRIBE_TO_CHIEFDOM,
				order = 1,
				requiredSocialStratification = 0.2f,
				requiredSpecialization = 0f,
				requiresUrbanCenter = false
			},
			new CultureStageDefinition
			{
				stage = 2,
				key = "Chiefdom",
				name = "酋邦",
				minPopulation = CultureStageEvolutionSystem.POP_TRIBE_TO_CHIEFDOM,
				nextPopulation = CultureStageEvolutionSystem.POP_CHIEFDOM_TO_ETHNIC,
				order = 2,
				requiredSocialStratification = 0.5f,
				requiredSpecialization = 0.3f,
				requiresUrbanCenter = false
			},
			new CultureStageDefinition
			{
				stage = 3,
				key = "EthnicGroup",
				name = "族群",
				minPopulation = CultureStageEvolutionSystem.POP_CHIEFDOM_TO_ETHNIC,
				nextPopulation = CultureStageEvolutionSystem.POP_ETHNIC_TO_CIVILIZATION,
				order = 3,
				requiredSocialStratification = 0.7f,
				requiredSpecialization = 0.5f,
				requiresUrbanCenter = true
			},
			new CultureStageDefinition
			{
				stage = 4,
				key = "HighCivilization",
				name = "高级文明",
				minPopulation = CultureStageEvolutionSystem.POP_ETHNIC_TO_CIVILIZATION,
				nextPopulation = -1,
				order = 4,
				requiredSocialStratification = 0f,
				requiredSpecialization = 0f,
				requiresUrbanCenter = false
			}
		};
	}

	private static bool Fail(string error)
	{
		_stages = CreateDefaultStages();
		_loadedFromJson = false;
		_lastError = error ?? "";
		Debug.LogWarning("[CultureStageCatalog] " + _lastError + "，回退代码默认阈值");
		return false;
	}
}
}
