using System.Collections.Generic;

namespace CivilizationEvolution.War
{
    [System.Serializable]
    public class GreatHolyWarState
    {
        public int warId;
        public int faithId;          // 发起的教统
        public int callerRealmId;    // 号召者（教宗国/哈里发政权）
        public int targetRealmId;    // 目标政权
        public int targetTile = -1;  // 目标地块（圣地/异教领地——-1=无特定）
        public int startDay;
        public bool ended;
        public bool holySideWon;     // 圣战方胜
        public int beneficiaryId = -1; // 受益人（谈判后选定——继承法线外者）
        public List<int> participants = new List<int>(); // 参战政权（号召加入）
        public List<int> contributors = new List<int>(); // 有贡献的政权（分战利品资格）
 /// <summary>关联的 WarState（战争结算——分数制——圣战方胜→受益人谈判）</summary>        public int linkedWarId = -1;
    }
}
