using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 角色疾病系统——个体层面的疾病、伤病、慢性病、遗传病
    /// 与人口级瘟疫系统（DiseaseSystem）分层但联动：人口瘟疫可以感染角色，角色传染病也可以传播
    /// </summary>
    public class CharacterDiseaseSystem
    {
        private readonly Dictionary<string, CharacterDiseaseDef> _diseaseDefs = new Dictionary<string, CharacterDiseaseDef>();

        public CharacterDiseaseSystem()
        {
            InitializeDiseaseDefs();
        }

        /// <summary>
        /// 初始化内置疾病定义——数据驱动，可由模组覆盖/新增
        /// </summary>
        private void InitializeDiseaseDefs()
        {
            // ===== 传染病（与人口级瘟疫系统联动） =====
            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "plague", diseaseName = "鼠疫", description = "黑死病，经跳蚤和呼吸道传播，致死率极高",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Vector,
                baseInfectionRate = 0.15f, baseMortalityRate = 0.02f, baseRecoveryRate = 0.01f,
                incubationDays = 7, acuteDurationDays = 14, isChronic = false, isPermanent = false,
                healthMod = -3f, prowessMod = -10f, militaryMod = -5f, charmMod = -5f,
                treatable = true, treatmentRecoveryBonus = 0.02f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "smallpox", diseaseName = "天花", description = "经呼吸道传播，传染性极强，幸存者留疤",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Respiratory,
                baseInfectionRate = 0.12f, baseMortalityRate = 0.01f, baseRecoveryRate = 0.02f,
                incubationDays = 12, acuteDurationDays = 18, isChronic = false, isPermanent = false,
                healthMod = -2f, prowessMod = -5f, charmMod = -8f,
                treatable = true, treatmentRecoveryBonus = 0.03f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "cholera", diseaseName = "霍乱", description = "经水源传播，急性腹泻，暴发快",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Waterborne,
                baseInfectionRate = 0.1f, baseMortalityRate = 0.008f, baseRecoveryRate = 0.05f,
                incubationDays = 3, acuteDurationDays = 7, isChronic = false, isPermanent = false,
                healthMod = -4f, prowessMod = -8f,
                treatable = true, treatmentRecoveryBonus = 0.05f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "typhus", diseaseName = "斑疹伤寒", description = "经虱子传播，与战争和拥挤环境相关",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Vector,
                baseInfectionRate = 0.08f, baseMortalityRate = 0.005f, baseRecoveryRate = 0.03f,
                incubationDays = 10, acuteDurationDays = 11, isChronic = false, isPermanent = false,
                healthMod = -2f, prowessMod = -6f, militaryMod = -3f,
                treatable = true, treatmentRecoveryBonus = 0.03f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "malaria", diseaseName = "疟疾", description = "经蚊子传播，地方性疾病，反复发作",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Vector,
                baseInfectionRate = 0.06f, baseMortalityRate = 0.002f, baseRecoveryRate = 0.01f,
                incubationDays = 14, acuteDurationDays = 7, isChronic = true, isPermanent = false,
                healthMod = -1f, prowessMod = -4f, scholarshipMod = -2f,
                treatable = true, treatmentRecoveryBonus = 0.02f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "tuberculosis", diseaseName = "结核病", description = "肺痨，慢性消耗性疾病，经密切接触传播",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Respiratory,
                baseInfectionRate = 0.03f, baseMortalityRate = 0.003f, baseRecoveryRate = 0.005f,
                incubationDays = 90, acuteDurationDays = 30, isChronic = true, isPermanent = false,
                healthMod = -0.5f, prowessMod = -3f, charmMod = -2f,
                treatable = true, treatmentRecoveryBonus = 0.01f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "dysentery", diseaseName = "痢疾", description = "经水源和食物传播，急性腹泻，恢复快",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Waterborne,
                baseInfectionRate = 0.05f, baseMortalityRate = 0.003f, baseRecoveryRate = 0.08f,
                incubationDays = 2, acuteDurationDays = 5, isChronic = false, isPermanent = false,
                healthMod = -3f, prowessMod = -5f,
                treatable = true, treatmentRecoveryBonus = 0.06f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "influenza", diseaseName = "流感", description = "经呼吸道传播，季节性暴发，传染性强但致死率低",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Respiratory,
                baseInfectionRate = 0.2f, baseMortalityRate = 0.001f, baseRecoveryRate = 0.08f,
                incubationDays = 2, acuteDurationDays = 5, isChronic = false, isPermanent = false,
                healthMod = -2f, prowessMod = -4f,
                treatable = true, treatmentRecoveryBonus = 0.05f
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "leprosy", diseaseName = "麻风病", description = "癞病，慢性传染病，皮肤和神经损伤，社会污名严重",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Contact,
                baseInfectionRate = 0.01f, baseMortalityRate = 0.001f, baseRecoveryRate = 0.002f,
                incubationDays = 180, acuteDurationDays = 60, isChronic = true, isPermanent = true,
                healthMod = -0.5f, prowessMod = -5f, charmMod = -15f, socialMod = -10f,
                treatable = false
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "syphilis", diseaseName = "梅毒", description = "经性传播，慢性病程，晚期影响神经系统",
                category = CharacterDiseaseCategory.Infectious, transmission = TransmissionType.Sexual,
                baseInfectionRate = 0.05f, baseMortalityRate = 0.001f, baseRecoveryRate = 0.005f,
                incubationDays = 21, acuteDurationDays = 30, isChronic = true, isPermanent = false,
                healthMod = -0.3f, charmMod = -5f, scholarshipMod = -3f,
                treatable = true, treatmentRecoveryBonus = 0.01f, requiredInnovation = "medicine"
            });

            // ===== 慢性病/老年病 =====
            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "heart_disease", diseaseName = "心脏病", description = "心血管疾病的统称，名称上不区分先天和后天，但描述上需要区分：后天性心脏病（冠心病、心衰等）与肥胖、饮食、年龄、压力相关，中年以后发病；先天性心脏病（见 congenital_heart_defect）出生即有，由遗传和发育异常导致，体力受限、寿命较短。急性发作（心梗、心衰）可致死。",
                category = CharacterDiseaseCategory.Chronic, transmission = TransmissionType.None,
                baseMortalityRate = 0.005f, baseRecoveryRate = 0f,
                acuteDurationDays = 3, isChronic = true, isPermanent = false,
                minAgeOnset = 40, maxAgeOnset = 80, obesityThreshold = 60f,
                healthMod = -0.5f, prowessMod = -5f, militaryMod = -3f,
                treatable = true, treatmentRecoveryBonus = 0f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "stroke", diseaseName = "中风", description = "急性脑血管事件，可致瘫痪、失语，老年高发",
                category = CharacterDiseaseCategory.Chronic, transmission = TransmissionType.None,
                baseMortalityRate = 0.02f, baseRecoveryRate = 0.01f,
                acuteDurationDays = 14, isChronic = true, isPermanent = false,
                minAgeOnset = 50, maxAgeOnset = 90, obesityThreshold = 60f,
                healthMod = -2f, prowessMod = -15f, socialMod = -5f, scholarshipMod = -5f,
                treatable = true, treatmentRecoveryBonus = 0.01f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "diabetes", diseaseName = "糖尿病", description = "慢性代谢疾病，与肥胖和饮食相关，长期损害健康",
                category = CharacterDiseaseCategory.Chronic, transmission = TransmissionType.None,
                baseMortalityRate = 0.001f, baseRecoveryRate = 0f,
                acuteDurationDays = 0, isChronic = true, isPermanent = true,
                minAgeOnset = 30, maxAgeOnset = 80, obesityThreshold = 70f,
                healthMod = -0.3f, fertilityMod = -5f,
                treatable = true, treatmentRecoveryBonus = 0f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "gout", diseaseName = "痛风", description = "代谢性关节炎，与饮食和肥胖相关，反复发作",
                category = CharacterDiseaseCategory.Chronic, transmission = TransmissionType.None,
                baseMortalityRate = 0f, baseRecoveryRate = 0.02f,
                acuteDurationDays = 5, isChronic = true, isPermanent = false,
                minAgeOnset = 30, maxAgeOnset = 80, obesityThreshold = 50f,
                healthMod = -0.2f, prowessMod = -3f,
                treatable = true, treatmentRecoveryBonus = 0.03f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "arthritis", diseaseName = "关节炎", description = "慢性关节炎症，老年高发，影响行动和战斗",
                category = CharacterDiseaseCategory.Chronic, transmission = TransmissionType.None,
                baseMortalityRate = 0f, baseRecoveryRate = 0.005f,
                acuteDurationDays = 0, isChronic = true, isPermanent = true,
                minAgeOnset = 40, maxAgeOnset = 90,
                healthMod = -0.1f, prowessMod = -5f, militaryMod = -2f,
                treatable = true, treatmentRecoveryBonus = 0.01f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "asthma", diseaseName = "哮喘", description = "慢性呼吸道疾病，反复发作，影响体力",
                category = CharacterDiseaseCategory.Chronic, transmission = TransmissionType.None,
                baseMortalityRate = 0.001f, baseRecoveryRate = 0.01f,
                acuteDurationDays = 3, isChronic = true, isPermanent = false,
                minAgeOnset = 5, maxAgeOnset = 60,
                healthMod = -0.3f, prowessMod = -4f,
                treatable = true, treatmentRecoveryBonus = 0.02f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "epilepsy", diseaseName = "癫痫", description = "神经系统疾病，反复发作抽搐，可能遗传",
                category = CharacterDiseaseCategory.Chronic, transmission = TransmissionType.None,
                baseMortalityRate = 0.002f, baseRecoveryRate = 0.005f,
                acuteDurationDays = 1, isChronic = true, isPermanent = true,
                minAgeOnset = 5, maxAgeOnset = 50,
                healthMod = -0.2f, prowessMod = -3f, scholarshipMod = -2f, socialMod = -3f,
                treatable = true, treatmentRecoveryBonus = 0.01f, requiredInnovation = "medicine"
            });

            // ===== 伤病（战斗/意外） =====
            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "wound_infection", diseaseName = "伤口感染", description = "战斗或意外受伤后伤口感染，前现代致死率高",
                category = CharacterDiseaseCategory.Injury, transmission = TransmissionType.None,
                baseMortalityRate = 0.01f, baseRecoveryRate = 0.03f,
                acuteDurationDays = 14, isChronic = false, isPermanent = false,
                combatInjury = true, accidentInjury = true,
                healthMod = -3f, prowessMod = -10f,
                treatable = true, treatmentRecoveryBonus = 0.05f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "fracture", diseaseName = "骨折", description = "战斗或意外导致的骨折，影响行动和战斗",
                category = CharacterDiseaseCategory.Injury, transmission = TransmissionType.None,
                baseMortalityRate = 0.001f, baseRecoveryRate = 0.02f,
                acuteDurationDays = 30, isChronic = false, isPermanent = false,
                combatInjury = true, accidentInjury = true,
                healthMod = -1f, prowessMod = -15f, militaryMod = -5f,
                treatable = true, treatmentRecoveryBonus = 0.03f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "blindness", diseaseName = "失明", description = "视力完全丧失，这是一个最终状态而非独立疾病——可由战斗受伤直接导致，也可由白内障、青光眼、糖尿病并发症、衰老等多种原因逐渐导致。名称上不区分原因，但描述上需要区分：外伤性失明通常是急性的、单侧或双侧；疾病性失明通常是渐进的、双侧的。",
                category = CharacterDiseaseCategory.Injury, transmission = TransmissionType.None,
                baseMortalityRate = 0f, baseRecoveryRate = 0f,
                acuteDurationDays = 7, isChronic = false, isPermanent = true,
                combatInjury = true,
                healthMod = -0.5f, prowessMod = -20f, militaryMod = -10f, scholarshipMod = -5f, charmMod = -5f,
                treatable = false
            });

            // ===== 眼科疾病（可导致失明的渐进性疾病） =====
            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "cataract", diseaseName = "白内障", description = "晶状体混浊导致的视力逐渐下降。不直接导致失明，而是导致视力渐进性衰退——从轻度模糊到中度下降到重度衰退，最终可能发展为失明。先天性的出生即有，后天性的与衰老、紫外线暴露、糖尿病、外伤相关，老年高发。前现代无法有效治疗。",
                category = CharacterDiseaseCategory.Chronic, transmission = TransmissionType.None,
                causesImpairmentId = "vision", impairmentProgressionRate = 0.002f, initialImpairmentStage = 0,
                baseMortalityRate = 0f, baseRecoveryRate = 0f,
                acuteDurationDays = 0, isChronic = true, isPermanent = true,
                minAgeOnset = 0, maxAgeOnset = 90,
                healthMod = -0.05f, prowessMod = -2f, scholarshipMod = -1f, charmMod = -1f,
                treatable = false
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "glaucoma", diseaseName = "青光眼", description = "眼压升高导致视神经损伤，视力逐渐丧失。导致视力衰退，进展比白内障快——急性发作时可能快速从轻度跳到重度。后天性与遗传、年龄、近视相关，中年以后发病；先天性出生即有或婴幼儿期发病。前现代无法有效治疗。",
                category = CharacterDiseaseCategory.Chronic, transmission = TransmissionType.None,
                causesImpairmentId = "vision", impairmentProgressionRate = 0.005f, initialImpairmentStage = 1,
                baseMortalityRate = 0f, baseRecoveryRate = 0f,
                acuteDurationDays = 3, isChronic = true, isPermanent = true,
                minAgeOnset = 0, maxAgeOnset = 90,
                healthMod = -0.1f, prowessMod = -4f, scholarshipMod = -2f, socialMod = -1f,
                treatable = false
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "lameness", diseaseName = "跛足", description = "战斗受伤或意外导致的腿部残疾，影响行动",
                category = CharacterDiseaseCategory.Injury, transmission = TransmissionType.None,
                baseMortalityRate = 0f, baseRecoveryRate = 0.005f,
                acuteDurationDays = 30, isChronic = false, isPermanent = false,
                combatInjury = true, accidentInjury = true,
                healthMod = -0.5f, prowessMod = -12f, militaryMod = -5f,
                treatable = true, treatmentRecoveryBonus = 0.01f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "concussion", diseaseName = "脑震荡", description = "头部受击导致的脑损伤，可能有长期后遗症",
                category = CharacterDiseaseCategory.Injury, transmission = TransmissionType.None,
                baseMortalityRate = 0.005f, baseRecoveryRate = 0.05f,
                acuteDurationDays = 14, isChronic = false, isPermanent = false,
                combatInjury = true, accidentInjury = true,
                healthMod = -2f, prowessMod = -8f, scholarshipMod = -5f, socialMod = -3f,
                treatable = true, treatmentRecoveryBonus = 0.04f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "burns", diseaseName = "烧伤", description = "火灾或意外导致的烧伤，感染风险高",
                category = CharacterDiseaseCategory.Injury, transmission = TransmissionType.None,
                baseMortalityRate = 0.015f, baseRecoveryRate = 0.02f,
                acuteDurationDays = 21, isChronic = false, isPermanent = false,
                combatInjury = false, accidentInjury = true,
                healthMod = -3f, prowessMod = -8f, charmMod = -10f,
                treatable = true, treatmentRecoveryBonus = 0.03f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "frostbite", diseaseName = "冻伤", description = "严寒环境导致的组织损伤，严重时可致截肢",
                category = CharacterDiseaseCategory.Injury, transmission = TransmissionType.None,
                baseMortalityRate = 0.003f, baseRecoveryRate = 0.03f,
                acuteDurationDays = 14, isChronic = false, isPermanent = false,
                combatInjury = false, accidentInjury = true,
                healthMod = -2f, prowessMod = -6f,
                treatable = true, treatmentRecoveryBonus = 0.03f, requiredInnovation = "medicine"
            });

            // ===== 遗传病/先天疾病 =====
            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "hemophilia", diseaseName = "血友病", description = "遗传性凝血障碍，受伤后出血不止，男性高发",
                category = CharacterDiseaseCategory.Genetic, transmission = TransmissionType.Perinatal,
                baseMortalityRate = 0.005f, baseRecoveryRate = 0f,
                acuteDurationDays = 0, isChronic = true, isPermanent = true, isGenetic = true,
                healthMod = -0.5f, prowessMod = -8f,
                treatable = true, treatmentRecoveryBonus = 0f, requiredInnovation = "medicine"
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "albinism", diseaseName = "白化病", description = "遗传性色素缺乏，皮肤和毛发白色，畏光",
                category = CharacterDiseaseCategory.Genetic, transmission = TransmissionType.Perinatal,
                baseMortalityRate = 0f, baseRecoveryRate = 0f,
                acuteDurationDays = 0, isChronic = false, isPermanent = true, isGenetic = true,
                healthMod = -0.2f, charmMod = -5f, prowessMod = -2f,
                treatable = false
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "dwarfism", diseaseName = "侏儒症", description = "遗传性身材矮小，智力通常正常",
                category = CharacterDiseaseCategory.Genetic, transmission = TransmissionType.Perinatal,
                baseMortalityRate = 0.001f, baseRecoveryRate = 0f,
                acuteDurationDays = 0, isChronic = false, isPermanent = true, isGenetic = true,
                healthMod = -0.3f, prowessMod = -10f, militaryMod = -3f, charmMod = -3f,
                treatable = false
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "gigantism", diseaseName = "巨人症", description = "遗传性身材异常高大，常伴健康问题",
                category = CharacterDiseaseCategory.Genetic, transmission = TransmissionType.Perinatal,
                baseMortalityRate = 0.003f, baseRecoveryRate = 0f,
                acuteDurationDays = 0, isChronic = true, isPermanent = true, isGenetic = true,
                healthMod = -0.5f, prowessMod = 5f, militaryMod = 2f, charmMod = -2f,
                treatable = false
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "congenital_heart_defect", diseaseName = "先天性心脏病", description = "出生即有的心脏结构异常，体力受限，寿命较短",
                category = CharacterDiseaseCategory.Genetic, transmission = TransmissionType.Perinatal,
                baseMortalityRate = 0.008f, baseRecoveryRate = 0f,
                acuteDurationDays = 0, isChronic = true, isPermanent = true, isGenetic = true,
                healthMod = -1f, prowessMod = -10f, fertilityMod = -5f,
                treatable = false
            });

            RegisterDisease(new CharacterDiseaseDef
            {
                diseaseId = "hunchback", diseaseName = "驼背", description = "脊柱后凸畸形，可能先天或后天疾病导致",
                category = CharacterDiseaseCategory.Genetic, transmission = TransmissionType.Perinatal,
                baseMortalityRate = 0.001f, baseRecoveryRate = 0f,
                acuteDurationDays = 0, isChronic = false, isPermanent = true, isGenetic = true,
                healthMod = -0.3f, prowessMod = -8f, charmMod = -5f,
                treatable = false
            });
        }

        private void RegisterDisease(CharacterDiseaseDef def)
        {
            _diseaseDefs[def.diseaseId] = def;
        }

        public CharacterDiseaseDef? GetDiseaseDef(string diseaseId)
        {
            if (_diseaseDefs.TryGetValue(diseaseId, out var def))
                return def;
            return null;
        }

        public Dictionary<string, CharacterDiseaseDef> GetAllDiseaseDefs()
        {
            return _diseaseDefs;
        }

        /// <summary>
        /// 每日疾病Tick——处理疾病阶段推进、健康扣减、恢复、死亡判定
        /// </summary>
        public void DailyTick(CharacterData character, int currentDay, int currentYear)
        {
            if (character.activeDiseases == null || character.activeDiseases.Count == 0)
                return;

            for (int i = character.activeDiseases.Count - 1; i >= 0; i--)
            {
                var disease = character.activeDiseases[i];
                var defOpt = GetDiseaseDef(disease.diseaseId);
                if (defOpt == null) continue;
                var def = defOpt.Value;

                disease.elapsedDays++;
                disease.stageDays++;

                // 阶段推进
                switch (disease.stage)
                {
                    case DiseaseStage.Incubation:
                        if (disease.stageDays >= def.incubationDays)
                        {
                            disease.stage = DiseaseStage.Acute;
                            disease.stageDays = 0;
                        }
                        break;

                    case DiseaseStage.Acute:
                        // 急性期扣健康
                        character.health = Mathf.Max(0, character.health + def.healthMod);

                        // 死亡判定
                        if (Random.value < def.baseMortalityRate)
                        {
                            character.health = 0;
                            return;
                        }

                        // 急性期结束
                        if (disease.stageDays >= def.acuteDurationDays)
                        {
                            if (def.isChronic)
                            {
                                disease.stage = DiseaseStage.Chronic;
                                disease.stageDays = 0;
                            }
                            else if (def.isPermanent)
                            {
                                disease.stage = DiseaseStage.Permanent;
                                disease.stageDays = 0;
                            }
                            else
                            {
                                // 恢复判定
                                float recoveryRate = def.baseRecoveryRate;
                                if (disease.isTreated && def.treatable)
                                    recoveryRate += def.treatmentRecoveryBonus;

                                if (Random.value < recoveryRate)
                                {
                                    character.activeDiseases.RemoveAt(i);
                                    continue;
                                }
                                else
                                {
                                    disease.stage = DiseaseStage.Recovery;
                                    disease.stageDays = 0;
                                }
                            }
                        }
                        break;

                    case DiseaseStage.Chronic:
                        // 慢性期轻微扣健康
                        character.health = Mathf.Max(0, character.health + def.healthMod * 0.3f);

                        // 慢性期急性发作
                        if (Random.value < 0.02f && def.acuteDurationDays > 0)
                        {
                            disease.stage = DiseaseStage.Acute;
                            disease.stageDays = 0;
                        }

                        // 慢性期恢复（部分慢性病可恢复）
                        if (!def.isPermanent && def.baseRecoveryRate > 0)
                        {
                            float recoveryRate = def.baseRecoveryRate * 0.2f;
                            if (disease.isTreated && def.treatable)
                                recoveryRate += def.treatmentRecoveryBonus * 0.3f;

                            if (Random.value < recoveryRate)
                            {
                                character.activeDiseases.RemoveAt(i);
                                continue;
                            }
                        }
                        break;

                    case DiseaseStage.Recovery:
                        // 恢复期逐渐恢复
                        if (disease.stageDays >= 7)
                        {
                            character.activeDiseases.RemoveAt(i);
                            continue;
                        }
                        break;

                    case DiseaseStage.Permanent:
                        // 永久期不恢复，持续影响
                        character.health = Mathf.Max(0, character.health + def.healthMod * 0.1f);
                        break;
                }

                // 疾病导致的感官/能力衰退
                if (!string.IsNullOrEmpty(def.causesImpairmentId) && def.impairmentProgressionRate > 0)
                {
                    // 慢性期和永久期持续导致衰退进展
                    if (disease.stage == DiseaseStage.Chronic || disease.stage == DiseaseStage.Permanent)
                    {
                        if (character.impairments == null)
                            character.impairments = new System.Collections.Generic.List<ActiveImpairment>();

                        string impId = def.causesImpairmentId;
                        int existingIndex = character.impairments.FindIndex(imp => imp.impairmentId == impId);

                        if (existingIndex >= 0)
                        {
                            var imp = character.impairments[existingIndex];
                            // 取较快的进展速度
                            if (def.impairmentProgressionRate > imp.progressionRate)
                                imp.progressionRate = def.impairmentProgressionRate;
                            character.impairments[existingIndex] = imp;
                        }
                        else
                        {
                            character.impairments.Add(new ActiveImpairment
                            {
                                impairmentId = impId,
                                stageIndex = def.initialImpairmentStage,
                                progressionRate = def.impairmentProgressionRate,
                                daysAtCurrentStage = 0,
                                cause = disease.diseaseId,
                                isReversible = false
                            });
                        }
                    }
                }
            }

            // 处理衰退进展
            if (character.impairments != null && character.impairments.Count > 0)
            {
                for (int i = character.impairments.Count - 1; i >= 0; i--)
                {
                    var imp = character.impairments[i];
                    imp.daysAtCurrentStage++;

                    var impDef = ImpairmentRegistry.GetDef(imp.impairmentId);
                    if (impDef == null) continue;

                    int maxStage = impDef.Stages.Length - 1;

                    // 检查是否进展到下一阶段
                    if (imp.stageIndex < maxStage && imp.progressionRate > 0)
                    {
                        // 进展概率受阶段影响——阶段越高越难进展
                        float progressChance = imp.progressionRate / (imp.stageIndex + 1);
                        if (UnityEngine.Random.value < progressChance)
                        {
                            imp.stageIndex++;
                            imp.daysAtCurrentStage = 0;

                            // 达到最高阶段，获得永久特质
                            if (imp.stageIndex == maxStage)
                            {
                                var stage = impDef.Stages[maxStage];
                                if (!string.IsNullOrEmpty(stage.permanentTraitId) &&
                                    !character.activeDiseases.Exists(d => d.diseaseId == stage.permanentTraitId))
                                {
                                    InfectCharacter(character, stage.permanentTraitId, "impairment_progression");
                                }
                            }
                        }
                    }

                    character.impairments[i] = imp;
                }
            }
        }



        /// <summary>
        /// 感染疾病——从人口级瘟疫或角色传播
        /// </summary>
        public bool InfectCharacter(CharacterData character, string diseaseId, float infectionSource = 1f)
        {
            var defOpt = GetDiseaseDef(diseaseId);
            if (defOpt == null) return false;
            var def = defOpt.Value;

            // 已经有这个病了，不重复感染
            if (character.activeDiseases.Exists(d => d.diseaseId == diseaseId))
                return false;

            // 个体抵抗力修正
            float infectionRate = def.baseInfectionRate;
            if (character.individualResistance > 0)
                infectionRate *= (1f - character.individualResistance / 200f);

            // 年龄修正（老人和小孩更易感染）
            if (character.age < 5 || character.age > 60)
                infectionRate *= 1.3f;

            if (Random.value < infectionRate)
            {
                character.activeDiseases.Add(new ActiveCharacterDisease(diseaseId, infectionSource));
                return true;
            }
            return false;
        }

        /// <summary>
        /// 战斗受伤判定
        /// </summary>
        public bool CombatInjury(CharacterData character, float severity = 0.5f)
        {
            string[] injuryTypes = { "wound_infection", "fracture", "blindness", "lameness", "concussion" };
            float[] weights = { 0.4f, 0.25f, 0.05f, 0.15f, 0.15f };

            // 根据严重度调整权重
            if (severity > 0.7f)
            {
                weights[2] = 0.15f; // 失明概率上升
                weights[1] = 0.3f;  // 骨折概率上升
            }

            float roll = Random.value;
            float cumulative = 0f;
            for (int i = 0; i < injuryTypes.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                {
                    if (!character.activeDiseases.Exists(d => d.diseaseId == injuryTypes[i]))
                    {
                        character.activeDiseases.Add(new ActiveCharacterDisease(injuryTypes[i], 3f));
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        /// <summary>
        /// 慢性病/老年病发病判定
        /// </summary>
        public void CheckChronicDiseaseOnset(CharacterData character)
        {
            string[] chronicTypes = { "heart_disease", "stroke", "diabetes", "gout", "arthritis", "asthma", "epilepsy" };

            foreach (var diseaseId in chronicTypes)
            {
                var defOpt = GetDiseaseDef(diseaseId);
                if (defOpt == null) continue;
                var def = defOpt.Value;

                // 已经有了
                if (character.activeDiseases.Exists(d => d.diseaseId == diseaseId))
                    continue;

                // 年龄检查
                if (character.age < def.minAgeOnset || character.age > def.maxAgeOnset)
                    continue;

                // 基础发病概率（随年龄上升）
                float onsetRate = 0.001f * (character.age - def.minAgeOnset) / 10f;

                // 肥胖修正
                if (def.obesityThreshold > 0 && character.obesity > def.obesityThreshold)
                    onsetRate *= 1.5f;

                // 遗传修正（简化：有家族史则概率上升）
                // TODO: 家族史检查

                if (Random.value < onsetRate)
                {
                    character.activeDiseases.Add(new ActiveCharacterDisease(diseaseId, 0f));
                }
            }
        }

        /// <summary>
        /// 获取疾病对属性的总修正
        /// </summary>
        public (float prowess, float social, float military, float management, float conspiracy, float scholarship, float charm, float fertility) GetDiseaseMods(CharacterData character)
        {
            float prowess = 0, social = 0, military = 0, management = 0, conspiracy = 0, scholarship = 0, charm = 0, fertility = 0;

            if (character.activeDiseases == null)
                return (prowess, social, military, management, conspiracy, scholarship, charm, fertility);

            foreach (var disease in character.activeDiseases)
            {
                var defOpt = GetDiseaseDef(disease.diseaseId);
                if (defOpt == null) continue;
                var def = defOpt.Value;

                // 潜伏期不生效
                if (disease.stage == DiseaseStage.Incubation)
                    continue;

                float multiplier = disease.severity;
                if (disease.stage == DiseaseStage.Chronic)
                    multiplier *= 0.5f;
                if (disease.stage == DiseaseStage.Recovery)
                    multiplier *= 0.3f;

                prowess += def.prowessMod * multiplier;
                social += def.socialMod * multiplier;
                military += def.militaryMod * multiplier;
                management += def.managementMod * multiplier;
                conspiracy += def.conspiracyMod * multiplier;
                scholarship += def.scholarshipMod * multiplier;
                charm += def.charmMod * multiplier;
                fertility += def.fertilityMod * multiplier;
            }

            return (prowess, social, military, management, conspiracy, scholarship, charm, fertility);
        }
    }
}
