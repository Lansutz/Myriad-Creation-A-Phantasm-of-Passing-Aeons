using System.Collections.Generic;

namespace CivilizationEvolution.Culture
{
    public class AdminDivision
    {
        public int divisionId;          // 唯一（realmId*10000+序）
        public int realmId;
        public int level;               // 1=政权根——2..N
        public string name = "";
        public int parentDivisionId = -1; // -1=根
        public List<int> childIds = new List<int>();
        public HashSet<int> tiles = new HashSet<int>(); // 辖境地块
        /// <summary>治理头衔（官僚=titleId[郡守…]/分封=领主爵位——TitleDef 键）</summary>
        public string titleId = "";
        /// <summary>治理者（官僚=官员 charId——分封=领主 charId——-1=空缺）</summary>
        public int holderCharacterId = -1;
    }
}
