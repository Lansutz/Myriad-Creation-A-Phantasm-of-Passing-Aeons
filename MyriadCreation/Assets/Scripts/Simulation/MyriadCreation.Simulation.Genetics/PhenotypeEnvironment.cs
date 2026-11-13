using System;

namespace MyriadCreation.Simulation.Genetics
{
[Serializable]
public struct PhenotypeEnvironment
{
	public float nutrition01;

	public float stress01;

	public float temperatureC;

	public static PhenotypeEnvironment Neutral => new PhenotypeEnvironment
	{
		nutrition01 = 0.5f,
		stress01 = 0.5f,
		temperatureC = 20f
	};
}
}
