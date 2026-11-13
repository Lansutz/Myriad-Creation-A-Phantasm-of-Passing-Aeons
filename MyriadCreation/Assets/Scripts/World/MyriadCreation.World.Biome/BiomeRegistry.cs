using System.Collections.Generic;

namespace MyriadCreation.World.Biome
{
public static class BiomeRegistry
{
	public static readonly Dictionary<int, BiomeDef> Overrides = new Dictionary<int, BiomeDef>();

	public static bool TryGet(int id, out BiomeDef def)
	{
		return Overrides.TryGetValue(id, out def);
	}
}
}
