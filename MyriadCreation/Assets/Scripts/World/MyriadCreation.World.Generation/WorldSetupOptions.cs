using System;
using UnityEngine;

namespace MyriadCreation.World.Generation
{
[Serializable]
public class WorldSetupOptions
{
	public InitialContentMode initialContent = InitialContentMode.Custom;

	public bool generatePolities;

	public bool generateSettlements;

	public bool generateInitialUnits;

	[Range(0f, 12f)]
	public int initialPolityCount;

	[Range(0f, 3f)]
	public float initialPopulationScale = 1f;

	[Tooltip("初始聚落数量：0=不生成；-1=按省自动生成；>0=指定数量")]
	[Range(-1f, 1000f)]
	public int initialSettlementCount;

	[Tooltip("初始地图单位数量：0=不生成；>0=在可居地块生成中立单位")]
	[Range(0f, 1000f)]
	public int initialUnitCount;

	public SandboxSpawnRates spawnRates = new SandboxSpawnRates();

	/// <summary>
	/// 自然基线：地理 + 聚落 + 自然生成，不预置政权/君主/宫廷/官职/军队/国教。
	/// 国家由世界运行中的聚落与行为体自然形成（P5.1），菜单里没有“选一个政权”。
	/// </summary>
	public static WorldSetupOptions CreateDefault()
	{
		WorldSetupOptions worldSetupOptions = new WorldSetupOptions();
		worldSetupOptions.initialContent = InitialContentMode.Custom;
		worldSetupOptions.generatePolities = false;
		worldSetupOptions.initialPolityCount = 0;
		worldSetupOptions.generateSettlements = true;
		worldSetupOptions.initialSettlementCount = -1;
		worldSetupOptions.generateInitialUnits = false;
		worldSetupOptions.initialUnitCount = 0;
		worldSetupOptions.initialPopulationScale = 1f;
		worldSetupOptions.spawnRates.enableNaturalSpawning = true;
		worldSetupOptions.Normalize();
		return worldSetupOptions;
	}

	/// <summary>
	/// 沙盒预置：预置 3 个政权与配套君主/国教/军队，仅供测试、调试和“从成熟局面开始”的沙盒。
	/// 它不是默认新游戏；自然演化请用 <see cref="CreateDefault"/>。
	/// </summary>
	public static WorldSetupOptions CreateStandard()
	{
		WorldSetupOptions worldSetupOptions = new WorldSetupOptions();
		worldSetupOptions.initialContent = InitialContentMode.Standard;
		worldSetupOptions.ApplyPreset();
		return worldSetupOptions;
	}

	public static WorldSetupOptions Create(InitialContentMode mode)
	{
		WorldSetupOptions worldSetupOptions = new WorldSetupOptions();
		worldSetupOptions.initialContent = mode;
		worldSetupOptions.ApplyPreset();
		return worldSetupOptions;
	}

	public void ApplyPreset()
	{
		switch (initialContent)
		{
		case InitialContentMode.Empty:
			generatePolities = false;
			generateSettlements = false;
			generateInitialUnits = false;
			initialPolityCount = 0;
			initialSettlementCount = 0;
			initialUnitCount = 0;
			spawnRates.enableNaturalSpawning = false;
			break;
		case InitialContentMode.GeographyOnly:
			generatePolities = false;
			generateSettlements = false;
			generateInitialUnits = false;
			initialPolityCount = 0;
			initialSettlementCount = 0;
			initialUnitCount = 0;
			spawnRates.enableNaturalSpawning = false;
			break;
		case InitialContentMode.Minimal:
			generatePolities = true;
			generateSettlements = true;
			generateInitialUnits = true;
			initialPolityCount = 1;
			initialSettlementCount = 5;
			initialUnitCount = 2;
			initialPopulationScale = 0.25f;
			spawnRates.enableNaturalSpawning = true;
			break;
		case InitialContentMode.Standard:
			generatePolities = true;
			generateSettlements = true;
			generateInitialUnits = false;
			initialPolityCount = 3;
			initialSettlementCount = -1;
			initialUnitCount = 0;
			initialPopulationScale = 1f;
			spawnRates.enableNaturalSpawning = true;
			break;
		}
	}

	public void Normalize()
	{
		if (initialContent != InitialContentMode.Custom)
		{
			ApplyPreset();
		}
		initialPolityCount = Mathf.Clamp(initialPolityCount, 0, 12);
		initialSettlementCount = Mathf.Clamp(initialSettlementCount, -1, 1000);
		initialUnitCount = Mathf.Clamp(initialUnitCount, 0, 1000);
		initialPopulationScale = Mathf.Clamp(initialPopulationScale, 0f, 3f);
		if (initialPolityCount == 0)
		{
			generatePolities = false;
		}
		if (initialSettlementCount == 0)
		{
			generateSettlements = false;
		}
		if (initialUnitCount == 0)
		{
			generateInitialUnits = false;
		}
		spawnRates.mapActorMultiplier = Mathf.Clamp(spawnRates.mapActorMultiplier, 0f, 3f);
	}
}
}
