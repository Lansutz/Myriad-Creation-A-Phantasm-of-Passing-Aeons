using System;

namespace MyriadCreation.World.Settlement
{
    [Serializable]
    public sealed class FacilityForm
    {
        public string wallShape = string.Empty;
    }

    [Serializable]
    public sealed class FacilityData
    {
        public string facilityId = Guid.NewGuid().ToString("D");
        public FacilityType facilityType;
        public ConstructionState constructionState;
        public FacilityDamageState damageState;
        public string material = string.Empty;
        public FacilityForm facilityForm = new FacilityForm();

        // 0~100；损毁后随修复推进，满则提升一档 damageState。
        public float repairProgress;
    }
}
