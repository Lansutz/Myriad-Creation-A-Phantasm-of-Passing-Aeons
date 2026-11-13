using System;

namespace MyriadCreation.World.Settlement
{
[Serializable]
public sealed class FacilityData
{
	public string facilityId = Guid.NewGuid().ToString("D");

	public FacilityType facilityType;

	public ConstructionState constructionState;

	public FacilityDamageState damageState;

	public string material = string.Empty;

	public FacilityForm facilityForm = new FacilityForm();

	public float repairProgress;
}
}
