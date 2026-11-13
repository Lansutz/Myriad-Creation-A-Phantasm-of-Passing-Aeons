using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Laws
{
[Serializable]
public class DecreeDef
{
	public string decreeId = "";

	public string name = "";

	public string description = "";

	public LawRequirement requirements = new LawRequirement();

	public LawEffect effects = new LawEffect();

	public float costTreasury;

	public float costAuthority;

	public int durationDays = 180;

	public int cooldownDays = 360;

	/// <summary>与这些法令互斥；任一已生效则不能颁布。</summary>
	public List<string> excludesDecreeIds = new List<string>();
}
}
