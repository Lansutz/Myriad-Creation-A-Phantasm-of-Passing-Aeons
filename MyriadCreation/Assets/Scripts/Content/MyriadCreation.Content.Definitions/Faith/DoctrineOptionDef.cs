using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class DoctrineOptionDef
{
	public string optionId;

	public string pillar = "doctrine";

	public string optionName;

	public string description = "";

	public List<int> exclusiveReligionIds = new List<int>();

	public string enhancedFrom = "";

	/// <summary>所属组（空=按 optionId 前缀派生）；组→域/序见 DoctrineSchema。</summary>
	public string groupId = "";

	/// <summary>组内序（加载期按声明顺序回填；sequence 组用它算偏离度）。</summary>
	public int level;

	/// <summary>绑定点：`域:键`（crime/driving、marriage:divorce、ritual:funeral…）；无消费者的写 `display:...`。</summary>
	public string bindsTo = "";

	/// <summary>组的序类型：sequence=光谱档，category=互斥并行项；加载期由 DoctrineSchema 回填。</summary>
	public string kind = "";

	/// <summary>参数声明；键必须在 DoctrineSchema 的全局注册表里。</summary>
	public List<DoctrineParameterDef> parameters = new List<DoctrineParameterDef>();
}
}
