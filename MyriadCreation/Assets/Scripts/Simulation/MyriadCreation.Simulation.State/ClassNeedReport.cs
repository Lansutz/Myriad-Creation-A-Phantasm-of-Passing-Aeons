using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class ClassNeedReport
{
	public GameEnums.SocialClass socialClass;

	[NonSerialized]
	public Dictionary<ClassNeedDimension, NeedDimensionScore> dimensions = new Dictionary<ClassNeedDimension, NeedDimensionScore>();

	[Range(0f, 100f)]
	public float overallSatisfaction = 50f;

	public ClassNeedDimension worstDimension;

	[Range(0f, 100f)]
	public float worstScore = 100f;

	public float population;

	public NeedDimensionScore Get(ClassNeedDimension d)
	{
		if (!dimensions.TryGetValue(d, out var value))
		{
			return default;
		}
		return value;
	}
}
}
