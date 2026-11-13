using UnityEngine;

namespace MyriadCreation.World.Terrain
{
/// <summary>
/// 生态生产力（借鉴 Earth Simulator 的 Miami 模型 + 高温叶片上限）：
/// NPP 由年均温与年降水共同限制，结果 0–1；只做模型移植，不复制其实现代码。
/// </summary>
public static class EcologyModel
{
	public const float NppHeat0 = 45f;
	public const float NppHeat1 = 55f;

	public static float ComputeNpp(float annualTempC, float annualPrecipMm)
	{
		float nppT = 3000f / (1f + Mathf.Exp(1.315f - 0.119f * annualTempC));
		if (annualTempC > NppHeat0)
		{
			nppT *= Mathf.Clamp01((NppHeat1 - annualTempC) / Mathf.Max(0.1f, NppHeat1 - NppHeat0));
		}
		float nppP = 3000f * (1f - Mathf.Exp(-0.000664f * annualPrecipMm));
		return Mathf.Clamp01(Mathf.Min(nppT, nppP) / 3000f);
	}
}
}
