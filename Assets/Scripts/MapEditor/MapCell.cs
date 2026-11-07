namespace Myriad.MapEditor
{
    public enum OceanTier
    {
        None,
        Coast,
        NearSea,
        DeepSea
    }

    /// <summary>Source and cached map data for one square-grid cell.</summary>
    public sealed class MapCell
    {
        public float Elevation;
        public bool IsLand;
        public bool IsCoast;
        public OceanTier OceanTier;
        public float OceanDepth01;
        public int? SeaConnectId;

        public float Slope01;
        public float Temperature01;
        public float Moisture01;
        public string Biome = "Unknown";
        public float TravelCost;
    }
}
