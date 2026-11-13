using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
/// <summary>生态物种（P4.4）：只描述对承载/捕猎有意义的性状，不复制遗传定义。</summary>
[Serializable]
public sealed class EcologySpeciesDef
{
	public string speciesId = "";
	public string name = "";
	/// <summary>herbivore / predator / fish / plant。</summary>
	public string role = "herbivore";
	/// <summary>栖息地：forest/grass/arid/wet/tundra/coast/generic。</summary>
	public string habitat = "generic";
	public float bodySize = 1f;
	public float fecundity = 1f;
	public float dietHerbivore = 1f;
	public float dietCarnivore;
}
}
