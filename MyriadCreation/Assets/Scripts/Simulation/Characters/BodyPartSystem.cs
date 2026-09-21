using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 身体部位系统
    /// 负责：初始化部位与子结构、每日更新病因数据进展、汇总计算HP和功能水平、简单诊断机制
    ///
    /// 核心设计：
    /// - 每个宏观部位下有若干子结构，子结构的定量数据（condition 0-100）就是病因数据
    /// - 病因数据在后台模拟，不直接显示
    /// - 病名（白内障/青光眼等）是子结构病变达到阈值后的诊断结果
    /// - 部位HP和功能水平由子结构汇总计算
    /// - 属性惩罚由部位功能水平自动计算（不需要独立的"失明""失聪"特质）
    /// </summary>
    public static class BodyPartSystem
    {
        // ===== 部位 → 子结构映射表 =====
        // 定义每个宏观部位有哪些子结构
        private static readonly Dictionary<BodyPartType, SubStructureType[]> PartToSubStructures = new()
        {
            // 眼部（左右眼相同子结构）
            { BodyPartType.LeftEye, new[] { SubStructureType.EyeLens, SubStructureType.EyeAqueous, SubStructureType.EyeRetina, SubStructureType.EyeCornea } },
            { BodyPartType.RightEye, new[] { SubStructureType.EyeLens, SubStructureType.EyeAqueous, SubStructureType.EyeRetina, SubStructureType.EyeCornea } },

            // 耳部（左右耳相同子结构）
            { BodyPartType.LeftEar, new[] { SubStructureType.EarDrum, SubStructureType.EarInner } },
            { BodyPartType.RightEar, new[] { SubStructureType.EarDrum, SubStructureType.EarInner } },

            // 大脑
            { BodyPartType.Brain, new[] { SubStructureType.BrainCognition, SubStructureType.BrainVessel, SubStructureType.BrainMental } },

            // 牙齿
            { BodyPartType.Teeth, new[] { SubStructureType.ToothBody, SubStructureType.ToothGum } },

            // 头发（外观为主）
            { BodyPartType.Hair, new[] { SubStructureType.HairFollicle } },

            // 面部
            { BodyPartType.Face, new[] { SubStructureType.FaceSkin } },

            // 颅骨
            { BodyPartType.Skull, new[] { SubStructureType.SkullBone } },

            // 心脏
            { BodyPartType.Heart, new[] { SubStructureType.HeartMyocardium, SubStructureType.HeartValve, SubStructureType.HeartCoronary } },

            // 肺部（左右肺相同子结构）
            { BodyPartType.LeftLung, new[] { SubStructureType.LungTissue } },
            { BodyPartType.RightLung, new[] { SubStructureType.LungTissue } },

            // 腹部内脏
            { BodyPartType.Abdomen, new[] { SubStructureType.AbdomenDigestive, SubStructureType.AbdomenMetabolic, SubStructureType.AbdomenLiverKidney } },

            // 脊柱
            { BodyPartType.Spine, new[] { SubStructureType.SpineVertebra } },

            // 皮肤（全身）
            { BodyPartType.Skin, new[] { SubStructureType.SkinTissue } },

            // 关节（肩/髋/膝相同子结构）
            { BodyPartType.LeftShoulder, new[] { SubStructureType.JointCartilage, SubStructureType.JointSynovium, SubStructureType.JointLigament } },
            { BodyPartType.RightShoulder, new[] { SubStructureType.JointCartilage, SubStructureType.JointSynovium, SubStructureType.JointLigament } },
            { BodyPartType.LeftHip, new[] { SubStructureType.JointCartilage, SubStructureType.JointSynovium, SubStructureType.JointLigament } },
            { BodyPartType.RightHip, new[] { SubStructureType.JointCartilage, SubStructureType.JointSynovium, SubStructureType.JointLigament } },
            { BodyPartType.LeftKnee, new[] { SubStructureType.JointCartilage, SubStructureType.JointSynovium, SubStructureType.JointLigament } },
            { BodyPartType.RightKnee, new[] { SubStructureType.JointCartilage, SubStructureType.JointSynovium, SubStructureType.JointLigament } },

            // 骨骼（手臂/腿）
            { BodyPartType.LeftArmBone, new[] { SubStructureType.BoneTissue } },
            { BodyPartType.RightArmBone, new[] { SubStructureType.BoneTissue } },
            { BodyPartType.LeftLegBone, new[] { SubStructureType.BoneTissue } },
            { BodyPartType.RightLegBone, new[] { SubStructureType.BoneTissue } },

            // 肌肉（手臂/腿）
            { BodyPartType.LeftArmMuscle, new[] { SubStructureType.MuscleFiber } },
            { BodyPartType.RightArmMuscle, new[] { SubStructureType.MuscleFiber } },
            { BodyPartType.LeftLegMuscle, new[] { SubStructureType.MuscleFiber } },
            { BodyPartType.RightLegMuscle, new[] { SubStructureType.MuscleFiber } },

            // 手/脚
            { BodyPartType.LeftHand, new[] { SubStructureType.Extremity } },
            { BodyPartType.RightHand, new[] { SubStructureType.Extremity } },
            { BodyPartType.LeftFoot, new[] { SubStructureType.Extremity } },
            { BodyPartType.RightFoot, new[] { SubStructureType.Extremity } },
        };

        // ===== 子结构 → 病名映射表（诊断阈值 + 病名）=====
        // 当子结构condition达到阈值，经过诊断后显示对应病名
        private static readonly Dictionary<SubStructureType, (float threshold, string conditionId, string displayName)> SubStructureToCondition = new()
        {
            // 眼部
            { SubStructureType.EyeLens, (30f, "cataract", "白内障") },
            { SubStructureType.EyeAqueous, (40f, "glaucoma", "青光眼") },
            { SubStructureType.EyeRetina, (35f, "retinopathy", "视网膜病变") },
            { SubStructureType.EyeCornea, (30f, "corneal_opacity", "角膜混浊") },

            // 耳部
            { SubStructureType.EarDrum, (30f, "otitis_media", "中耳炎") },
            { SubStructureType.EarInner, (40f, "sensorineural_hearing_loss", "神经性耳聋") },

            // 大脑
            { SubStructureType.BrainCognition, (50f, "dementia", "痴呆") },
            { SubStructureType.BrainVessel, (60f, "stroke", "中风") },
            { SubStructureType.BrainMental, (40f, "mental_disorder", "精神障碍") },

            // 牙齿
            { SubStructureType.ToothBody, (30f, "dental_caries", "龋齿") },
            { SubStructureType.ToothGum, (30f, "periodontitis", "牙周炎") },

            // 头发
            { SubStructureType.HairFollicle, (50f, "baldness", "秃顶") },

            // 心脏
            { SubStructureType.HeartMyocardium, (40f, "heart_disease", "心脏病") },
            { SubStructureType.HeartValve, (40f, "valve_disease", "瓣膜病") },
            { SubStructureType.HeartCoronary, (50f, "coronary_heart_disease", "冠心病") },

            // 肺部
            { SubStructureType.LungTissue, (30f, "lung_infection", "肺部感染") },

            // 腹部
            { SubStructureType.AbdomenDigestive, (30f, "digestive_disorder", "消化系统疾病") },
            { SubStructureType.AbdomenMetabolic, (40f, "metabolic_disorder", "代谢疾病") },
            { SubStructureType.AbdomenLiverKidney, (50f, "organ_failure", "肝肾衰竭") },

            // 脊柱
            { SubStructureType.SpineVertebra, (40f, "spine_disorder", "脊柱病变") },

            // 皮肤
            { SubStructureType.SkinTissue, (30f, "skin_disorder", "皮肤疾病") },

            // 关节
            { SubStructureType.JointCartilage, (40f, "osteoarthritis", "骨关节炎") },
            { SubStructureType.JointSynovium, (40f, "rheumatoid_arthritis", "类风湿关节炎") },
            { SubStructureType.JointLigament, (30f, "ligament_injury", "韧带损伤") },

            // 骨骼
            { SubStructureType.BoneTissue, (50f, "fracture", "骨折") },

            // 肌肉
            { SubStructureType.MuscleFiber, (30f, "muscle_injury", "肌肉损伤") },

            // 手/脚
            { SubStructureType.Extremity, (30f, "extremity_injury", "肢体损伤") },
        };

        // ===== 诊断所需天数 =====
        private const int DiagnosisDays = 30;  // 子结构condition达到阈值后，30天自动诊断

        /// <summary>
        /// 初始化角色的所有身体部位
        /// </summary>
        public static Dictionary<BodyPartType, BodyPartData> InitializeBodyParts()
        {
            var bodyParts = new Dictionary<BodyPartType, BodyPartData>();
            foreach (BodyPartType part in System.Enum.GetValues(typeof(BodyPartType)))
            {
                var partData = new BodyPartData(part);
                if (PartToSubStructures.TryGetValue(part, out var subTypes))
                {
                    foreach (var subType in subTypes)
                    {
                        partData.subStructures.Add(new SubStructureData(subType));
                    }
                }
                bodyParts[part] = partData;
            }
            return bodyParts;
        }

        /// <summary>
        /// 每日更新：子结构病因数据进展 + 汇总计算部位HP和功能水平 + 诊断
        /// </summary>
        public static void DailyUpdate(Dictionary<BodyPartType, BodyPartData> bodyParts, int currentDay)
        {
            if (bodyParts == null) return;

            foreach (var kvp in bodyParts)
            {
                var part = kvp.Value;

                // 1. 更新子结构病因数据进展
                foreach (var sub in part.subStructures)
                {
                    if (sub.progressionRate > 0 && sub.condition < 100f)
                    {
                        sub.condition = UnityEngine.Mathf.Clamp01(sub.condition / 100f + sub.progressionRate) * 100f;
                    }
                }

                // 2. 汇总计算部位HP和功能水平
                RecalculatePartState(ref part);

                // 3. 诊断机制：子结构condition达到阈值后，经过DiagnosisDays自动诊断
                TryDiagnose(ref part, currentDay);

                bodyParts[kvp.Key] = part;
            }
        }

        /// <summary>
        /// 汇总计算部位HP和功能水平（由子结构condition加权平均）
        /// </summary>
        private static void RecalculatePartState(ref BodyPartData part)
        {
            if (part.subStructures == null || part.subStructures.Count == 0)
            {
                part.currentHp = part.maxHp;
                part.functionLevel = 1f;
                part.state = BodyPartState.Healthy;
                return;
            }

            float totalCondition = 0f;
            foreach (var sub in part.subStructures)
            {
                totalCondition += sub.condition;
            }
            float avgCondition = totalCondition / part.subStructures.Count;

            // HP = 100 - 平均病变程度
            part.currentHp = UnityEngine.Mathf.Clamp(part.maxHp - avgCondition, 0f, part.maxHp);

            // 功能水平 = 1 - 平均病变程度/100
            part.functionLevel = UnityEngine.Mathf.Clamp01(1f - avgCondition / 100f);

            // 状态
            if (part.currentHp <= 0f)
                part.state = BodyPartState.Destroyed;
            else if (part.currentHp < part.maxHp * 0.3f)
                part.state = BodyPartState.Critical;
            else if (part.currentHp < part.maxHp)
                part.state = BodyPartState.Injured;
            else
                part.state = BodyPartState.Healthy;
        }

        /// <summary>
        /// 尝试诊断：子结构condition达到阈值后，经过一段时间自动诊断出病名
        /// </summary>
        private static void TryDiagnose(ref BodyPartData part, int currentDay)
        {
            if (part.subStructures == null) return;
            if (part.diagnosedConditions == null)
                part.diagnosedConditions = new List<DiagnosedCondition>();

            foreach (var sub in part.subStructures)
            {
                if (!SubStructureToCondition.TryGetValue(sub.type, out var mapping)) continue;

                // condition达到阈值
                if (sub.condition >= mapping.threshold)
                {
                    // 检查是否已经诊断过
                    bool alreadyDiagnosed = part.diagnosedConditions.Exists(d => d.conditionId == mapping.conditionId);
                    if (alreadyDiagnosed) continue;

                    // 简化诊断：达到阈值后立即诊断（后续可以加诊断进度和医疗加速）
                    part.diagnosedConditions.Add(new DiagnosedCondition
                    {
                        conditionId = mapping.conditionId,
                        displayName = mapping.displayName,
                        sourceSubStructure = sub.type,
                        sourcePart = part.part,
                        diagnosedDay = currentDay,
                        severityAtDiagnosis = sub.condition
                    });
                }
            }
        }

        /// <summary>
        /// 给指定部位的子结构添加病因进展（由疾病/老化/外伤等调用）
        /// </summary>
        public static void AddSubStructureProgression(
            Dictionary<BodyPartType, BodyPartData> bodyParts,
            BodyPartType part, SubStructureType subStructure,
            float progressionRate, string causingFactor)
        {
            if (bodyParts == null || !bodyParts.TryGetValue(part, out var partData)) return;
            if (partData.subStructures == null) return;

            int idx = partData.subStructures.FindIndex(s => s.type == subStructure);
            if (idx < 0) return;

            var sub = partData.subStructures[idx];
            sub.progressionRate = UnityEngine.Mathf.Max(sub.progressionRate, progressionRate);
            if (!string.IsNullOrEmpty(causingFactor) && !sub.causingFactors.Contains(causingFactor))
            {
                sub.causingFactors.Add(causingFactor);
            }
            partData.subStructures[idx] = sub;
            bodyParts[part] = partData;
        }

        /// <summary>
        /// 直接给子结构设置condition值（用于外伤等即时损伤）
        /// </summary>
        public static void SetSubStructureCondition(
            Dictionary<BodyPartType, BodyPartData> bodyParts,
            BodyPartType part, SubStructureType subStructure,
            float condition, string causingFactor)
        {
            if (bodyParts == null || !bodyParts.TryGetValue(part, out var partData)) return;
            if (partData.subStructures == null) return;

            int idx = partData.subStructures.FindIndex(s => s.type == subStructure);
            if (idx < 0) return;

            var sub = partData.subStructures[idx];
            sub.condition = UnityEngine.Mathf.Clamp(condition, 0f, 100f);
            if (!string.IsNullOrEmpty(causingFactor) && !sub.causingFactors.Contains(causingFactor))
            {
                sub.causingFactors.Add(causingFactor);
            }
            partData.subStructures[idx] = sub;

            // 立即重新计算部位状态
            RecalculatePartState(ref partData);
            bodyParts[part] = partData;
        }

        /// <summary>
        /// 获取部位功能水平（0-1）
        /// </summary>
        public static float GetPartFunction(Dictionary<BodyPartType, BodyPartData> bodyParts, BodyPartType part)
        {
            if (bodyParts == null || !bodyParts.TryGetValue(part, out var partData)) return 1f;
            return partData.functionLevel;
        }

        /// <summary>
        /// 获取双眼综合功能水平（用于判断是否失明）
        /// </summary>
        public static float GetVisionFunction(Dictionary<BodyPartType, BodyPartData> bodyParts)
        {
            float left = GetPartFunction(bodyParts, BodyPartType.LeftEye);
            float right = GetPartFunction(bodyParts, BodyPartType.RightEye);
            // 单眼失明不影响整体视力太多，双眼都丧失才是失明
            return UnityEngine.Mathf.Max(left, right) * 0.7f + (left + right) / 2f * 0.3f;
        }

        /// <summary>
        /// 获取双耳综合功能水平（用于判断是否全聋）
        /// </summary>
        public static float GetHearingFunction(Dictionary<BodyPartType, BodyPartData> bodyParts)
        {
            float left = GetPartFunction(bodyParts, BodyPartType.LeftEar);
            float right = GetPartFunction(bodyParts, BodyPartType.RightEar);
            return UnityEngine.Mathf.Max(left, right) * 0.7f + (left + right) / 2f * 0.3f;
        }
    }
}
