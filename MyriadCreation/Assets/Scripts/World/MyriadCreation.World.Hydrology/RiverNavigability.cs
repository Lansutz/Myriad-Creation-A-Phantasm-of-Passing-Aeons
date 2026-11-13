using MyriadCreation.Core.Data;

namespace MyriadCreation.World.Hydrology
{
public static class RiverNavigability
{
	public static bool IsNavigable(TileData tile)
	{
		if (!tile.isRiver)
		{
			return false;
		}
		if (tile.riverStreamOrder >= 2)
		{
			return true;
		}
		if (tile.riverWidthMeters >= 20f && tile.riverDepthMeters >= 0.6f)
		{
			return true;
		}
		if (tile.riverStreamOrder == 0 && tile.riverWidthMeters <= 0f && tile.elevation01 < 0.5f)
		{
			return tile.annualPrecipMm > 600f;
		}
		return false;
	}
}
}
