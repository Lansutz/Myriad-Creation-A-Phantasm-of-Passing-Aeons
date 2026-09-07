namespace CivilizationEvolution.Thought
{
    [System.Serializable]
    public struct Doctrine
    {
        public string name;
        public string description;
        public DoctrineCategory category;
        public float strictness; // 严格程度 0~1
    }
}
