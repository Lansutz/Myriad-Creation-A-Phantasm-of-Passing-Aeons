namespace MyriadCreation.World.Settlement
{
public struct WallDefenseSnapshot
{
	public bool hasWall;

	public float wallLengthMeters;

	public int wallLayerCount;

	public int wallBlockCount;

	public int wallSegmentCount;

	public int fortificationLevel;

	public WallMaterial wallMaterial;

	public int wallMaterialGrade;

	public float averageCondition;

	public int towerCount;

	public int requiredGarrison;

	public int garrison;

	public float coverage;

	public float effectiveFortification;

	public float unguardedLengthMeters;

	public bool overcrowded;
}
}
