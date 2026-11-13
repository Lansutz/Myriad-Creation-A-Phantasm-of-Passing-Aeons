using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Infrastructure.Save
{
    public static class DenominationPersistence
    {
        public static List<DenominationSaveData> Capture(GameWorld world)
        {
            var result = new List<DenominationSaveData>();
            if (world == null || world.Denominations == null)
                return result;

            foreach (Denomination denomination in world.Denominations)
            {
                if (denomination == null)
                    continue;
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

        public static void Restore(GameWorld world, List<DenominationSaveData> saved)
        {
            if (world == null)
                return;

            int applied = 0;
            int created = 0;
            if (saved != null)
            {
                foreach (DenominationSaveData dto in saved)
                {
                    if (dto == null)
                        continue;

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
                            Debug.LogWarning($"[DenominationPersistence] 宗派 id={dto.denominationId} 无法注册，跳过恢复");
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
            Debug.Log($"[DenominationPersistence] 宗派运行态还原：{applied} 个（新建 {created} 个）");
        }
    }
}
