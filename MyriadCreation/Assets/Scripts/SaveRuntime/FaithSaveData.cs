using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>
    /// 信仰运行态存档 DTO（SaveVersion 15 起；审计 A P1-3 剩余项）。
    ///
    /// FaithSystem 的内容定义（faithId/faithName/virtues/sins 等）读档后由 ReligionCatalog 在
    /// PrepareLoadedWorld → InitializeFaithSystems 中重建，不入档；这里只存运行态：
    /// 热忱、教会财富/影响力、传教热忱/宽容度、大祭司、教阶/教职、圣地、追随者、地区归附、教际关系。
    ///
    /// JsonUtility 不支持 Dictionary，regionAdherence / faithRelations 拍平为并行数组；
    /// hierarchyTitles（EcclesiasticalTitle 未标 [Serializable]）同样拍平为并行数组。
    /// </summary>
    [Serializable]
    public class FaithSaveData
    {
        public int faithId;

        // G1/G2/G3（SaveVersion 18）：faith 的归属与传承。
        // religionId 必挂（-1 = 旧档/异常，读档时按内容表回填）；
        // denominationId 由 G3 宗派初始化写入并随档保存；
        // lineageFaithId 由 G2 解析并随档保存。
        public int religionId = -1;
        public int denominationId = -1;
        public int lineageFaithId = -1;

        public float fervor = 50f;
        public float churchWealth;
        public float churchInfluence;
        public float missionaryZeal = 0.5f;
        public float tolerance = 0.5f;
        public int highPriestCharacterId = -1;
        public int hierarchyLevel;

        /// <summary>运行时教义覆盖（合一/改宗写入）；空数组 = 沿用内容表。</summary>
        public string[] doctrineIds = new string[0];

        /// <summary>共融期限（妥协写入）；-1 = 无共融约定。</summary>
        public int communionUntilDay = -1;

        public int[] holySiteTileIndices = new int[0];
        public int[] followerCharacterIds = new int[0];

        // regionAdherence：regionId → 归附度；两个数组按下标一一对应。
        public int[] regionAdherenceRegionIds = new int[0];
        public float[] regionAdherenceValues = new float[0];

        // faithRelations：目标 faithId → 关系值；两个数组按下标一一对应。
        public int[] relationFaithIds = new int[0];
        public float[] relationValues = new float[0];

        // hierarchyTitles 拍平；三个数组按下标一一对应。
        public string[] hierarchyTitleNames = new string[0];
        public int[] hierarchyTitleLevels = new int[0];
        public bool[] hierarchyTitleTemporalAppointments = new bool[0];

        /// <summary>从运行时 FaithSystem 捕获运行态（内容定义字段不取）。</summary>
        public static FaithSaveData FromFaith(FaithSystem faith)
        {
            if (faith == null)
                return null;

            var dto = new FaithSaveData
            {
                faithId = faith.faithId,
                religionId = faith.religionId,
                denominationId = faith.denominationId,
                lineageFaithId = faith.lineageFaithId,
                fervor = faith.fervor,
                churchWealth = faith.churchWealth,
                churchInfluence = faith.churchInfluence,
                missionaryZeal = faith.missionaryZeal,
                tolerance = faith.tolerance,
                highPriestCharacterId = faith.highPriestCharacterId,
                hierarchyLevel = faith.hierarchyLevel,
                doctrineIds = faith.doctrineIds != null ? new List<string>(faith.doctrineIds).ToArray() : new string[0],
                communionUntilDay = faith.communionUntilDay,
                holySiteTileIndices = faith.holySiteTileIndices != null
                    ? faith.holySiteTileIndices.ToArray()
                    : new int[0],
                followerCharacterIds = faith.followerCharacterIds != null
                    ? faith.followerCharacterIds.ToArray()
                    : new int[0]
            };

            if (faith.regionAdherence != null && faith.regionAdherence.Count > 0)
            {
                int n = faith.regionAdherence.Count;
                dto.regionAdherenceRegionIds = new int[n];
                dto.regionAdherenceValues = new float[n];
                int i = 0;
                foreach (var pair in faith.regionAdherence)
                {
                    dto.regionAdherenceRegionIds[i] = pair.Key;
                    dto.regionAdherenceValues[i] = pair.Value;
                    i++;
                }
            }

            if (faith.faithRelations != null && faith.faithRelations.Count > 0)
            {
                int n = faith.faithRelations.Count;
                dto.relationFaithIds = new int[n];
                dto.relationValues = new float[n];
                int i = 0;
                foreach (var pair in faith.faithRelations)
                {
                    dto.relationFaithIds[i] = pair.Key;
                    dto.relationValues[i] = pair.Value;
                    i++;
                }
            }

            if (faith.hierarchyTitles != null && faith.hierarchyTitles.Count > 0)
            {
                int n = faith.hierarchyTitles.Count;
                dto.hierarchyTitleNames = new string[n];
                dto.hierarchyTitleLevels = new int[n];
                dto.hierarchyTitleTemporalAppointments = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    var title = faith.hierarchyTitles[i];
                    dto.hierarchyTitleNames[i] = title?.titleName ?? "";
                    dto.hierarchyTitleLevels[i] = title != null ? title.level : 0;
                    dto.hierarchyTitleTemporalAppointments[i] = title != null && title.temporalAppointment;
                }
            }

            return dto;
        }

        /// <summary>
        /// 把运行态覆盖到读档时按内容表重建出的 FaithSystem（只动运行态，faithName/virtues/sins 等内容字段不碰）。
        /// 数组字段容忍 null / 长度不一致（旧 DTO 或手工改档），按最短长度恢复。
        /// </summary>
        public void ApplyTo(FaithSystem faith)
        {
            if (faith == null)
                return;

            faith.fervor = fervor;
            // 旧档（v17-）没有这三字段，读出来是 -1/0：只在新值有效时覆盖，
            // 避免把内容表回填好的 religionId 冲掉。
            if (religionId > 0) faith.religionId = religionId;
            if (denominationId != 0) faith.denominationId = denominationId;
            if (lineageFaithId != 0) faith.lineageFaithId = lineageFaithId;
            faith.churchWealth = churchWealth;
            faith.churchInfluence = churchInfluence;
            faith.missionaryZeal = missionaryZeal;
            faith.tolerance = tolerance;
            faith.highPriestCharacterId = highPriestCharacterId;
            faith.hierarchyLevel = hierarchyLevel;
            if (faith.doctrineIds == null)
                faith.doctrineIds = new List<string>();
            faith.doctrineIds.Clear();
            if (doctrineIds != null)
                faith.doctrineIds.AddRange(doctrineIds);
            faith.communionUntilDay = communionUntilDay;

            if (faith.holySiteTileIndices == null)
                faith.holySiteTileIndices = new List<int>();
            faith.holySiteTileIndices.Clear();
            if (holySiteTileIndices != null)
                faith.holySiteTileIndices.AddRange(holySiteTileIndices);

            if (faith.followerCharacterIds == null)
                faith.followerCharacterIds = new List<int>();
            faith.followerCharacterIds.Clear();
            if (followerCharacterIds != null)
                faith.followerCharacterIds.AddRange(followerCharacterIds);

            if (faith.regionAdherence == null)
                faith.regionAdherence = new Dictionary<int, float>();
            faith.regionAdherence.Clear();
            if (regionAdherenceRegionIds != null && regionAdherenceValues != null)
            {
                int count = Math.Min(regionAdherenceRegionIds.Length, regionAdherenceValues.Length);
                for (int i = 0; i < count; i++)
                    faith.regionAdherence[regionAdherenceRegionIds[i]] = regionAdherenceValues[i];
            }

            if (faith.faithRelations == null)
                faith.faithRelations = new Dictionary<int, float>();
            faith.faithRelations.Clear();
            if (relationFaithIds != null && relationValues != null)
            {
                int count = Math.Min(relationFaithIds.Length, relationValues.Length);
                for (int i = 0; i < count; i++)
                    faith.faithRelations[relationFaithIds[i]] = relationValues[i];
            }

            if (faith.hierarchyTitles == null)
                faith.hierarchyTitles = new List<EcclesiasticalTitle>();
            faith.hierarchyTitles.Clear();
            if (hierarchyTitleNames != null
                && hierarchyTitleLevels != null
                && hierarchyTitleTemporalAppointments != null)
            {
                int count = Math.Min(hierarchyTitleNames.Length,
                    Math.Min(hierarchyTitleLevels.Length, hierarchyTitleTemporalAppointments.Length));
                for (int i = 0; i < count; i++)
                {
                    faith.hierarchyTitles.Add(new EcclesiasticalTitle
                    {
                        titleName = hierarchyTitleNames[i] ?? "",
                        level = hierarchyTitleLevels[i],
                        temporalAppointment = hierarchyTitleTemporalAppointments[i]
                    });
                }
            }
        }
    }
}
