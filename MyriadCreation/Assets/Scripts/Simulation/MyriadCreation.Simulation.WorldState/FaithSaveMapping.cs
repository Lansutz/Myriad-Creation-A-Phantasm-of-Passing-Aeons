using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Faith;

namespace MyriadCreation.Simulation.WorldState
{
    /// <summary>
    /// 信仰/宗派运行态入档与还原（A2 迁移：映射器从存档层搬回状态所有者侧）。
    /// Save.Runtime 只保留 DTO 与文件读写，不再直接引用 Faith 程序集。
    /// </summary>
    public static class FaithSaveMapping
    {
        public static List<FaithSaveData> Capture(GameWorld world)
        {
            var result = new List<FaithSaveData>();
            if (world == null || world.FaithSystems == null)
                return result;

            var faiths = new List<FaithSystem>(world.FaithSystems.Count);
            foreach (FaithSystem faith in world.FaithSystems)
            {
                if (faith != null) faiths.Add(faith);
            }
            faiths.Sort((a, b) => a.faithId.CompareTo(b.faithId));

            foreach (FaithSystem faith in faiths)
                result.Add(FromFaith(faith));
            return result;
        }

        public static void Restore(GameWorld world, List<FaithSaveData> saved)
        {
            if (world == null || saved == null || saved.Count == 0)
                return;

            int applied = 0;
            int skipped = 0;
            foreach (FaithSaveData dto in saved)
            {
                if (dto == null) continue;

                FaithSystem faith = world.GetFaithSystem(dto.faithId);
                if (faith == null)
                {
                    skipped++;
                    Debug.LogWarning($"[FaithSaveMapping] 存档中的 faithId={dto.faithId} 在内容表中不存在，跳过恢复");
                    continue;
                }

                ApplyTo(dto, faith);
                applied++;
            }

            Debug.Log($"[FaithSaveMapping] 信仰运行态还原：{applied} 个（跳过 {skipped} 个）");
        }

        public static List<DenominationSaveData> CaptureDenominations(GameWorld world)
        {
            var result = new List<DenominationSaveData>();
            if (world == null || world.Denominations == null)
                return result;

            foreach (Denomination denomination in world.Denominations)
            {
                if (denomination == null) continue;
                result.Add(new DenominationSaveData
                {
                    denominationId = denomination.denominationId,
                    religionId = denomination.religionId,
                    parentDenominationId = denomination.parentDenominationId,
                    denominationName = denomination.denominationName ?? "",
                    isDefault = denomination.isDefault,
                    status = (int)denomination.status,
                    divergence = denomination.divergence,
                    sinceDay = denomination.sinceDay
                });
            }
            result.Sort((a, b) => a.denominationId.CompareTo(b.denominationId));
            return result;
        }

        public static void RestoreDenominations(GameWorld world, List<DenominationSaveData> saved)
        {
            if (world == null)
                return;

            int applied = 0;
            int created = 0;
            if (saved != null)
            {
                foreach (DenominationSaveData dto in saved)
                {
                    if (dto == null) continue;

                    Denomination denomination = world.GetDenomination(dto.denominationId);
                    if (denomination == null)
                    {
                        denomination = new Denomination
                        {
                            denominationId = dto.denominationId,
                            denominationName = dto.denominationName ?? ""
                        };
                        if (!world.RegisterDenomination(denomination))
                        {
                            Debug.LogWarning($"[FaithSaveMapping] 宗派 id={dto.denominationId} 无法注册，跳过恢复");
                            continue;
                        }
                        created++;
                    }

                    denomination.religionId = dto.religionId;
                    denomination.parentDenominationId = dto.parentDenominationId;
                    denomination.isDefault = dto.isDefault;
                    denomination.status = Enum.IsDefined(typeof(DenominationStatus), dto.status)
                        ? (DenominationStatus)dto.status
                        : DenominationStatus.Mainstream;
                    denomination.divergence = dto.divergence;
                    denomination.sinceDay = dto.sinceDay;
                    applied++;
                }
            }

            world.RebuildDenominationMembership();
            Debug.Log($"[FaithSaveMapping] 宗派运行态还原：{applied} 个（新建 {created} 个）");
        }

        public static FaithSaveData FromFaith(FaithSystem faith)
        {
            if (faith == null) return null;

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
                foreach (KeyValuePair<int, float> pair in faith.regionAdherence)
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
                foreach (KeyValuePair<int, float> pair in faith.faithRelations)
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
                    EcclesiasticalTitle title = faith.hierarchyTitles[i];
                    dto.hierarchyTitleNames[i] = title?.titleName ?? "";
                    dto.hierarchyTitleLevels[i] = title != null ? title.level : 0;
                    dto.hierarchyTitleTemporalAppointments[i] = title != null && title.temporalAppointment;
                }
            }

            return dto;
        }

        public static void ApplyTo(FaithSaveData dto, FaithSystem faith)
        {
            if (dto == null || faith == null) return;

            faith.fervor = dto.fervor;
            // 旧档（v17-）没有这三字段，读出来是 -1/0：只在新值有效时覆盖。
            if (dto.religionId > 0) faith.religionId = dto.religionId;
            if (dto.denominationId != 0) faith.denominationId = dto.denominationId;
            if (dto.lineageFaithId != 0) faith.lineageFaithId = dto.lineageFaithId;
            faith.churchWealth = dto.churchWealth;
            faith.churchInfluence = dto.churchInfluence;
            faith.missionaryZeal = dto.missionaryZeal;
            faith.tolerance = dto.tolerance;
            faith.highPriestCharacterId = dto.highPriestCharacterId;
            faith.hierarchyLevel = dto.hierarchyLevel;
            if (faith.doctrineIds == null) faith.doctrineIds = new List<string>();
            faith.doctrineIds.Clear();
            if (dto.doctrineIds != null) faith.doctrineIds.AddRange(dto.doctrineIds);
            faith.communionUntilDay = dto.communionUntilDay;

            if (faith.holySiteTileIndices == null) faith.holySiteTileIndices = new List<int>();
            faith.holySiteTileIndices.Clear();
            if (dto.holySiteTileIndices != null) faith.holySiteTileIndices.AddRange(dto.holySiteTileIndices);

            if (faith.followerCharacterIds == null) faith.followerCharacterIds = new List<int>();
            faith.followerCharacterIds.Clear();
            if (dto.followerCharacterIds != null) faith.followerCharacterIds.AddRange(dto.followerCharacterIds);

            if (faith.regionAdherence == null) faith.regionAdherence = new Dictionary<int, float>();
            faith.regionAdherence.Clear();
            if (dto.regionAdherenceRegionIds != null && dto.regionAdherenceValues != null)
            {
                int count = Math.Min(dto.regionAdherenceRegionIds.Length, dto.regionAdherenceValues.Length);
                for (int i = 0; i < count; i++)
                    faith.regionAdherence[dto.regionAdherenceRegionIds[i]] = dto.regionAdherenceValues[i];
            }

            if (faith.faithRelations == null) faith.faithRelations = new Dictionary<int, float>();
            faith.faithRelations.Clear();
            if (dto.relationFaithIds != null && dto.relationValues != null)
            {
                int count = Math.Min(dto.relationFaithIds.Length, dto.relationValues.Length);
                for (int i = 0; i < count; i++)
                    faith.faithRelations[dto.relationFaithIds[i]] = dto.relationValues[i];
            }

            if (faith.hierarchyTitles == null) faith.hierarchyTitles = new List<EcclesiasticalTitle>();
            faith.hierarchyTitles.Clear();
            if (dto.hierarchyTitleNames != null
                && dto.hierarchyTitleLevels != null
                && dto.hierarchyTitleTemporalAppointments != null)
            {
                int count = Math.Min(dto.hierarchyTitleNames.Length,
                    Math.Min(dto.hierarchyTitleLevels.Length, dto.hierarchyTitleTemporalAppointments.Length));
                for (int i = 0; i < count; i++)
                {
                    faith.hierarchyTitles.Add(new EcclesiasticalTitle
                    {
                        titleName = dto.hierarchyTitleNames[i] ?? "",
                        level = dto.hierarchyTitleLevels[i],
                        temporalAppointment = dto.hierarchyTitleTemporalAppointments[i]
                    });
                }
            }
        }
    }
}
