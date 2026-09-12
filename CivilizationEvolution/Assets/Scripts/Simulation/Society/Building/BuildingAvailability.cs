using System.Collections.Generic;

namespace CivilizationEvolution.Building
{
    public struct BuildingAvailability
    {
        public bool available;
        public List<string> failedConditions;
        public string summary;

        public static BuildingAvailability Available()
        {
            return new BuildingAvailability { available = true, failedConditions = new List<string>(), summary = "可修建" };
        }

        public static BuildingAvailability Unavailable(List<string> failed)
        {
            return new BuildingAvailability
            {
                available = false,
                failedConditions = failed,
                summary = $"不可修建（{failed.Count}项条件未满足）"
            };
        }
    }
}
