using System;
using System.Collections.Generic;

namespace MyriadCreation.Simulation.State
{
public enum ExpeditionState
{
	Outbound,
	Completed,
	Failed
}

/// <summary>
/// 远征实体（P4.1）：地图知识的过程载体。不是军队：不参与会战，只按路线移动、消耗补给、揭示地块。
/// 路线在发起时一次算好（BFS），运行期只推进 cursor；入档后继续。
/// </summary>
[Serializable]
public class Expedition
{
	public int expeditionId;
	public int ownerRealmId;
	public int leaderCharacterId = -1;
	public int homeTileIndex = -1;
	public int currentTileIndex = -1;
	public List<int> route = new List<int>();
	public int routeCursor;
	public float supplies = 100f;
	public ExpeditionState state = ExpeditionState.Outbound;
	public int startDay;
	public string failReason = "";

	public bool IsTerminal => state != ExpeditionState.Outbound;
}
}
