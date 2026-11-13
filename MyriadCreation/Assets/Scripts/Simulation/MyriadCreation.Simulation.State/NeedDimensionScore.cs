using System;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public struct NeedDimensionScore
{
	public ClassNeedDimension dimension;

	[Range(0f, 100f)]
	public float score;

	public string reason;
}
}
