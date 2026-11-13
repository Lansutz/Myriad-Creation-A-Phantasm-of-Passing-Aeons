using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class ReligionDef
{
	public int religionId;

	public string religionName;

	public int parentReligionId = -1;

	/// <summary>
	/// 宗派分裂父（仅 denomination 节点）：本宗派从哪个宗派分出，-1 = 由该 religion 直接产生。
	/// 与 `parentReligionId` 分开：后者只表示层级上的上位 religion。
	/// </summary>
	public int parentDenominationId = -1;

	/// <summary>
	/// **传承**（G2）：本 faith 由哪个 faith 分出，-1 = 初代信仰。
	/// 与 `parentReligionId`（层级父）是两件事：传承可跨层级（例如某信仰从另一个宗教下的信仰分出）。
	/// 留空时由 GameWorld 回退推导（父节点本身是 faith 则取父），并做环检测。
	/// </summary>
	public int lineageFaithId = -1;

	/// <summary>faith 显式所属的宗派；-1 = 未声明，由层级父或 religion 默认宗派解析。</summary>
	public int denominationId = -1;

	public int schoolParentId = -1;

	public ReligionNodeType nodeType = ReligionNodeType.Succession;

	public bool hasSuccession = true;

	public string worldview = "";

	public string founder = "";

	public bool orthodoxy = true;

	public List<string> virtues = new List<string>();

	public List<string> sins = new List<string>();

	public string headName = "";

	public string riteHeadName = "";

	public string communionName = "";

	public bool isMainstreamTradition;

	public string liturgicalLanguage = "";

	public string scripturalLanguage = "";

	public List<string> selectedDoctrines = new List<string>();

	/// <summary>正统必备教义 optionId；为空表示该 religion 未声明正统边界（不判异端）。</summary>
	public List<string> orthodoxyTenets = new List<string>();

	/// <summary>明确排除的教义 optionId；出现即越界。</summary>
	public List<string> orthodoxyExclusions = new List<string>();

	public List<int> requiredInnovations = new List<int>();

	public List<string> rites = new List<string>();

	public string riteFamily = "";

	public string school = "";

	public Color color = Color.white;

	public bool IsRoot => parentReligionId < 0;

	public string PrimaryRite
	{
		get
		{
			if (rites == null || rites.Count <= 0)
			{
				return "";
			}
			return rites[0];
		}
	}

	public ReligionDef()
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
	}
}
}
