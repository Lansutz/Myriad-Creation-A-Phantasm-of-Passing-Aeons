namespace CivilizationEvolution.Thought
{
    [System.Serializable]
    public struct TrialResult
    {
        public VerdictType verdict;
        public PunishmentType punishment;
        public float severity;
        public float fineAmount;
        public int imprisonmentDays;
    }
}
