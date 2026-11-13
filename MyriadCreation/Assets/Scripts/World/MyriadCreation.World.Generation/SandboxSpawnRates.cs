using System;
using UnityEngine;

namespace MyriadCreation.World.Generation
{
[Serializable]
public class SandboxSpawnRates
{
	[Tooltip("运行中是否自然生成流民/游牧/商队/野兽/土匪/灾害")]
	public bool enableNaturalSpawning = true;

	[Tooltip("自然生成总倍率（0=停止，1=默认）")]
	[Range(0f, 3f)]
	public float mapActorMultiplier = 1f;

	[Tooltip("是否自然生成新聚落（营地/村落）")]
	public bool spawnSettlements = true;

	[Tooltip("是否自然扩散人口/物种")]
	public bool spreadPopulation = true;
}
}
