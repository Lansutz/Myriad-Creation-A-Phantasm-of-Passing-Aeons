using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Genetics
{
[Serializable]
public class AppearanceStyleDef
{
	public string styleId = "appearance_human";

	public string bloodPigmentId = "hemoglobin_red";

	public string skinBasePigmentId = "melanin";

	public string hairBasePigmentId = "melanin";

	public string eyeModel = "structural";

	public string eyeBasePigmentId = "";

	public List<PigmentDef> pigments = new List<PigmentDef>();
}
}
