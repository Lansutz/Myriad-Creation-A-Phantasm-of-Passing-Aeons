using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.Modding
{
    /// <summary>
    /// A user-authored content package. The same package can be produced by
    /// the built-in editor or authored manually according to the Extension API.
    /// </summary>
    [Serializable]
    public sealed class ContentPackageManifest
    {
        public string formatVersion = "1";
        public string packageId = string.Empty;
        public string displayName = string.Empty;
        public string author = string.Empty;
        public string description = string.Empty;
        public string gameVersion = string.Empty;
        public List<string> contentTypes = new List<string>();
    }

    /// <summary>
    /// Stable package layout shared by Editor, Scenario tools and external mods.
    /// </summary>
    public static class ContentPackageLayout
    {
        public const string ManifestFileName = "mod.json";

        public static readonly string[] SupportedContentDirectories =
        {
            "Culture",
            "Race",
            "Religion",
            "Ethos",
            "Tradition",
            "Language",
            "EthnicGroup",
            "FamilyTradition",
            "CharacterTemplate",
            "Dna",
            "MentalHealth",
            "Innovation",
            "Biome",
            "Title"
        };
    }
}
