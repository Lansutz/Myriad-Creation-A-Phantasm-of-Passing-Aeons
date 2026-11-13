namespace MyriadCreation.Content.Definitions
{
public class EconomicalArchetypeInfo
{
	public EconomicalArchetype archetype;

	public string displayName;

	public string description;

	public float warBias;

	public float buildBias;

	public float faithBias;

	public float schemeBias;

	public float diplomacyBias;

	public EconomicalArchetypeInfo(EconomicalArchetype archetype, string displayName, string description, float war, float build, float faith, float scheme, float diplomacy)
	{
		this.archetype = archetype;
		this.displayName = displayName;
		this.description = description;
		warBias = war;
		buildBias = build;
		faithBias = faith;
		schemeBias = scheme;
		diplomacyBias = diplomacy;
	}
}
}
