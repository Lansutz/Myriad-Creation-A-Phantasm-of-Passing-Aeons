using System.Collections.Generic;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Systems
{
public class CharacterDiseaseSystem
{
	private readonly Dictionary<string, CharacterDiseaseDef> _diseaseDefs = new Dictionary<string, CharacterDiseaseDef>();

	public CharacterDiseaseSystem()
	{
		InitializeDiseaseDefs();
	}

	private void InitializeDiseaseDefs()
	{
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "plague",
			diseaseName = "鼠疫",
			description = "黑死病，经跳蚤和呼吸道传播，致死率极高",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Vector,
			baseInfectionRate = 0.15f,
			baseMortalityRate = 0.02f,
			baseRecoveryRate = 0.01f,
			incubationDays = 7,
			acuteDurationDays = 14,
			isChronic = false,
			isPermanent = false,
			healthMod = -3f,
			prowessMod = -10f,
			militaryMod = -5f,
			charmMod = -5f,
			treatable = true,
			treatmentRecoveryBonus = 0.02f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "smallpox",
			diseaseName = "天花",
			description = "经呼吸道传播，传染性极强，幸存者留疤",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Respiratory,
			baseInfectionRate = 0.12f,
			baseMortalityRate = 0.01f,
			baseRecoveryRate = 0.02f,
			incubationDays = 12,
			acuteDurationDays = 18,
			isChronic = false,
			isPermanent = false,
			healthMod = -2f,
			prowessMod = -5f,
			charmMod = -8f,
			treatable = true,
			treatmentRecoveryBonus = 0.03f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "cholera",
			diseaseName = "霍乱",
			description = "经水源传播，急性腹泻，暴发快",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Waterborne,
			baseInfectionRate = 0.1f,
			baseMortalityRate = 0.008f,
			baseRecoveryRate = 0.05f,
			incubationDays = 3,
			acuteDurationDays = 7,
			isChronic = false,
			isPermanent = false,
			healthMod = -4f,
			prowessMod = -8f,
			treatable = true,
			treatmentRecoveryBonus = 0.05f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "typhus",
			diseaseName = "斑疹伤寒",
			description = "经虱子传播，与战争和拥挤环境相关",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Vector,
			baseInfectionRate = 0.08f,
			baseMortalityRate = 0.005f,
			baseRecoveryRate = 0.03f,
			incubationDays = 10,
			acuteDurationDays = 11,
			isChronic = false,
			isPermanent = false,
			healthMod = -2f,
			prowessMod = -6f,
			militaryMod = -3f,
			treatable = true,
			treatmentRecoveryBonus = 0.03f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "malaria",
			diseaseName = "疟疾",
			description = "经蚊子传播，地方性疾病，反复发作",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Vector,
			baseInfectionRate = 0.06f,
			baseMortalityRate = 0.002f,
			baseRecoveryRate = 0.01f,
			incubationDays = 14,
			acuteDurationDays = 7,
			isChronic = true,
			isPermanent = false,
			healthMod = -1f,
			prowessMod = -4f,
			scholarshipMod = -2f,
			treatable = true,
			treatmentRecoveryBonus = 0.02f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "tuberculosis",
			diseaseName = "结核病",
			description = "肺痨，慢性消耗性疾病，经密切接触传播",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Respiratory,
			baseInfectionRate = 0.03f,
			baseMortalityRate = 0.003f,
			baseRecoveryRate = 0.005f,
			incubationDays = 90,
			acuteDurationDays = 30,
			isChronic = true,
			isPermanent = false,
			healthMod = -0.5f,
			prowessMod = -3f,
			charmMod = -2f,
			treatable = true,
			treatmentRecoveryBonus = 0.01f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "dysentery",
			diseaseName = "痢疾",
			description = "经水源和食物传播，急性腹泻，恢复快",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Waterborne,
			baseInfectionRate = 0.05f,
			baseMortalityRate = 0.003f,
			baseRecoveryRate = 0.08f,
			incubationDays = 2,
			acuteDurationDays = 5,
			isChronic = false,
			isPermanent = false,
			healthMod = -3f,
			prowessMod = -5f,
			treatable = true,
			treatmentRecoveryBonus = 0.06f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "influenza",
			diseaseName = "流感",
			description = "经呼吸道传播，季节性暴发，传染性强但致死率低",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Respiratory,
			baseInfectionRate = 0.2f,
			baseMortalityRate = 0.001f,
			baseRecoveryRate = 0.08f,
			incubationDays = 2,
			acuteDurationDays = 5,
			isChronic = false,
			isPermanent = false,
			healthMod = -2f,
			prowessMod = -4f,
			treatable = true,
			treatmentRecoveryBonus = 0.05f
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "leprosy",
			diseaseName = "麻风病",
			description = "癞病，慢性传染病，皮肤和神经损伤，社会污名严重",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Contact,
			baseInfectionRate = 0.01f,
			baseMortalityRate = 0.001f,
			baseRecoveryRate = 0.002f,
			incubationDays = 180,
			acuteDurationDays = 60,
			isChronic = true,
			isPermanent = true,
			healthMod = -0.5f,
			prowessMod = -5f,
			charmMod = -15f,
			socialMod = -10f,
			treatable = false
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "syphilis",
			diseaseName = "梅毒",
			description = "经性传播，慢性病程，晚期影响神经系统",
			category = CharacterDiseaseCategory.Infectious,
			transmission = TransmissionType.Sexual,
			baseInfectionRate = 0.05f,
			baseMortalityRate = 0.001f,
			baseRecoveryRate = 0.005f,
			incubationDays = 21,
			acuteDurationDays = 30,
			isChronic = true,
			isPermanent = false,
			healthMod = -0.3f,
			charmMod = -5f,
			scholarshipMod = -3f,
			treatable = true,
			treatmentRecoveryBonus = 0.01f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "heart_disease",
			diseaseName = "心脏病",
			description = "心血管疾病的统称，名称上不区分先天和后天，但描述上需要区分：后天性心脏病（冠心病、心衰等）与肥胖、饮食、年龄、压力相关，中年以后发病；先天性心脏病（见 congenital_heart_defect）出生即有，由遗传和发育异常导致，体力受限、寿命较短。急性发作（心梗、心衰）可致死。",
			category = CharacterDiseaseCategory.Chronic,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.005f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 3,
			isChronic = true,
			isPermanent = false,
			minAgeOnset = 40,
			maxAgeOnset = 80,
			obesityThreshold = 60f,
			healthMod = -0.5f,
			prowessMod = -5f,
			militaryMod = -3f,
			treatable = true,
			treatmentRecoveryBonus = 0f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "stroke",
			diseaseName = "中风",
			description = "急性脑血管事件，可致瘫痪、失语，老年高发",
			category = CharacterDiseaseCategory.Chronic,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.02f,
			baseRecoveryRate = 0.01f,
			acuteDurationDays = 14,
			isChronic = true,
			isPermanent = false,
			minAgeOnset = 50,
			maxAgeOnset = 90,
			obesityThreshold = 60f,
			healthMod = -2f,
			prowessMod = -15f,
			socialMod = -5f,
			scholarshipMod = -5f,
			treatable = true,
			treatmentRecoveryBonus = 0.01f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "diabetes",
			diseaseName = "糖尿病",
			description = "慢性代谢疾病，与肥胖和饮食相关，长期损害健康",
			category = CharacterDiseaseCategory.Chronic,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.001f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 0,
			isChronic = true,
			isPermanent = true,
			minAgeOnset = 30,
			maxAgeOnset = 80,
			obesityThreshold = 70f,
			healthMod = -0.3f,
			fertilityMod = -5f,
			treatable = true,
			treatmentRecoveryBonus = 0f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "gout",
			diseaseName = "痛风",
			description = "代谢性关节炎，与饮食和肥胖相关，反复发作",
			category = CharacterDiseaseCategory.Chronic,
			transmission = TransmissionType.None,
			baseMortalityRate = 0f,
			baseRecoveryRate = 0.02f,
			acuteDurationDays = 5,
			isChronic = true,
			isPermanent = false,
			minAgeOnset = 30,
			maxAgeOnset = 80,
			obesityThreshold = 50f,
			healthMod = -0.2f,
			prowessMod = -3f,
			treatable = true,
			treatmentRecoveryBonus = 0.03f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "arthritis",
			diseaseName = "关节炎",
			description = "慢性关节炎症，老年高发，影响行动和战斗",
			category = CharacterDiseaseCategory.Chronic,
			transmission = TransmissionType.None,
			baseMortalityRate = 0f,
			baseRecoveryRate = 0.005f,
			acuteDurationDays = 0,
			isChronic = true,
			isPermanent = true,
			minAgeOnset = 40,
			maxAgeOnset = 90,
			healthMod = -0.1f,
			prowessMod = -5f,
			militaryMod = -2f,
			treatable = true,
			treatmentRecoveryBonus = 0.01f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "asthma",
			diseaseName = "哮喘",
			description = "慢性呼吸道疾病，反复发作，影响体力",
			category = CharacterDiseaseCategory.Chronic,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.001f,
			baseRecoveryRate = 0.01f,
			acuteDurationDays = 3,
			isChronic = true,
			isPermanent = false,
			minAgeOnset = 5,
			maxAgeOnset = 60,
			healthMod = -0.3f,
			prowessMod = -4f,
			treatable = true,
			treatmentRecoveryBonus = 0.02f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "epilepsy",
			diseaseName = "癫痫",
			description = "神经系统疾病，反复发作抽搐，可能遗传",
			category = CharacterDiseaseCategory.Chronic,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.002f,
			baseRecoveryRate = 0.005f,
			acuteDurationDays = 1,
			isChronic = true,
			isPermanent = true,
			minAgeOnset = 5,
			maxAgeOnset = 50,
			healthMod = -0.2f,
			prowessMod = -3f,
			scholarshipMod = -2f,
			socialMod = -3f,
			treatable = true,
			treatmentRecoveryBonus = 0.01f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "wound_infection",
			diseaseName = "伤口感染",
			description = "战斗或意外受伤后伤口感染，前现代致死率高",
			category = CharacterDiseaseCategory.Injury,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.01f,
			baseRecoveryRate = 0.03f,
			acuteDurationDays = 14,
			isChronic = false,
			isPermanent = false,
			combatInjury = true,
			accidentInjury = true,
			healthMod = -3f,
			prowessMod = -10f,
			treatable = true,
			treatmentRecoveryBonus = 0.05f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "fracture",
			diseaseName = "骨折",
			description = "战斗或意外导致的骨折，影响行动和战斗",
			category = CharacterDiseaseCategory.Injury,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.001f,
			baseRecoveryRate = 0.02f,
			acuteDurationDays = 30,
			isChronic = false,
			isPermanent = false,
			combatInjury = true,
			accidentInjury = true,
			healthMod = -1f,
			prowessMod = -15f,
			militaryMod = -5f,
			treatable = true,
			treatmentRecoveryBonus = 0.03f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "blindness",
			diseaseName = "失明",
			description = "视力完全丧失，这是一个最终状态而非独立疾病——可由战斗受伤直接导致，也可由白内障、青光眼、糖尿病并发症、衰老等多种原因逐渐导致。名称上不区分原因，但描述上需要区分：外伤性失明通常是急性的、单侧或双侧；疾病性失明通常是渐进的、双侧的。",
			category = CharacterDiseaseCategory.Injury,
			transmission = TransmissionType.None,
			baseMortalityRate = 0f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 7,
			isChronic = false,
			isPermanent = true,
			combatInjury = true,
			healthMod = -0.5f,
			prowessMod = -20f,
			militaryMod = -10f,
			scholarshipMod = -5f,
			charmMod = -5f,
			treatable = false
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "cataract",
			diseaseName = "白内障",
			description = "晶状体混浊导致的视力逐渐下降。不直接导致失明，而是导致视力渐进性衰退——从轻度模糊到中度下降到重度衰退，最终可能发展为失明。先天性的出生即有，后天性的与衰老、紫外线暴露、糖尿病、外伤相关，老年高发。前现代无法有效治疗。",
			category = CharacterDiseaseCategory.Chronic,
			transmission = TransmissionType.None,
			causesImpairmentId = "vision",
			impairmentProgressionRate = 0.002f,
			initialImpairmentStage = 0,
			baseMortalityRate = 0f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 0,
			isChronic = true,
			isPermanent = true,
			minAgeOnset = 0,
			maxAgeOnset = 90,
			healthMod = -0.05f,
			prowessMod = -2f,
			scholarshipMod = -1f,
			charmMod = -1f,
			treatable = false
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "glaucoma",
			diseaseName = "青光眼",
			description = "眼压升高导致视神经损伤，视力逐渐丧失。导致视力衰退，进展比白内障快——急性发作时可能快速从轻度跳到重度。后天性与遗传、年龄、近视相关，中年以后发病；先天性出生即有或婴幼儿期发病。前现代无法有效治疗。",
			category = CharacterDiseaseCategory.Chronic,
			transmission = TransmissionType.None,
			causesImpairmentId = "vision",
			impairmentProgressionRate = 0.005f,
			initialImpairmentStage = 1,
			baseMortalityRate = 0f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 3,
			isChronic = true,
			isPermanent = true,
			minAgeOnset = 0,
			maxAgeOnset = 90,
			healthMod = -0.1f,
			prowessMod = -4f,
			scholarshipMod = -2f,
			socialMod = -1f,
			treatable = false
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "lameness",
			diseaseName = "跛足",
			description = "战斗受伤或意外导致的腿部残疾，影响行动",
			category = CharacterDiseaseCategory.Injury,
			transmission = TransmissionType.None,
			baseMortalityRate = 0f,
			baseRecoveryRate = 0.005f,
			acuteDurationDays = 30,
			isChronic = false,
			isPermanent = false,
			combatInjury = true,
			accidentInjury = true,
			healthMod = -0.5f,
			prowessMod = -12f,
			militaryMod = -5f,
			treatable = true,
			treatmentRecoveryBonus = 0.01f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "concussion",
			diseaseName = "脑震荡",
			description = "头部受击导致的脑损伤，可能有长期后遗症",
			category = CharacterDiseaseCategory.Injury,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.005f,
			baseRecoveryRate = 0.05f,
			acuteDurationDays = 14,
			isChronic = false,
			isPermanent = false,
			combatInjury = true,
			accidentInjury = true,
			healthMod = -2f,
			prowessMod = -8f,
			scholarshipMod = -5f,
			socialMod = -3f,
			treatable = true,
			treatmentRecoveryBonus = 0.04f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "burns",
			diseaseName = "烧伤",
			description = "火灾或意外导致的烧伤，感染风险高",
			category = CharacterDiseaseCategory.Injury,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.015f,
			baseRecoveryRate = 0.02f,
			acuteDurationDays = 21,
			isChronic = false,
			isPermanent = false,
			combatInjury = false,
			accidentInjury = true,
			healthMod = -3f,
			prowessMod = -8f,
			charmMod = -10f,
			treatable = true,
			treatmentRecoveryBonus = 0.03f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "frostbite",
			diseaseName = "冻伤",
			description = "严寒环境导致的组织损伤，严重时可致截肢",
			category = CharacterDiseaseCategory.Injury,
			transmission = TransmissionType.None,
			baseMortalityRate = 0.003f,
			baseRecoveryRate = 0.03f,
			acuteDurationDays = 14,
			isChronic = false,
			isPermanent = false,
			combatInjury = false,
			accidentInjury = true,
			healthMod = -2f,
			prowessMod = -6f,
			treatable = true,
			treatmentRecoveryBonus = 0.03f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "hemophilia",
			diseaseName = "血友病",
			description = "遗传性凝血障碍，受伤后出血不止，男性高发",
			category = CharacterDiseaseCategory.Genetic,
			transmission = TransmissionType.Perinatal,
			baseMortalityRate = 0.005f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 0,
			isChronic = true,
			isPermanent = true,
			isGenetic = true,
			healthMod = -0.5f,
			prowessMod = -8f,
			treatable = true,
			treatmentRecoveryBonus = 0f,
			requiredInnovation = "medicine"
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "albinism",
			diseaseName = "白化病",
			description = "遗传性色素缺乏，皮肤和毛发白色，畏光",
			category = CharacterDiseaseCategory.Genetic,
			transmission = TransmissionType.Perinatal,
			baseMortalityRate = 0f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 0,
			isChronic = false,
			isPermanent = true,
			isGenetic = true,
			healthMod = -0.2f,
			charmMod = -5f,
			prowessMod = -2f,
			treatable = false
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "dwarfism",
			diseaseName = "侏儒症",
			description = "遗传性身材矮小，智力通常正常",
			category = CharacterDiseaseCategory.Genetic,
			transmission = TransmissionType.Perinatal,
			baseMortalityRate = 0.001f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 0,
			isChronic = false,
			isPermanent = true,
			isGenetic = true,
			healthMod = -0.3f,
			prowessMod = -10f,
			militaryMod = -3f,
			charmMod = -3f,
			treatable = false
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "gigantism",
			diseaseName = "巨人症",
			description = "遗传性身材异常高大，常伴健康问题",
			category = CharacterDiseaseCategory.Genetic,
			transmission = TransmissionType.Perinatal,
			baseMortalityRate = 0.003f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 0,
			isChronic = true,
			isPermanent = true,
			isGenetic = true,
			healthMod = -0.5f,
			prowessMod = 5f,
			militaryMod = 2f,
			charmMod = -2f,
			treatable = false
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "congenital_heart_defect",
			diseaseName = "先天性心脏病",
			description = "出生即有的心脏结构异常，体力受限，寿命较短",
			category = CharacterDiseaseCategory.Genetic,
			transmission = TransmissionType.Perinatal,
			baseMortalityRate = 0.008f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 0,
			isChronic = true,
			isPermanent = true,
			isGenetic = true,
			healthMod = -1f,
			prowessMod = -10f,
			fertilityMod = -5f,
			treatable = false
		});
		RegisterDisease(new CharacterDiseaseDef
		{
			diseaseId = "hunchback",
			diseaseName = "驼背",
			description = "脊柱后凸畸形，可能先天或后天疾病导致",
			category = CharacterDiseaseCategory.Genetic,
			transmission = TransmissionType.Perinatal,
			baseMortalityRate = 0.001f,
			baseRecoveryRate = 0f,
			acuteDurationDays = 0,
			isChronic = false,
			isPermanent = true,
			isGenetic = true,
			healthMod = -0.3f,
			prowessMod = -8f,
			charmMod = -5f,
			treatable = false
		});
	}

	private void RegisterDisease(CharacterDiseaseDef def)
	{
		_diseaseDefs[def.diseaseId] = def;
	}

	public CharacterDiseaseDef? GetDiseaseDef(string diseaseId)
	{
		if (_diseaseDefs.TryGetValue(diseaseId, out var value))
		{
			return value;
		}
		return null;
	}

	public Dictionary<string, CharacterDiseaseDef> GetAllDiseaseDefs()
	{
		return _diseaseDefs;
	}

	public void DailyTick(CharacterData character, int currentDay, int currentYear)
	{
		if (character.activeDiseases == null || character.activeDiseases.Count == 0)
		{
			return;
		}
		for (int num = character.activeDiseases.Count - 1; num >= 0; num--)
		{
			ActiveCharacterDisease activeCharacterDisease = character.activeDiseases[num];
			CharacterDiseaseDef? diseaseDef = GetDiseaseDef(activeCharacterDisease.diseaseId);
			if (!diseaseDef.HasValue)
			{
				continue;
			}
			CharacterDiseaseDef value = diseaseDef.Value;
			activeCharacterDisease.elapsedDays++;
			activeCharacterDisease.stageDays++;
			switch (activeCharacterDisease.stage)
			{
			case DiseaseStage.Incubation:
				if (activeCharacterDisease.stageDays >= value.incubationDays)
				{
					activeCharacterDisease.stage = DiseaseStage.Acute;
					activeCharacterDisease.stageDays = 0;
				}
				break;
			case DiseaseStage.Acute:
			{
				character.health = Mathf.Max(0f, character.health + value.healthMod);
				if (Random.value < value.baseMortalityRate)
				{
					character.health = 0f;
					return;
				}
				if (activeCharacterDisease.stageDays < value.acuteDurationDays)
				{
					break;
				}
				if (value.isChronic)
				{
					activeCharacterDisease.stage = DiseaseStage.Chronic;
					activeCharacterDisease.stageDays = 0;
					break;
				}
				if (value.isPermanent)
				{
					activeCharacterDisease.stage = DiseaseStage.Permanent;
					activeCharacterDisease.stageDays = 0;
					break;
				}
				float num2 = value.baseRecoveryRate;
				if (activeCharacterDisease.isTreated && value.treatable)
				{
					num2 += value.treatmentRecoveryBonus;
				}
				if (Random.value < num2)
				{
					character.activeDiseases.RemoveAt(num);
					continue;
				}
				activeCharacterDisease.stage = DiseaseStage.Recovery;
				activeCharacterDisease.stageDays = 0;
				break;
			}
			case DiseaseStage.Chronic:
				character.health = Mathf.Max(0f, character.health + value.healthMod * 0.3f);
				if (Random.value < 0.02f && value.acuteDurationDays > 0)
				{
					activeCharacterDisease.stage = DiseaseStage.Acute;
					activeCharacterDisease.stageDays = 0;
				}
				if (!value.isPermanent && value.baseRecoveryRate > 0f)
				{
					float num3 = value.baseRecoveryRate * 0.2f;
					if (activeCharacterDisease.isTreated && value.treatable)
					{
						num3 += value.treatmentRecoveryBonus * 0.3f;
					}
					if (Random.value < num3)
					{
						character.activeDiseases.RemoveAt(num);
						continue;
					}
				}
				break;
			case DiseaseStage.Recovery:
				if (activeCharacterDisease.stageDays >= 7)
				{
					character.activeDiseases.RemoveAt(num);
					continue;
				}
				break;
			case DiseaseStage.Permanent:
				character.health = Mathf.Max(0f, character.health + value.healthMod * 0.1f);
				break;
			}
			if (string.IsNullOrEmpty(value.causesImpairmentId) || !(value.impairmentProgressionRate > 0f) || (activeCharacterDisease.stage != DiseaseStage.Chronic && activeCharacterDisease.stage != DiseaseStage.Permanent))
			{
				continue;
			}
			if (character.impairments == null)
			{
				character.impairments = new List<ActiveImpairment>();
			}
			string impId = value.causesImpairmentId;
			int num4 = character.impairments.FindIndex((ActiveImpairment imp) => imp.impairmentId == impId);
			if (num4 >= 0)
			{
				ActiveImpairment value2 = character.impairments[num4];
				if (value.impairmentProgressionRate > value2.progressionRate)
				{
					value2.progressionRate = value.impairmentProgressionRate;
				}
				character.impairments[num4] = value2;
			}
			else
			{
				character.impairments.Add(new ActiveImpairment
				{
					impairmentId = impId,
					stageIndex = value.initialImpairmentStage,
					progressionRate = value.impairmentProgressionRate,
					daysAtCurrentStage = 0,
					cause = activeCharacterDisease.diseaseId,
					isReversible = false
				});
			}
		}
		if (character.impairments == null || character.impairments.Count <= 0)
		{
			return;
		}
		for (int num5 = character.impairments.Count - 1; num5 >= 0; num5--)
		{
			ActiveImpairment value3 = character.impairments[num5];
			value3.daysAtCurrentStage++;
			IImpairmentDef def = ImpairmentRegistry.GetDef(value3.impairmentId);
			if (def != null)
			{
				int num6 = def.Stages.Length - 1;
				if (value3.stageIndex < num6 && value3.progressionRate > 0f)
				{
					float num7 = value3.progressionRate / (float)(value3.stageIndex + 1);
					if (Random.value < num7)
					{
						value3.stageIndex++;
						value3.daysAtCurrentStage = 0;
						if (value3.stageIndex == num6)
						{
							ImpairmentStage stage = def.Stages[num6];
							if (!string.IsNullOrEmpty(stage.permanentTraitId) && !character.activeDiseases.Exists((ActiveCharacterDisease d) => d.diseaseId == stage.permanentTraitId))
							{
								InfectCharacter(character, stage.permanentTraitId);
							}
						}
					}
				}
				character.impairments[num5] = value3;
			}
		}
	}

	public bool InfectCharacter(CharacterData character, string diseaseId, float infectionSource = 1f)
	{
		CharacterDiseaseDef? diseaseDef = GetDiseaseDef(diseaseId);
		if (!diseaseDef.HasValue)
		{
			return false;
		}
		CharacterDiseaseDef value = diseaseDef.Value;
		if (character.activeDiseases.Exists((ActiveCharacterDisease d) => d.diseaseId == diseaseId))
		{
			return false;
		}
		float num = value.baseInfectionRate;
		if (character.individualResistance > 0f)
		{
			num *= 1f - character.individualResistance / 200f;
		}
		if (character.age < 5 || character.age > 60)
		{
			num *= 1.3f;
		}
		if (Random.value < num)
		{
			character.activeDiseases.Add(new ActiveCharacterDisease(diseaseId, infectionSource));
			return true;
		}
		return false;
	}

	public bool CombatInjury(CharacterData character, float severity = 0.5f)
	{
		string[] injuryTypes = new string[5] { "wound_infection", "fracture", "blindness", "lameness", "concussion" };
		float[] array = new float[5] { 0.4f, 0.25f, 0.05f, 0.15f, 0.15f };
		if (severity > 0.7f)
		{
			array[2] = 0.15f;
			array[1] = 0.3f;
		}
		float value = Random.value;
		float num = 0f;
		int i;
		for (i = 0; i < injuryTypes.Length; i++)
		{
			num += array[i];
			if (value < num)
			{
				if (character.activeDiseases.Exists((ActiveCharacterDisease d) => d.diseaseId == injuryTypes[i]))
				{
					break;
				}
				character.activeDiseases.Add(new ActiveCharacterDisease(injuryTypes[i], 3f));
				return true;
			}
		}
		return false;
	}

	public void CheckChronicDiseaseOnset(CharacterData character)
	{
		string[] array = new string[7] { "heart_disease", "stroke", "diabetes", "gout", "arthritis", "asthma", "epilepsy" };
		foreach (string diseaseId in array)
		{
			CharacterDiseaseDef? diseaseDef = GetDiseaseDef(diseaseId);
			if (!diseaseDef.HasValue)
			{
				continue;
			}
			CharacterDiseaseDef value = diseaseDef.Value;
			if (!character.activeDiseases.Exists((ActiveCharacterDisease d) => d.diseaseId == diseaseId) && character.age >= value.minAgeOnset && character.age <= value.maxAgeOnset)
			{
				float num = 0.001f * (float)(character.age - value.minAgeOnset) / 10f;
				if (value.obesityThreshold > 0f && character.obesity > value.obesityThreshold)
				{
					num *= 1.5f;
				}
				if (Random.value < num)
				{
					character.activeDiseases.Add(new ActiveCharacterDisease(diseaseId));
				}
			}
		}
	}

	public (float prowess, float social, float military, float management, float conspiracy, float scholarship, float charm, float fertility) GetDiseaseMods(CharacterData character)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		float num8 = 0f;
		if (character.activeDiseases == null)
		{
			return (prowess: num, social: num2, military: num3, management: num4, conspiracy: num5, scholarship: num6, charm: num7, fertility: num8);
		}
		foreach (ActiveCharacterDisease activeDisease in character.activeDiseases)
		{
			CharacterDiseaseDef? diseaseDef = GetDiseaseDef(activeDisease.diseaseId);
			if (!diseaseDef.HasValue)
			{
				continue;
			}
			CharacterDiseaseDef value = diseaseDef.Value;
			if (activeDisease.stage != DiseaseStage.Incubation)
			{
				float num9 = activeDisease.severity;
				if (activeDisease.stage == DiseaseStage.Chronic)
				{
					num9 *= 0.5f;
				}
				if (activeDisease.stage == DiseaseStage.Recovery)
				{
					num9 *= 0.3f;
				}
				num += value.prowessMod * num9;
				num2 += value.socialMod * num9;
				num3 += value.militaryMod * num9;
				num4 += value.managementMod * num9;
				num5 += value.conspiracyMod * num9;
				num6 += value.scholarshipMod * num9;
				num7 += value.charmMod * num9;
				num8 += value.fertilityMod * num9;
			}
		}
		return (prowess: num, social: num2, military: num3, management: num4, conspiracy: num5, scholarship: num6, charm: num7, fertility: num8);
	}
}
}
