using System.Collections.Generic;
using System;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;



namespace CivilizationEvolution.Simulation.Politics
{
    public static class PoliticalAccessAnalyzer
    {
 /// <summary>计算指定阶层在给定政体下的政治通道 0~1</summary>
        public static float GetAccess(GameEnums.SocialClass cls, GovernmentComposition comp)
        {
            if (comp == null) return 0.2f;

            bool isElectiveSupreme = SupremeSuccessionLevel.IsElective((SupremeSuccession)comp.supremeSuccession.primary);
            bool directDemocracy = (SupremeSuccession)comp.supremeSuccession.primary == SupremeSuccession.ElectiveDirect;
            var central = (CentralInstitution)comp.centralInstitution.primary;
            bool hasAssembly = central == CentralInstitution.Assembly;
            bool hasElders = central == CentralInstitution.EldersCouncil;
            bool hasReligious = central == CentralInstitution.ReligiousCouncil;
            bool hasBureaucracy = central == CentralInstitution.BureaucraticCore;
            var localSucc = (LocalSuccession)comp.localSuccession.primary;
            bool examChannel = localSucc == LocalSuccession.Examination;
            bool charterChannel = localSucc == LocalSuccession.CityCharter;
            bool localElective = localSucc == LocalSuccession.Elected;
            bool hereditarySupreme = (SupremeSuccession)comp.supremeSuccession.primary == SupremeSuccession.Hereditary;
            bool localHereditary = localSucc == LocalSuccession.Hereditary;
 // 两院制含平民院、等级会议含第三等级，对自由民更开放
            bool lowerHouse = hasAssembly &&
                (comp.assemblyComposition == AssemblyComposition.Bicameral || comp.assemblyComposition == AssemblyComposition.Estate);

            switch (cls)
            {
                case GameEnums.SocialClass.Royalty:
 // 君主制下王室通道完整；共和制下无世袭君主，通道弱
                    return hereditarySupreme ? 1f : (isElectiveSupreme ? 0.35f : 0.6f);

                case GameEnums.SocialClass.NobilityClergy:
                {
                    float a = 0.2f;
                    if (hereditarySupreme) a += 0.3f;            // 世袭君主制贵族承统
                    if (localHereditary) a += 0.25f;             // 地方世袭领有
                    if (hasElders) a += 0.3f;                    // 长老议事会=贵族传统
                    if (hasAssembly) a += lowerHouse ? 0.15f : 0.3f; // 议会（贵族院权重高）
                    if (hasReligious) a += 0.25f;                // 宗教会议=教士通道
                    return Mathf.Clamp01(a);
                }

                case GameEnums.SocialClass.MerchantFreeman:
                {
                    float a = 0.15f;
                    if (isElectiveSupreme) a += directDemocracy ? 0.35f : 0.25f; // 选举/公民大会
                    if (hasAssembly) a += lowerHouse ? 0.35f : 0.2f;             // 平民院/等级会议
                    if (examChannel) a += 0.3f;             // 科举=士人入仕
                    if (charterChannel) a += 0.3f;          // 城市特许自治
                    if (localElective) a += 0.2f;           // 地方选举
                    if (hasBureaucracy && examChannel) a += 0.1f; // 官僚中枢+考试=文官通道
                    return Mathf.Clamp01(a);
                }

                case GameEnums.SocialClass.Peasant:
 // 农民通常被排除在政治之外，仅直接民主（部落/公民大会）时有微弱通道
                    return directDemocracy ? 0.45f : 0.12f;

                case GameEnums.SocialClass.Slave:
                    return 0f; // 奴隶无政治通道

                default:
                    return 0.2f;
            }
        }

 /// <summary>一次性计算全部阶层的政治通道（情境采集时调用，避免重复解析）</summary>
        public static Dictionary<GameEnums.SocialClass, float> GetAllAccess(GovernmentComposition comp)
        {
            var result = new Dictionary<GameEnums.SocialClass, float>();
            foreach (GameEnums.SocialClass cls in Enum.GetValues(typeof(GameEnums.SocialClass)))
                result[cls] = GetAccess(cls, comp);
            return result;
        }
    }
}
