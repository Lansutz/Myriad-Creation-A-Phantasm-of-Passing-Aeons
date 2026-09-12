namespace CivilizationEvolution.Politics
{
    [System.Serializable]
    public class OfficeTitleEntry
    {
        public string office;      // OfficialOffice 枚举名
        public string polityKey;   // 政体语境键：Kingdom/Empire/Federation/Republic/Tribal/Theocracy
        public string titleKey;    // 本地化键（缺键回退默认称号）
    }
}
