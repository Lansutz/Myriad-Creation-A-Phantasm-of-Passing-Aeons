using System;

namespace MyriadCreation.World.Settlement
{
[Serializable]
public class CityDistrict
{
	public CityDistrictType districtType;

	public string districtName;

	public float areaRatio;

	public float populationRatio;

	public float development;

	public float wealth;

	public float foundedDate;

	public CityDistrict()
	{
		districtType = CityDistrictType.None;
		districtName = "";
		areaRatio = 0f;
		populationRatio = 0f;
		development = 0f;
		wealth = 0f;
		foundedDate = 0f;
	}

	public CityDistrict(CityDistrictType type, string name, float area, float pop, float dev, float wealthVal, float date)
	{
		districtType = type;
		districtName = name;
		areaRatio = area;
		populationRatio = pop;
		development = dev;
		wealth = wealthVal;
		foundedDate = date;
	}
}
}
