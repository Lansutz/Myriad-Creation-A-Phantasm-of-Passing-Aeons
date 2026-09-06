namespace CivilizationEvolution.Diplomacy
{
    [System.Serializable]
    public class Subordination
    {
        public SubordinationType type;
        public int suzerainId;  // 宗主国
        public int vassalId;     // 附庸国
        public int establishedDay;
        public bool isActive = true;

        // 从属条款
        public float tributeAmount = 0f;      // 贡赋金额/年
        public float tributeRatio = 0f;       // 贡赋比例（收入的百分比）
        public bool militaryObligation = false; // 军事义务
        public bool foreignPolicyControl = false; // 外交权控制
        public bool successionControl = false;    // 继承权控制
        public float autonomy = 1f;               // 自治度 0~1

        /// <summary>计算年度贡赋</summary>
        public float CalculateAnnualTribute(float vassalIncome)
        {
            return tributeAmount + vassalIncome * tributeRatio;
        }
    }
}
