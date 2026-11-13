using System.Collections.Generic;
using MyriadCreation.Save.Contracts;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// `WallSegment` 与存档 DTO 的映射（**状态所有者侧**）。
    ///
    /// 为什么在这里：DTO 是纯数据（放在 `MyriadCreation.Save.Contracts`，只依赖 Core），
    /// 而 `From/CaptureAll/RestoreAll` 必须读写 `WallSegment`——它属于 World 层。
    /// 把映射留在所有者这边，存档层（Infrastructure）就不再需要引用 World/Simulation。
    /// </summary>
    public static class WallSegmentSaveMapping
    {
        public static WallSegmentSaveData From(WallSegment segment)
        {
            if (segment == null) return null;
            return new WallSegmentSaveData
            {
                length = segment.length,
                condition = segment.condition,
                towerCount = segment.towerCount,
                hasGate = segment.hasGate,
                moatLevel = segment.moatLevel
            };
        }

        public static WallSegment ToSegment(WallSegmentSaveData data)
        {
            if (data == null) return null;
            return new WallSegment
            {
                length = data.length,
                condition = data.condition,
                towerCount = data.towerCount,
                hasGate = data.hasGate,
                moatLevel = data.moatLevel
            };
        }

        public static WallSegmentSaveData[] CaptureAll(List<WallSegment> segments)
        {
            if (segments == null || segments.Count == 0) return null;
            var result = new WallSegmentSaveData[segments.Count];
            for (int i = 0; i < segments.Count; i++)
            {
                result[i] = From(segments[i]);
            }
            return result;
        }

        public static void RestoreAll(List<WallSegment> target, WallSegmentSaveData[] data)
        {
            if (target == null) return;
            target.Clear();
            if (data == null) return;
            foreach (var entry in data)
            {
                if (entry != null)
                {
                    target.Add(ToSegment(entry));
                }
            }
        }
    }
}
