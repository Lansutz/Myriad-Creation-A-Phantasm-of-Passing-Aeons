using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Faith
{
    /// <summary>宗派状态：主流 / 分裂容忍 / 被判异端（G3 起由 divergence + 正统边界推导，不是手填）。</summary>
    public enum DenominationStatus
    {
        Mainstream = 0,
        Tolerated = 1,
        Heterodox = 2,
    }

    /// <summary>
    /// 宗派挂在 religion 下；无统一组织的 faith 不属于任何宗派。
    /// divergence 由教义、礼仪与戒律差计算，当前 0 表示尚未计算。
    /// </summary>
    [Serializable]
    public class Denomination
    {
        public int denominationId = -1;

        /// <summary>上位 religion（必挂）。</summary>
        public int religionId = -1;

        /// <summary>父宗派（分裂自哪个宗派）；-1 = 由该 religion 直接产生。</summary>
        public int parentDenominationId = -1;

        public string denominationName = "";

        /// <summary>是否为该 religion 的默认宗派（未被分裂时 faith 都挂在它上面）。</summary>
        public bool isDefault;

        public DenominationStatus status = DenominationStatus.Mainstream;

        /// <summary>分歧度 0–100；由教义差算出（WP2 接线），当前恒 0 表示"尚未计算"。</summary>
        public float divergence;

        /// <summary>成立于哪一天（分裂/立宗时写入）。</summary>
        public int sinceDay;

        public List<int> faithIds = new List<int>();
    }
}
