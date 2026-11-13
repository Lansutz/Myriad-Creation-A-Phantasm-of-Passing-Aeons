using MyriadCreation.Simulation.State;
using MyriadCreation.World.Biome;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Content.Definitions;

using MyriadCreation.Content.Modding;
namespace MyriadCreation.Simulation.ContentHost
{
public static class ContentResolvers
{
	public static IContentResolver<int, ContentRegistry.CultureContentPack> Cultures { get; } = new ContentResolver<int, ContentRegistry.CultureContentPack>((int id) => (!ContentRegistry.Cultures.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Cultures.Values);

	public static IContentResolver<int, RaceData> Races { get; } = new ContentResolver<int, RaceData>((int id) => (!ContentRegistry.Races.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Races.Values);

	public static IContentResolver<int, InnovationDef> Innovations { get; } = new ContentResolver<int, InnovationDef>((int id) => (!ContentRegistry.Innovations.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Innovations.Values);

	public static IContentResolver<int, ReligionDef> Religions { get; } = new ContentResolver<int, ReligionDef>((int id) => (!ContentRegistry.Religions.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Religions.Values);

	public static IContentResolver<string, EthosDef> Ethos { get; } = new ContentResolver<string, EthosDef>((string id) => (!ContentRegistry.Ethos.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Ethos.Values);

	public static IContentResolver<string, HeritageDef> Heritages { get; } = new ContentResolver<string, HeritageDef>((string id) => (!ContentRegistry.Heritages.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Heritages.Values);

	public static IContentResolver<string, TraditionDef> Traditions { get; } = new ContentResolver<string, TraditionDef>((string id) => (!ContentRegistry.Traditions.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Traditions.Values);

	public static IContentResolver<string, LanguageDef> Languages { get; } = new ContentResolver<string, LanguageDef>((string id) => (!ContentRegistry.Languages.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Languages.Values);

	public static IContentResolver<string, EthnicGroupDef> EthnicGroups { get; } = new ContentResolver<string, EthnicGroupDef>((string id) => (!ContentRegistry.EthnicGroups.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.EthnicGroups.Values);

	public static IContentResolver<string, FamilyTraditionDef> FamilyTraditions { get; } = new ContentResolver<string, FamilyTraditionDef>((string id) => (!ContentRegistry.FamilyTraditions.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.FamilyTraditions.Values);

	public static IContentResolver<string, CharacterTemplateDef> CharacterTemplates { get; } = new ContentResolver<string, CharacterTemplateDef>((string id) => (!ContentRegistry.CharacterTemplates.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.CharacterTemplates.Values);

	public static IContentResolver<string, TalentDefectDef> TalentDefects { get; } = new ContentResolver<string, TalentDefectDef>((string id) => (!ContentRegistry.TalentDefects.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.TalentDefects.Values);

	public static IContentResolver<string, MentalDisorderDef> MentalDisorders { get; } = new ContentResolver<string, MentalDisorderDef>((string id) => (!ContentRegistry.MentalDisorders.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.MentalDisorders.Values);

	public static IContentResolver<string, TitleDef> Titles { get; } = new ContentResolver<string, TitleDef>((string id) => (!ContentRegistry.Titles.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Titles.Values);

	public static IContentResolver<string, DoctrineOptionDef> Doctrines { get; } = new ContentResolver<string, DoctrineOptionDef>((string id) => (!ContentRegistry.Doctrines.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Doctrines.Values);

	public static IContentResolver<int, BiomeDef> Biomes { get; } = new ContentResolver<int, BiomeDef>((int id) => (!ContentRegistry.Biomes.TryGetValue(id, out var value)) ? null : value, () => ContentRegistry.Biomes.Values);
}
}
