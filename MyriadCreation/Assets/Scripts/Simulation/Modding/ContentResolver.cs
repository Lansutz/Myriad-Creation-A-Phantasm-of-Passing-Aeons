using System;
using System.Collections.Generic;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Culture;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Society;

namespace CivilizationEvolution.Simulation.Modding
{
    /// <summary>
    /// Stable read-only contract for resolving runtime content.
    /// Consumers depend on this contract instead of ContentRegistry's internal dictionaries.
    /// </summary>
    public interface IContentResolver<TKey, TValue>
    {
        bool TryGet(TKey id, out TValue value);
        IEnumerable<TValue> All { get; }
    }

    /// <summary>
    /// Small adapter used while the legacy ContentRegistry is being migrated.
    /// The backing implementation may later become a dense array, cache, package index,
    /// or another storage mechanism without changing consumers.
    /// </summary>
    public sealed class ContentResolver<TKey, TValue> : IContentResolver<TKey, TValue>
    {
        private readonly Func<TKey, TValue> _lookup;
        private readonly Func<IEnumerable<TValue>> _all;

        public ContentResolver(Func<TKey, TValue> lookup, Func<IEnumerable<TValue>> all)
        {
            _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            _all = all ?? throw new ArgumentNullException(nameof(all));
        }

        public bool TryGet(TKey id, out TValue value)
        {
            value = _lookup(id);
            return !EqualityComparer<TValue>.Default.Equals(value, default(TValue));
        }

        public IEnumerable<TValue> All => _all();
    }

    /// <summary>
    /// Stable entry points for new code.
    /// Existing ContentRegistry dictionaries remain a compatibility facade during migration.
    /// </summary>
    public static class ContentResolvers
    {
        public static IContentResolver<int, ContentRegistry.CultureContentPack> Cultures { get; } =
            new ContentResolver<int, ContentRegistry.CultureContentPack>(
                id => ContentRegistry.Cultures.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Cultures.Values);

        public static IContentResolver<int, RaceData> Races { get; } =
            new ContentResolver<int, RaceData>(
                id => ContentRegistry.Races.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Races.Values);

        public static IContentResolver<int, InnovationDef> Innovations { get; } =
            new ContentResolver<int, InnovationDef>(
                id => ContentRegistry.Innovations.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Innovations.Values);

        public static IContentResolver<int, ReligionDef> Religions { get; } =
            new ContentResolver<int, ReligionDef>(
                id => ContentRegistry.Religions.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Religions.Values);

        public static IContentResolver<string, EthosDef> Ethos { get; } =
            new ContentResolver<string, EthosDef>(
                id => ContentRegistry.Ethos.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Ethos.Values);

        public static IContentResolver<string, TraditionDef> Traditions { get; } =
            new ContentResolver<string, TraditionDef>(
                id => ContentRegistry.Traditions.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Traditions.Values);

        public static IContentResolver<string, LanguageDef> Languages { get; } =
            new ContentResolver<string, LanguageDef>(
                id => ContentRegistry.Languages.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Languages.Values);

        public static IContentResolver<string, EthnicGroupDef> EthnicGroups { get; } =
            new ContentResolver<string, EthnicGroupDef>(
                id => ContentRegistry.EthnicGroups.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.EthnicGroups.Values);

        public static IContentResolver<string, FamilyTraditionDef> FamilyTraditions { get; } =
            new ContentResolver<string, FamilyTraditionDef>(
                id => ContentRegistry.FamilyTraditions.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.FamilyTraditions.Values);

        public static IContentResolver<string, CharacterTemplateDef> CharacterTemplates { get; } =
            new ContentResolver<string, CharacterTemplateDef>(
                id => ContentRegistry.CharacterTemplates.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.CharacterTemplates.Values);

        public static IContentResolver<string, TalentDefectDef> TalentDefects { get; } =
            new ContentResolver<string, TalentDefectDef>(
                id => ContentRegistry.TalentDefects.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.TalentDefects.Values);

        public static IContentResolver<string, MentalDisorderDef> MentalDisorders { get; } =
            new ContentResolver<string, MentalDisorderDef>(
                id => ContentRegistry.MentalDisorders.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.MentalDisorders.Values);

        public static IContentResolver<string, TitleDef> Titles { get; } =
            new ContentResolver<string, TitleDef>(
                id => ContentRegistry.Titles.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Titles.Values);

        public static IContentResolver<string, DoctrineOptionDef> Doctrines { get; } =
            new ContentResolver<string, DoctrineOptionDef>(
                id => ContentRegistry.Doctrines.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Doctrines.Values);

        public static IContentResolver<int, BiomeDef> Biomes { get; } =
            new ContentResolver<int, BiomeDef>(
                id => ContentRegistry.Biomes.TryGetValue(id, out var value) ? value : null,
                () => ContentRegistry.Biomes.Values);
    }
}
