using System.Collections.Generic;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.Definitions
{
public static class ImpairmentRegistry
{
	private static Dictionary<string, IImpairmentDef> _defs;

	public static IReadOnlyDictionary<string, IImpairmentDef> Defs
	{
		get
		{
			if (_defs == null)
			{
				Initialize();
			}
			return _defs;
		}
	}

	private static void Initialize()
	{
		_defs = new Dictionary<string, IImpairmentDef>
		{
			{
				"vision",
				new VisionImpairmentDef()
			},
			{
				"hearing",
				new HearingImpairmentDef()
			},
			{
				"cognition",
				new CognitionImpairmentDef()
			},
			{
				"mobility",
				new MobilityImpairmentDef()
			},
			{
				"speech",
				new SpeechImpairmentDef()
			}
		};
	}

	public static IImpairmentDef GetDef(string impairmentId)
	{
		if (Defs.TryGetValue(impairmentId, out var value))
		{
			return value;
		}
		return null;
	}

	public static ImpairmentStage GetStage(string impairmentId, int stageIndex)
	{
		IImpairmentDef def = GetDef(impairmentId);
		if (def == null || stageIndex < 0 || stageIndex >= def.Stages.Length)
		{
			return default;
		}
		return def.Stages[stageIndex];
	}
}
}
