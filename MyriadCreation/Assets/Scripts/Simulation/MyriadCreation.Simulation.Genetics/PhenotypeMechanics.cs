using System;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.State;
using UnityEngine;

namespace MyriadCreation.Simulation.Genetics
{
/// <summary>
/// 表型 → 机制（D3）：只把可解释的生理差异接到既有系统，不新建加成层。
/// - 角/爪/体型/天然护甲 → 个人战力（近身对抗的生理基础）
/// - 隔热/耐温 → 寒热健康损耗
/// - 翼型 → 军队斥候范围（具翼指挥官）
/// </summary>
public static class PhenotypeMechanics
{
	public const float ColdComfortC = -10f;

	public const float HeatComfortC = 30f;

	public const float MaxDailyTemperaturePenalty = 0.5f;

	/// <summary>角色当前表型；缺基因组/旧档回退默认，不抛错。</summary>
	public static PhenotypeResult PhenotypeOf(CharacterData character)
	{
		if (character?.genome == null) return new PhenotypeResult();
		GenomeDef def = GeneticsCatalog.ResolveGenome(character.genome.genomeId);
		return GeneticsEngine.ComputePhenotype(def, character.genome);
	}

	/// <summary>个人战力差值：体型、角、天然护甲都来自身体构造，不是通用 buff。</summary>
	public static float ProwessBonus(PhenotypeResult phenotype)
	{
		float bonus = 0f;
		float size = phenotype.metabolism.size <= 0f ? 1f : phenotype.metabolism.size;
		bonus += (size - 1f) * 10f;
		switch (phenotype.bodyPlan.hornType)
		{
			case "keratin": bonus += 5f; break;
			case "antler": bonus += 3f; break;
		}
		bonus += Mathf.Clamp01(phenotype.integument.naturalArmor) * 8f;
		return bonus;
	}

	/// <summary>
	/// 寒热健康损耗（每日）：舒适区由耐温与隔热共同决定；隔热利于寒、略不利于暑。
	/// 返回 0 表示无损耗；上限避免极端气候一天致死。
	/// </summary>
	public static float TemperatureHealthPenalty(PhenotypeResult phenotype, float ambientTempC)
	{
		float tolerance = phenotype.metabolism.tempToleranceC;
		float insulation = Mathf.Clamp01(phenotype.integument.insulation);
		float lower = ColdComfortC - tolerance - insulation * 10f;
		float upper = HeatComfortC + tolerance - insulation * 5f;
		float penalty = 0f;
		if (ambientTempC < lower) penalty = (lower - ambientTempC) * 0.002f;
		else if (ambientTempC > upper) penalty = (ambientTempC - upper) * 0.002f;
		return Mathf.Clamp(penalty, 0f, MaxDailyTemperaturePenalty);
	}

	/// <summary>具翼指挥官提供的斥候范围增量（膜翼 > 羽翼；无翼 0）。</summary>
	public static int ScoutRangeBonus(PhenotypeResult phenotype)
	{
		switch (phenotype.bodyPlan.wingType)
		{
			case "membrane": return 2;
			case "feathered": return 1;
			default: return 0;
		}
	}
}
}
