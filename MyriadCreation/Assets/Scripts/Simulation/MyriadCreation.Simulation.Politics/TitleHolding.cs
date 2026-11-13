using System;

namespace MyriadCreation.Simulation.Politics
{
    /// <summary>
    /// 头衔持有（M1：头衔可共有、可机构持有、可指定角色与份额）。
    /// 运行时状态，不进 Content；当前不入档（封闭开发，SaveVersion 未改）。
    /// </summary>
    [Serializable]
    public class TitleHolding
    {
        /// <summary>人物持有者；机构持有时为 -1。</summary>
        public int characterId = -1;

        /// <summary>机构持有者（议会/教会/官职机构）；人物持有时为 -1。</summary>
        public int institutionId = -1;

        public TitleHolderRole role = TitleHolderRole.Sole;

        /// <summary>份额 0-1；Sole/Regent 通常为 1，CoRuler 按约定分配。</summary>
        public float share = 1f;

        public int sinceDay = -1;

        /// <summary>结束日；-1 = 仍在任。</summary>
        public int endDay = -1;

        public float legitimacy = 50f;

        public bool IsInstitutionHolding => institutionId >= 0 && characterId < 0;

        public bool IsActive(int currentDay)
        {
            return endDay < 0 || currentDay < endDay;
        }
    }
}
