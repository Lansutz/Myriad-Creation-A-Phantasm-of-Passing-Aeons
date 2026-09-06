namespace CivilizationEvolution.Diplomacy
{
    [System.Serializable]
    public struct DiplomaticEvent
    {
        public int day;
        public int year;
        public DiplomaticEventType type;
        public string description;
        public float relationChange;
        public float trustChange;
        public float threatChange;
    }
}
