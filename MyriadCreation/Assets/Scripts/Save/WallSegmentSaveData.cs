using System;

namespace MyriadCreation.Save.Contracts
{
    /// <summary>
    /// 城墙横向段存档 DTO（**纯字段**）。
    /// 映射（From/ToSegment/CaptureAll/RestoreAll）在状态所有者侧：
    /// `MyriadCreation.World.Settlement.WallSegmentSaveMapping`。
    /// </summary>
    [Serializable]
    public class WallSegmentSaveData
    {
        public float length;
        public float condition;
        public int towerCount;
        public bool hasGate;
        public int moatLevel;
    }
}
