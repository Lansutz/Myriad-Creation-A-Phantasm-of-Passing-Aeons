using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.FaithHost
{
[Serializable]
public class GreatHolyWarState
{
	public int warId;

	public int faithId;

	public int callerRealmId;

	public int targetRealmId;

	public int targetTile = -1;

	public int startDay;

	public bool ended;

	public bool holySideWon;

	public int beneficiaryId = -1;

	public List<int> participants = new List<int>();

	public List<int> contributors = new List<int>();

	public int linkedWarId = -1;
}
}
