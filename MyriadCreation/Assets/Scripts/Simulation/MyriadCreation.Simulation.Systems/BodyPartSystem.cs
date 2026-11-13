using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public static class BodyPartSystem
{
	private static readonly Dictionary<BodyPartType, SubStructureType[]> PartToSubStructures = new Dictionary<BodyPartType, SubStructureType[]>
	{
		{
			BodyPartType.LeftEye,
			new SubStructureType[4]
			{
				SubStructureType.EyeLens,
				SubStructureType.EyeAqueous,
				SubStructureType.EyeRetina,
				SubStructureType.EyeCornea
			}
		},
		{
			BodyPartType.RightEye,
			new SubStructureType[4]
			{
				SubStructureType.EyeLens,
				SubStructureType.EyeAqueous,
				SubStructureType.EyeRetina,
				SubStructureType.EyeCornea
			}
		},
		{
			BodyPartType.LeftEar,
			new SubStructureType[2]
			{
				SubStructureType.EarDrum,
				SubStructureType.EarInner
			}
		},
		{
			BodyPartType.RightEar,
			new SubStructureType[2]
			{
				SubStructureType.EarDrum,
				SubStructureType.EarInner
			}
		},
		{
			BodyPartType.Brain,
			new SubStructureType[3]
			{
				SubStructureType.BrainCognition,
				SubStructureType.BrainVessel,
				SubStructureType.BrainMental
			}
		},
		{
			BodyPartType.Teeth,
			new SubStructureType[2]
			{
				SubStructureType.ToothBody,
				SubStructureType.ToothGum
			}
		},
		{
			BodyPartType.Hair,
			new SubStructureType[1] { SubStructureType.HairFollicle }
		},
		{
			BodyPartType.Face,
			new SubStructureType[1] { SubStructureType.FaceSkin }
		},
		{
			BodyPartType.Skull,
			new SubStructureType[1] { SubStructureType.SkullBone }
		},
		{
			BodyPartType.Heart,
			new SubStructureType[3]
			{
				SubStructureType.HeartMyocardium,
				SubStructureType.HeartValve,
				SubStructureType.HeartCoronary
			}
		},
		{
			BodyPartType.LeftLung,
			new SubStructureType[1] { SubStructureType.LungTissue }
		},
		{
			BodyPartType.RightLung,
			new SubStructureType[1] { SubStructureType.LungTissue }
		},
		{
			BodyPartType.Abdomen,
			new SubStructureType[3]
			{
				SubStructureType.AbdomenDigestive,
				SubStructureType.AbdomenMetabolic,
				SubStructureType.AbdomenLiverKidney
			}
		},
		{
			BodyPartType.Spine,
			new SubStructureType[1] { SubStructureType.SpineVertebra }
		},
		{
			BodyPartType.Skin,
			new SubStructureType[1] { SubStructureType.SkinTissue }
		},
		{
			BodyPartType.LeftShoulder,
			new SubStructureType[3]
			{
				SubStructureType.JointCartilage,
				SubStructureType.JointSynovium,
				SubStructureType.JointLigament
			}
		},
		{
			BodyPartType.RightShoulder,
			new SubStructureType[3]
			{
				SubStructureType.JointCartilage,
				SubStructureType.JointSynovium,
				SubStructureType.JointLigament
			}
		},
		{
			BodyPartType.LeftHip,
			new SubStructureType[3]
			{
				SubStructureType.JointCartilage,
				SubStructureType.JointSynovium,
				SubStructureType.JointLigament
			}
		},
		{
			BodyPartType.RightHip,
			new SubStructureType[3]
			{
				SubStructureType.JointCartilage,
				SubStructureType.JointSynovium,
				SubStructureType.JointLigament
			}
		},
		{
			BodyPartType.LeftKnee,
			new SubStructureType[3]
			{
				SubStructureType.JointCartilage,
				SubStructureType.JointSynovium,
				SubStructureType.JointLigament
			}
		},
		{
			BodyPartType.RightKnee,
			new SubStructureType[3]
			{
				SubStructureType.JointCartilage,
				SubStructureType.JointSynovium,
				SubStructureType.JointLigament
			}
		},
		{
			BodyPartType.LeftArmBone,
			new SubStructureType[1] { SubStructureType.BoneTissue }
		},
		{
			BodyPartType.RightArmBone,
			new SubStructureType[1] { SubStructureType.BoneTissue }
		},
		{
			BodyPartType.LeftLegBone,
			new SubStructureType[1] { SubStructureType.BoneTissue }
		},
		{
			BodyPartType.RightLegBone,
			new SubStructureType[1] { SubStructureType.BoneTissue }
		},
		{
			BodyPartType.LeftArmMuscle,
			new SubStructureType[1] { SubStructureType.MuscleFiber }
		},
		{
			BodyPartType.RightArmMuscle,
			new SubStructureType[1] { SubStructureType.MuscleFiber }
		},
		{
			BodyPartType.LeftLegMuscle,
			new SubStructureType[1] { SubStructureType.MuscleFiber }
		},
		{
			BodyPartType.RightLegMuscle,
			new SubStructureType[1] { SubStructureType.MuscleFiber }
		},
		{
			BodyPartType.LeftHand,
			new SubStructureType[1] { SubStructureType.Extremity }
		},
		{
			BodyPartType.RightHand,
			new SubStructureType[1] { SubStructureType.Extremity }
		},
		{
			BodyPartType.LeftFoot,
			new SubStructureType[1] { SubStructureType.Extremity }
		},
		{
			BodyPartType.RightFoot,
			new SubStructureType[1] { SubStructureType.Extremity }
		}
	};

	private static readonly Dictionary<SubStructureType, (float threshold, string conditionId, string displayName)> SubStructureToCondition = new Dictionary<SubStructureType, (float, string, string)>
	{
		{
			SubStructureType.EyeLens,
			(30f, "cataract", "白内障")
		},
		{
			SubStructureType.EyeAqueous,
			(40f, "glaucoma", "青光眼")
		},
		{
			SubStructureType.EyeRetina,
			(35f, "retinopathy", "视网膜病变")
		},
		{
			SubStructureType.EyeCornea,
			(30f, "corneal_opacity", "角膜混浊")
		},
		{
			SubStructureType.EarDrum,
			(30f, "otitis_media", "中耳炎")
		},
		{
			SubStructureType.EarInner,
			(40f, "sensorineural_hearing_loss", "神经性耳聋")
		},
		{
			SubStructureType.BrainCognition,
			(50f, "dementia", "痴呆")
		},
		{
			SubStructureType.BrainVessel,
			(60f, "stroke", "中风")
		},
		{
			SubStructureType.BrainMental,
			(40f, "mental_disorder", "精神障碍")
		},
		{
			SubStructureType.ToothBody,
			(30f, "dental_caries", "龋齿")
		},
		{
			SubStructureType.ToothGum,
			(30f, "periodontitis", "牙周炎")
		},
		{
			SubStructureType.HairFollicle,
			(50f, "baldness", "秃顶")
		},
		{
			SubStructureType.HeartMyocardium,
			(40f, "heart_disease", "心脏病")
		},
		{
			SubStructureType.HeartValve,
			(40f, "valve_disease", "瓣膜病")
		},
		{
			SubStructureType.HeartCoronary,
			(50f, "coronary_heart_disease", "冠心病")
		},
		{
			SubStructureType.LungTissue,
			(30f, "lung_infection", "肺部感染")
		},
		{
			SubStructureType.AbdomenDigestive,
			(30f, "digestive_disorder", "消化系统疾病")
		},
		{
			SubStructureType.AbdomenMetabolic,
			(40f, "metabolic_disorder", "代谢疾病")
		},
		{
			SubStructureType.AbdomenLiverKidney,
			(50f, "organ_failure", "肝肾衰竭")
		},
		{
			SubStructureType.SpineVertebra,
			(40f, "spine_disorder", "脊柱病变")
		},
		{
			SubStructureType.SkinTissue,
			(30f, "skin_disorder", "皮肤疾病")
		},
		{
			SubStructureType.JointCartilage,
			(40f, "osteoarthritis", "骨关节炎")
		},
		{
			SubStructureType.JointSynovium,
			(40f, "rheumatoid_arthritis", "类风湿关节炎")
		},
		{
			SubStructureType.JointLigament,
			(30f, "ligament_injury", "韧带损伤")
		},
		{
			SubStructureType.BoneTissue,
			(50f, "fracture", "骨折")
		},
		{
			SubStructureType.MuscleFiber,
			(30f, "muscle_injury", "肌肉损伤")
		},
		{
			SubStructureType.Extremity,
			(30f, "extremity_injury", "肢体损伤")
		}
	};

	private const int DiagnosisDays = 30;

	public static Dictionary<BodyPartType, BodyPartData> InitializeBodyParts()
	{
		Dictionary<BodyPartType, BodyPartData> dictionary = new Dictionary<BodyPartType, BodyPartData>();
		foreach (BodyPartType value3 in Enum.GetValues(typeof(BodyPartType)))
		{
			BodyPartData value = new BodyPartData(value3);
			if (PartToSubStructures.TryGetValue(value3, out var value2))
			{
				SubStructureType[] array = value2;
				foreach (SubStructureType type in array)
				{
					value.subStructures.Add(new SubStructureData(type));
				}
			}
			dictionary[value3] = value;
		}
		return dictionary;
	}

	public static void DailyUpdate(Dictionary<BodyPartType, BodyPartData> bodyParts, int currentDay)
	{
		if (bodyParts == null)
		{
			return;
		}
		foreach (KeyValuePair<BodyPartType, BodyPartData> bodyPart in bodyParts)
		{
			BodyPartData part = bodyPart.Value;
			for (int i = 0; i < part.subStructures.Count; i++)
			{
				SubStructureData value = part.subStructures[i];
				if (value.progressionRate > 0f && value.condition < 100f)
				{
					value.condition = Mathf.Clamp01(value.condition / 100f + value.progressionRate) * 100f;
					part.subStructures[i] = value;
				}
			}
			RecalculatePartState(ref part);
			TryDiagnose(ref part, currentDay);
			bodyParts[bodyPart.Key] = part;
		}
	}

	private static void RecalculatePartState(ref BodyPartData part)
	{
		if (part.subStructures == null || part.subStructures.Count == 0)
		{
			part.currentHp = part.maxHp;
			part.functionLevel = 1f;
			part.state = BodyPartState.Healthy;
			return;
		}
		float num = 0f;
		foreach (SubStructureData subStructure in part.subStructures)
		{
			num += subStructure.condition;
		}
		float num2 = num / (float)part.subStructures.Count;
		part.currentHp = Mathf.Clamp(part.maxHp - num2, 0f, part.maxHp);
		part.functionLevel = Mathf.Clamp01(1f - num2 / 100f);
		if (part.currentHp <= 0f)
		{
			part.state = BodyPartState.Destroyed;
		}
		else if (part.currentHp < part.maxHp * 0.3f)
		{
			part.state = BodyPartState.Critical;
		}
		else if (part.currentHp < part.maxHp)
		{
			part.state = BodyPartState.Injured;
		}
		else
		{
			part.state = BodyPartState.Healthy;
		}
	}

	private static void TryDiagnose(ref BodyPartData part, int currentDay)
	{
		if (part.subStructures == null)
		{
			return;
		}
		if (part.diagnosedConditions == null)
		{
			part.diagnosedConditions = new List<DiagnosedCondition>();
		}
		foreach (SubStructureData subStructure in part.subStructures)
		{
			if (SubStructureToCondition.TryGetValue(subStructure.type, out var mapping) && subStructure.condition >= mapping.threshold && !part.diagnosedConditions.Exists((DiagnosedCondition d) => d.conditionId == mapping.conditionId))
			{
				part.diagnosedConditions.Add(new DiagnosedCondition
				{
					conditionId = mapping.conditionId,
					displayName = mapping.displayName,
					sourceSubStructure = subStructure.type,
					sourcePart = part.part,
					diagnosedDay = currentDay,
					severityAtDiagnosis = subStructure.condition
				});
			}
		}
	}

	public static void AddSubStructureProgression(Dictionary<BodyPartType, BodyPartData> bodyParts, BodyPartType part, SubStructureType subStructure, float progressionRate, string causingFactor)
	{
		if (bodyParts == null || !bodyParts.TryGetValue(part, out var value) || value.subStructures == null)
		{
			return;
		}
		int num = value.subStructures.FindIndex((SubStructureData s) => s.type == subStructure);
		if (num >= 0)
		{
			SubStructureData value2 = value.subStructures[num];
			value2.progressionRate = Mathf.Max(value2.progressionRate, progressionRate);
			if (!string.IsNullOrEmpty(causingFactor) && !value2.causingFactors.Contains(causingFactor))
			{
				value2.causingFactors.Add(causingFactor);
			}
			value.subStructures[num] = value2;
			bodyParts[part] = value;
		}
	}

	public static void SetSubStructureCondition(Dictionary<BodyPartType, BodyPartData> bodyParts, BodyPartType part, SubStructureType subStructure, float condition, string causingFactor)
	{
		if (bodyParts == null || !bodyParts.TryGetValue(part, out var part2) || part2.subStructures == null)
		{
			return;
		}
		int num = part2.subStructures.FindIndex((SubStructureData s) => s.type == subStructure);
		if (num >= 0)
		{
			SubStructureData value = part2.subStructures[num];
			value.condition = Mathf.Clamp(condition, 0f, 100f);
			if (!string.IsNullOrEmpty(causingFactor) && !value.causingFactors.Contains(causingFactor))
			{
				value.causingFactors.Add(causingFactor);
			}
			part2.subStructures[num] = value;
			RecalculatePartState(ref part2);
			bodyParts[part] = part2;
		}
	}

	public static float GetPartFunction(Dictionary<BodyPartType, BodyPartData> bodyParts, BodyPartType part)
	{
		if (bodyParts == null || !bodyParts.TryGetValue(part, out var value))
		{
			return 1f;
		}
		return value.functionLevel;
	}

	public static float GetVisionFunction(Dictionary<BodyPartType, BodyPartData> bodyParts)
	{
		float partFunction = GetPartFunction(bodyParts, BodyPartType.LeftEye);
		float partFunction2 = GetPartFunction(bodyParts, BodyPartType.RightEye);
		return Mathf.Max(partFunction, partFunction2) * 0.7f + (partFunction + partFunction2) / 2f * 0.3f;
	}

	public static float GetHearingFunction(Dictionary<BodyPartType, BodyPartData> bodyParts)
	{
		float partFunction = GetPartFunction(bodyParts, BodyPartType.LeftEar);
		float partFunction2 = GetPartFunction(bodyParts, BodyPartType.RightEar);
		return Mathf.Max(partFunction, partFunction2) * 0.7f + (partFunction + partFunction2) / 2f * 0.3f;
	}
}
}
