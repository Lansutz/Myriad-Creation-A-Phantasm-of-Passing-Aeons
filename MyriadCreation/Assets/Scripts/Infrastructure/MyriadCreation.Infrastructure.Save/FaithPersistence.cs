using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>
    /// 信仰运行态入档与还原（SaveVersion 15 起，审计 A P1-3 剩余项）。
    ///
    /// 读档链路：SaveSystem.ApplyToWorld → GameWorld.PrepareLoadedWorld()
    /// 先按 ReligionCatalog 内容表重建 FaithSystem 骨架（fervor=50/大祭司=-1/空圣地等），
    /// 本类再把存档中的运行态按 faithId overlay 回去。
    /// </summary>
    public static class FaithPersistence
    {
        /// <summary>捕获全部教统运行态，按 faithId 升序（存档稳定、可与内容表对齐）。</summary>
        public static List<FaithSaveData> Capture(GameWorld world)
        {
            var result = new List<FaithSaveData>();
            if (world == null || world.FaithSystems == null)
                return result;

            var faiths = new List<FaithSystem>(world.FaithSystems.Count);
            foreach (var faith in world.FaithSystems)
            {
                if (faith != null)
                    faiths.Add(faith);
            }
            faiths.Sort((a, b) => a.faithId.CompareTo(b.faithId));

            foreach (var faith in faiths)
                result.Add(FaithSaveData.FromFaith(faith));
            return result;
        }

        /// <summary>
        /// 把存档运行态 overlay 到 PrepareLoadedWorld 按内容表重建出的 FaithSystem：
        /// 找不到对应 faithId 的条目跳过并 LogWarning；saved 为 null（v15 之前旧档，无 faiths 段）
        /// 时保持内容重建默认值，不抛异常。
        /// </summary>
        public static void Restore(GameWorld world, List<FaithSaveData> saved)
        {
            if (world == null || saved == null || saved.Count == 0)
                return;

            int applied = 0;
            int skipped = 0;
            foreach (var dto in saved)
            {
                if (dto == null)
                    continue;

                var faith = world.GetFaithSystem(dto.faithId);
                if (faith == null)
                {
                    skipped++;
                    Debug.LogWarning($"[FaithPersistence] 存档中的 faithId={dto.faithId} 在内容表中不存在，跳过恢复");
                    continue;
                }

                dto.ApplyTo(faith);
                applied++;
            }

            Debug.Log($"[FaithPersistence] 信仰运行态还原：{applied} 个（跳过 {skipped} 个）");
        }
    }
}
