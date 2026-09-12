using System;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.WorldState;



namespace CivilizationEvolution.Simulation.Society
{
    [Serializable]
    public class ClassProfile
    {
        public GameEnums.SocialClass socialClass;
        public float population;            // 该阶层总人口（跨地块汇总）
        [UnityEngine.Range(0f, 1f)] public float populationShare; // 占政权总人口比例
        [UnityEngine.Range(0f, 100f)] public float satisfaction;   // 需求综合满足度（来自 ClassNeedReport）
        [UnityEngine.Range(0f, 1f)] public float organization;     // 组织化系数
        [UnityEngine.Range(0f, 100f)] public float influence;      // 政治影响力（中性）
        [UnityEngine.Range(0f, 100f)] public float unrest;         // 反对/动荡能量
        [UnityEngine.Range(0f, 100f)] public float support;        // 支持能量
        [UnityEngine.Range(0f, 100f)] public float loyalty;        // 阶层忠诚（=平滑后的 classRelations）
        public ClassNeedDimension chiefGrievance;      // 主要不满维度
        public string chiefGrievanceReason = "";       // 主因文本
        public ClassNeedReport needReport;             // 详细需求报告（UI/AI 用）
    }
}
