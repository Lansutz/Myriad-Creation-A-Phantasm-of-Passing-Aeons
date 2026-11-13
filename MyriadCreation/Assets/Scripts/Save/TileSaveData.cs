using System;

namespace MyriadCreation.Save.Contracts
{
    /// <summary>
    /// 地块存档 DTO（纯字段，无方法、无引擎依赖）。
    ///
    /// 架构：本程序集 `MyriadCreation.Save.Contracts` 只放**纯字段 DTO**，引用只有 Core。
    /// 捕获/恢复（`From`/`Restore`）留在状态所有者那一侧（World/Simulation），
    /// 文件 IO 与迁移留在 Infrastructure——这样存档层不再反向依赖模拟层。
    /// </summary>
    [Serializable]
    public struct TileSaveData
    {
        public bool exists;
        public bool isLand;
        public float elevation01;
        public float slopeDegree;
        public float annualTemp;
        public float annualPrecipMm;
        public float airHumidityPct;
        public int biome;
        public int climateZone;
        public float fertility;
        public int provinceId;
        public int ownerRealmId;
        public int occupyingRealmId;
        public bool isCoast;
        public bool isRiver;
        public int riverStreamOrder;
        public float riverFlowAccumulation;
        public float riverWidthMeters;
        public float riverDepthMeters;
        public float riverCurrentMps;
        public int seaConnectId;
        public int oceanTier;
        public int roadLevel;
        public float development;
        public float stability;
        public float order;
    }
}
