using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Culture
{
public static class CultureTransition
{
	private const float IndividualAdoptThreshold = 0.25f;

	private const float GroupMergeThreshold = 0.55f;

	private const float SettlementGapMergeCap = 0.35f;

	public static float GetIndividualResistance(CultureData source, CultureData target, CharacterRole role = CharacterRole.Commoner, IContentLookup content = null)
	{
		if (source == null || target == null)
		{
			return 1f;
		}
		float num = CultureSimilarity.CalculateFullSimilarity(source, target);
		float num2 = CultureSimilarity.SettlementGap(source, target);
		return Mathf.Clamp01(0.6f * (1f - num) + 0.2f * num2 + 0.2f * CultureSimilarity.CalculateFactorSimilarity(source, target) - source.GetFusionTendency(content) * 0.1f);
	}

	public static bool CanCharacterAdopt(CultureData source, CultureData target, CharacterRole role = CharacterRole.Commoner, IContentLookup content = null)
	{
		return GetIndividualResistance(source, target, role, content) < 0.25f;
	}

	public static float GetGroupResistance(CultureData source, CultureData target, IContentLookup content = null)
	{
		if (source == null || target == null)
		{
			return 1f;
		}
		float num = CultureSimilarity.CalculateFullSimilarity(source, target);
		float num2 = CultureSimilarity.SettlementGap(source, target);
		return Mathf.Clamp01(0.5f * (1f - num) + 0.35f * num2 + 0.15f * source.GetSeparationTendency(content) - 0.15f * target.GetFusionTendency(content));
	}

	public static bool CanCultureMerge(CultureData source, CultureData target, IContentLookup content = null)
	{
		if (source == null || target == null)
		{
			return false;
		}
		if (CultureSimilarity.SettlementGap(source, target) > 0.35f)
		{
			return false;
		}
		return GetGroupResistance(source, target, content) < 0.55f;
	}
}
}
