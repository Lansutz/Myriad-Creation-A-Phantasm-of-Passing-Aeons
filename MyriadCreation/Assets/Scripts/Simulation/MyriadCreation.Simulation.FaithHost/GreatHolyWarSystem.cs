using System.Collections.Generic;
using MyriadCreation.Simulation.Definitions;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.FaithHost
{
public static class GreatHolyWarSystem
{
	private static int _nextWarId = 1;

	public static readonly List<GreatHolyWarState> ActiveWars = new List<GreatHolyWarState>();

	public static GreatHolyWarState Declare(int faithId, int callerRealmId, int targetRealmId, int targetTile, int day, bool hasLeader, float fervor)
	{
		if (!hasLeader)
		{
			return null;
		}
		if (fervor < 60f)
		{
			return null;
		}
		if (targetRealmId < 0)
		{
			return null;
		}
		GreatHolyWarState greatHolyWarState = new GreatHolyWarState
		{
			warId = _nextWarId++,
			faithId = faithId,
			callerRealmId = callerRealmId,
			targetRealmId = targetRealmId,
			targetTile = targetTile,
			startDay = day,
			linkedWarId = -1
		};
		greatHolyWarState.participants.Add(callerRealmId);
		ActiveWars.Add(greatHolyWarState);
		return greatHolyWarState;
	}

	public static void BindWar(GreatHolyWarState war, int warId)
	{
		if (war != null)
		{
			war.linkedWarId = warId;
		}
	}

	public static void OnLinkedWarEnded(GreatHolyWarState war, bool attackerWon, CharacterManager characters, RealmData callerRealm, int divisibleEstates, InheritanceLaw law)
	{
		if (war != null && !war.ended)
		{
			war.holySideWon = attackerWon;
			Resolve(war, characters, callerRealm, divisibleEstates, law);
		}
	}

	public static void Rally(GreatHolyWarState war, IEnumerable<RealmData> realms)
	{
		if (war == null || war.ended)
		{
			return;
		}
		foreach (RealmData realm in realms)
		{
			if (realm != null && realm.realmId != war.callerRealmId && realm.realmId != war.targetRealmId && realm.stateReligionId == war.faithId && !war.participants.Contains(realm.realmId))
			{
				war.participants.Add(realm.realmId);
			}
		}
	}

	public static int Resolve(GreatHolyWarState war, CharacterManager characters, RealmData callerRealm, int divisibleEstates, InheritanceLaw law)
	{
		if (war == null || war.ended)
		{
			return -1;
		}
		war.ended = true;
		if (!war.holySideWon)
		{
			return -1;
		}
		if (characters == null || callerRealm == null)
		{
			return -1;
		}
		List<int> greatHolyWarBeneficiaryCandidates = SuccessionSystem.GetGreatHolyWarBeneficiaryCandidates(callerRealm, characters, law ?? InheritanceLaw.Primogeniture(), divisibleEstates);
		if (greatHolyWarBeneficiaryCandidates.Count == 0)
		{
			return -1;
		}
		war.beneficiaryId = greatHolyWarBeneficiaryCandidates[0];
		return war.beneficiaryId;
	}

	public static void Cleanup()
	{
		ActiveWars.RemoveAll((GreatHolyWarState w) => w.ended);
	}
}
}
