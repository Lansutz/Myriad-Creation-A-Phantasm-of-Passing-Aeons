using System;
using UnityEngine;

namespace MyriadCreation.World.Terrain
{
[Serializable]
public class TerrainTemplatePreset
{
	public string Name;

	public string Description;

	public TerrainScale Scale;

	public float TargetLandFraction = 0.3f;

	public float TerrainFrequency = 1.8f;

	public int TerrainOctaves = 6;

	public float WarpStrength = 0.7f;

	public float WarpFrequency = 1.3f;

	public float MountainHeight = 0.35f;

	public int PlateCount = 12;

	public float PlateBoundaryMountainBoost = 0.25f;

	[Tooltip("主大陆最小数量")]
	public int MinContinentCount = 1;

	[Tooltip("主大陆最大数量")]
	public int MaxContinentCount = 1;

	[Tooltip("单块主大陆最小占比（占总陆地面积比例，0~1）")]
	public float MinContinentSize = 0.15f;

	[Tooltip("单块主大陆最大占比（占总陆地面积比例，0~1）")]
	public float MaxContinentSize = 0.6f;

	[Tooltip("零散岛屿/半岛密度（0=无，1=大量）。模板以主大陆为主，辅以零散小东西。")]
	public float IslandDensity = 0.3f;

	public bool HasSpecialShape;

	public string SpecialShapeType = "";
}
}
