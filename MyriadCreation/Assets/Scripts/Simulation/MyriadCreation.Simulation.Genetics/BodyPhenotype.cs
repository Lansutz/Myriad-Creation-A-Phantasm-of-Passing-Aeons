using System;

namespace MyriadCreation.Simulation.Genetics
{
/// <summary>身体构造表型：由离散等位基因推导，不做随机指派。</summary>
[Serializable]
public struct BodyPlanPhenotype
{
	public string wingType;
	public string hornType;
	public string tailType;
	public string posture;
}

/// <summary>体表表型：皮/鳞/羽/甲壳与隔热、天然护甲。</summary>
[Serializable]
public struct IntegumentPhenotype
{
	public string skinType;
	public float insulation;
	public float naturalArmor;
}

/// <summary>代谢表型：食性、耐温、体型。</summary>
[Serializable]
public struct MetabolicPhenotype
{
	public float dietHerbivore;
	public float dietCarnivore;
	public float tempToleranceC;
	public float size;
}
}
