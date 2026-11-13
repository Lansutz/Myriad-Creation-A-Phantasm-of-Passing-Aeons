using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Genetics
{
public static class GeneticsAppearance
{
	public static PhenotypeAppearance Build(GenomeDef genome, PhenotypeResult phenotype)
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		AppearanceStyleDef appearanceStyleDef = genome?.appearance ?? new AppearanceStyleDef();
		PhenotypeAppearance result = new PhenotypeAppearance
		{
			eyeModel = appearanceStyleDef.eyeModel,
			bloodColor = PigmentColor(appearanceStyleDef, appearanceStyleDef.bloodPigmentId, new Color(0.55f, 0.08f, 0.08f)),
			skinColor = PigmentColor(appearanceStyleDef, appearanceStyleDef.skinBasePigmentId, new Color(0.72f, 0.54f, 0.42f)),
			hairColor = PigmentColor(appearanceStyleDef, appearanceStyleDef.hairBasePigmentId, new Color(0.12f, 0.09f, 0.07f)),
			heightScale = 1f + phenotype.GetAppearance("Height") * 0.05f,
			buildScale = 1f + phenotype.GetAppearance("BodyBuild") * 0.05f,
			skinTexture = ((ValueSign(phenotype.GetAppearance("SkinTexture")) > 0f) ? "coarse" : "fine"),
			hairStructure = ((ValueSign(phenotype.GetAppearance("HairStructure")) > 0f) ? "coarse" : "fine")
		};
		float appearance = phenotype.GetAppearance("SkinPigment");
		result.skinColor = AdjustPigment(result.skinColor, appearance);
		float appearance2 = phenotype.GetAppearance("HairPigment");
		result.hairColor = AdjustPigment(result.hairColor, appearance2);
		result.eyeColor = ComputeEyeColor(appearanceStyleDef, result.bloodColor, PigmentColor(appearanceStyleDef, appearanceStyleDef.eyeBasePigmentId, new Color(0.42f, 0.3f, 0.18f)), phenotype.GetAppearance("EyePigment"), phenotype.GetAppearance("EyeStructure"));
		return result;
	}

	public static Color ComputeEyeColor(AppearanceStyleDef style, Color blood, Color basePigment, float pigmentAmount, float structureAmount)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		return (style?.eyeModel ?? "structural") switch
		{
			"pigment" => AdjustPigment(basePigment, pigmentAmount * 1.4f), 
			"structural" => Color.Lerp(Color.Lerp(new Color(0.3f, 0.58f, 0.92f), blood, 0.22f), basePigment, Mathf.Clamp01(pigmentAmount * 0.5f + 0.25f)), 
			"reflective" => Color.Lerp(new Color(0.85f, 0.72f, 0.26f), blood, 0.15f), 
			"compound" => Color.Lerp(new Color(0.08f, 0.06f, 0.05f), new Color(0.3f, 0.55f, 0.65f), Mathf.Clamp01(0.35f + structureAmount * 0.2f)), 
			"emissive" => Color.Lerp(blood, new Color(0.9f, 0.95f, 1f), 0.5f) * Mathf.Clamp(1f + structureAmount * 0.3f, 0.8f, 1.8f), 
			_ => AdjustPigment(basePigment, pigmentAmount), 
		};
	}

	public static Color AdjustPigment(Color baseColor, float amount)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Clamp(amount, -1f, 1f);
		if (num >= 0f)
		{
			Color val = new Color(baseColor.r * 0.55f, baseColor.g * 0.55f, baseColor.b * 0.55f, baseColor.a);
			return Color.Lerp(baseColor, val, Mathf.Clamp01(num) * 0.7f);
		}
		return Color.Lerp(baseColor, new Color(0.82f, 0.78f, 0.72f, baseColor.a), Mathf.Clamp01(0f - num) * 0.7f);
	}

	public static Color PigmentColor(AppearanceStyleDef style, string pigmentId, Color fallback)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		if (style?.pigments == null || string.IsNullOrEmpty(pigmentId))
		{
			return fallback;
		}
		foreach (PigmentDef pigment in style.pigments)
		{
			if (!(pigment.pigmentId != pigmentId))
			{
				Color color = pigment.color;
				color.a = Mathf.Clamp01(pigment.opacity);
				return color;
			}
		}
		return fallback;
	}

	private static float ValueSign(float value)
	{
		if (!(value > 0f))
		{
			if (!(value < 0f))
			{
				return 0f;
			}
			return -1f;
		}
		return 1f;
	}
}
}
