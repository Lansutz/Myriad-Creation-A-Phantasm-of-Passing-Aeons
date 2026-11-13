using System;
using UnityEngine;

namespace MyriadCreation.Simulation.Genetics
{
[Serializable]
public class PigmentDef
{
	public string pigmentId = "";

	public Color color = Color.white;

	public float opacity = 1f;

	public bool reflective;

	public string note = "";

	public PigmentDef()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
	}
}
}
