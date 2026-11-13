using System;
using MyriadCreation.Core;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public class CharacterTemplateDef
{
	public string templateId;

	public CharacterRole role;

	public int minAge;

	public int maxAge;

	public float[] statMin = new float[6];

	public float[] statMax = new float[6];

	public float boldnessBias;

	public float compassionBias;

	public float greedBias;

	public float honorBias;

	public float rationalityBias;

	public float vengefulnessBias;

	public float pietyBias;

	public float weight = 1f;

	public string GetName()
	{
		return Localization.Get(templateId + "_name");
	}

	public string GetDescription()
	{
		return Localization.Get(templateId + "_desc");
	}

	public float GetPersonalityBias(PersonalityDimension dim)
	{
		return dim switch
		{
			PersonalityDimension.Boldness => boldnessBias, 
			PersonalityDimension.Compassion => compassionBias, 
			PersonalityDimension.Greed => greedBias, 
			PersonalityDimension.Honor => honorBias, 
			PersonalityDimension.Rationality => rationalityBias, 
			PersonalityDimension.Vengefulness => vengefulnessBias, 
			PersonalityDimension.Piety => pietyBias, 
			_ => 0f, 
		};
	}

	public bool HasStatConstraints()
	{
		for (int i = 0; i < 6; i++)
		{
			if ((statMin != null && statMin[i] > 0f) || (statMax != null && statMax[i] > 0f))
			{
				return true;
			}
		}
		return false;
	}
}
}
