using System;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct DnaExpression
{
	public float longevityOffsetYears;

	public float intelligenceOffset;

	public float martialOffset;

	public float resistanceOffset;

	public string appearanceTag;

	public string talentId;

	public string defectId;

	public bool carriesDefect;
}
}
