namespace MyriadCreation.Content.Modding
{
public static class ContentPackageLayout
{
	public const string ManifestFileName = "mod.json";

	public static readonly string[] SupportedContentDirectories = new string[16]
	{
		"Culture", "Race", "Religion", "Ethos", "Tradition", "Language", "EthnicGroup", "FamilyTradition", "CharacterTemplate", "Dna",
		"MentalHealth", "Innovation", "Biome", "Title", "Genetics", "Ecology"
	};
}
}
