namespace CivilizationEvolution.Core.Events
{
    /// <summary>
    /// A war between realms with different state faiths occurred.
    /// Religion reacts to this fact; warfare does not call religion directly.
    /// </summary>
    public readonly struct FaithConflictTriggeredEvent
    {
        public readonly int FaithA;
        public readonly int FaithB;
        public readonly int Day;

        public FaithConflictTriggeredEvent(int faithA, int faithB, int day)
        {
            FaithA = faithA;
            FaithB = faithB;
            Day = day;
        }
    }
}
