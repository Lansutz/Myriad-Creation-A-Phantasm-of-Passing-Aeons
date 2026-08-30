using System;

namespace CivilizationEvolution.Diplomacy
{
    /// <summary>
    /// 战争规则（借鉴《地图上发生的事》WarRules 参数化：
    /// score_*/truce_years/allow_alliance_intervention/allow_vassal_obligation/
    /// allow_white_peace/annual_drift/peace_min_years/pocket_surrender_max_provinces）
    /// 数值为战争系统的全局参数，数据驱动后续可 JSON 化
    /// </summary>
    [Serializable]
    public class WarRules
    {
        // ===== 停战与和平 =====
        /// <summary>停战年限（和平条约后强制休战）</summary>
        public int truceYears = 5;
        /// <summary>允许白和（无条件停战）</summary>
        public bool allowWhitePeace = true;
        /// <summary>白和最低战争分数</summary>
        public float peaceWhiteScore = 0.15f;
        /// <summary>和平后最短维持年限（提前再战需理由）</summary>
        public int peaceMinYears = 2;
        /// <summary>白和/停战达成后和平分数随时间自然增长（每年漂移）</summary>
        public float annualDrift = 0.05f;
        /// <summary>年度漂移上限</summary>
        public float annualDriftLimit = 0.6f;

        // ===== 介入与义务 =====
        /// <summary>允许联盟介入战争（防御同盟义务触发）</summary>
        public bool allowAllianceIntervention = true;
        /// <summary>允许封臣义务（附庸/藩属随宗主参战）</summary>
        public bool allowVassalObligation = true;
        /// <summary>围攻作战模式（0=城市围攻 1=野战为主）</summary>
        public int settlementMode = 0;

        // ===== 战争分数（score 体系） =====
        /// <summary>野战胜利得分</summary>
        public float scoreBattle = 10f;
        /// <summary>野战每名士兵得分</summary>
        public float scoreBattleMenPerPoint = 100f;
        /// <summary>野战最低得分</summary>
        public float scoreBattleMin = 2f;
        /// <summary>攻占首都得分</summary>
        public float scoreCapital = 30f;
        /// <summary>攻占城市得分</summary>
        public float scoreCity = 15f;
        /// <summary>占领省份得分（持续占领）</summary>
        public float scoreProvince = 5f;
        /// <summary>占领小城得分</summary>
        public float scoreSmallCity = 3f;
        /// <summary>围攻占领得分</summary>
        public float scoreOccupationCity = 12f;
        /// <summary>围攻占领省份得分</summary>
        public float scoreOccupationProvince = 4f;
        /// <summary>占领首都得分</summary>
        public float scoreOccupationCapital = 25f;
        /// <summary>力量对比（军力优势方得分加成）</summary>
        public float peaceForceScore = 0.1f;

        // ===== 投降与终结 =====
        /// <summary>小国投降上限（失去省份 ≤ 此数时可能整体投降）</summary>
        public int pocketSurrenderMaxProvinces = 3;

        /// <summary>计算停战到期日（游戏日）</summary>
        public int GetTruceUntilDay(int currentDay, int years)
        {
            return currentDay + years * 365;
        }

        /// <summary>默认规则（数值基准：中世纪战争）</summary>
        public static WarRules Default() => new WarRules();
    }
}
