using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Culture
{
/// <summary>
/// 文化阶段定义（内容管线试点，对应 StreamingAssets/Base/Culture/CultureStages.json）。
/// stage 取值与 GameEnums.CultureStage 一致（0..4）；nextPopulation 为"从该阶段进化所需人口"，-1 表示终态。
/// 这些字段全部是内容数据，脚本只保留通用评估逻辑与革新门槛。
/// </summary>
[Serializable]
public class CultureStageDefinition
{
	public int stage;

	public string key = "";

	public string name = "";

	/// <summary>进入该阶段所需的最低人口（用于按人口查询阶段，与 nextPopulation 链一致）。</summary>
	public int minPopulation;

	/// <summary>从该阶段进化到下一阶段所需人口；-1 = 终态不可进化。</summary>
	public int nextPopulation = -1;

	/// <summary>排序（默认与 stage 相同）。</summary>
	public int order;

	public float requiredSocialStratification;

	public float requiredSpecialization;

	public bool requiresUrbanCenter;
}

/// <summary>CultureStages.json 的根对象（JsonUtility 不支持 Dictionary，阶段用数组）。</summary>
[Serializable]
public class CultureStagesWrapper
{
	public List<CultureStageDefinition> stages = new List<CultureStageDefinition>();
}
}
