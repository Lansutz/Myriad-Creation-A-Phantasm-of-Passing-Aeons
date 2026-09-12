namespace CivilizationEvolution.Culture
{
    [System.Serializable]
    public class SaintDef
    {
        public int saintId;
 /// <summary>原型角色（已故美德角色——linkedCharacterId）</summary>
        public int linkedCharacterId = -1;
        public string saintName;
 /// <summary>庇护领域（战争/航海/病患/丰收——主保选择依据）</summary>
        public string domain = "";
 /// <summary>所属教统</summary>
        public int faithId;
 /// <summary>封圣时灵性满足（角色生前虔诚）</summary>
        public float canonizationPiety = 80f;
    }
}
