using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class RegimeChangeState
{
	public int realmId;

	public StructuralTension tension = new StructuralTension();

	public ActiveJuncture activeJuncture;

	public int compositionEstablishedDay;

	[Range(0f, 100f)]
	public float institutionalInertia;

	public List<string> history = new List<string>();

	public bool IsWindowOpen
	{
		get
		{
			if (activeJuncture != null)
			{
				return !activeJuncture.resolved;
			}
			return false;
		}
	}
}
}
