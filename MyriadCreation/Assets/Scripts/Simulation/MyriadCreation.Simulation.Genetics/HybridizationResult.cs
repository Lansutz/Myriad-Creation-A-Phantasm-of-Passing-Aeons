using MyriadCreation.Content.Definitions;
namespace MyriadCreation.Simulation.Genetics
{
public struct HybridizationResult
{
	public bool allowed;

	public float introgressionRate;

	public float paternalIntrogressionRate;

	public float maternalIntrogressionRate;

	public bool useMotherSpecies;

	public string reason;
}
}