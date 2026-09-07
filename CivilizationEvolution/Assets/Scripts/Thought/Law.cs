namespace CivilizationEvolution.Thought
{
    [System.Serializable]
    public struct Law
    {
        public int lawId;
        public string lawName;
        public string description;
        public LawCategory category;
        public int enactedDay;
        public bool isActive;
    }
}
