using System;

namespace MyriadCreation.Save.Contracts
{
    /// <summary>
    /// 栖居点块树存档 DTO（**纯字段**：区划/市场/城墙/建筑/多道城墙/城层/护城河/角色）。
    /// 映射（From/ToBlock/CaptureAll/RestoreAll，递归）在
    /// `MyriadCreation.World.Settlement.SettlementBlockSaveMapping`。
    /// </summary>
    [Serializable]
    public class SettlementBlockSaveData
    {
        public string block_id;
        public string display_name;

        /// <summary>建造者人物 id；区块名由建造者生成（`SettlementBlockNaming`），存 id 才能重建归属。</summary>
        public int builderCharacterId = -1;
        public int kind;
        public int subtype;
        public bool enabled;
        public int level;
        public float condition;
        public bool hasSpatialData;
        public float areaRatio;
        public float populationRatio;
        public float development;
        public float wealth;
        public float length;
        public int districtType;
        public int buildingDefId;
        public int wallLayerCount;
        public int wallMoatLevel;
        public int wallRole;
        public int wallLayout;
        /// <summary>城墙数据版本：0=旧档（level 是旧 WallLevel），1=新档（level 是 0–15）。</summary>
        public int wallDataVersion;
        public int wallMaterial;
        public int wallMaterialGrade;
        public WallSegmentSaveData[] wallSegments;

        /// <summary>
        /// 扁平化（D30，学 CK3）：父块 id，空串 = 根。新档只用这个指针表达层级，
        /// 不再用嵌套子数组——嵌套会撞 Unity 序列化深度 10，深层数据被静默截断。
        /// </summary>
        public string parent_block_id = "";

        /// <summary>
        /// **旧档兼容字段**：新档恒为 null。读到旧档（嵌套形状）时由映射器递归挂回，
        /// 不再用于写出。详见 `SettlementBlockSaveMapping.RestoreAll`。
        /// </summary>
        public SettlementBlockSaveData[] children;
    }
}
