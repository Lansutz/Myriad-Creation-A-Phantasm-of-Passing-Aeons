using System.Collections.Generic;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Simulation.LawHost
{
public class LawSystem
{
	private GameWorld _world;

	private readonly Dictionary<int, LawEffects.LawEffectData> _cachedEffects = new Dictionary<int, LawEffects.LawEffectData>();

	private readonly Dictionary<int, string> _lastFingerprint = new Dictionary<int, string>();

	private float _refreshTimer;

	private const float RefreshInterval = 30f;

	public LawSystem(GameWorld world)
	{
		_world = world;
	}

	public void InitializeRealmLaws(RealmData realm)
	{
		if (realm != null && realm.laws != null)
		{
			LawEffects.LawEffectData lawEffectData = LawEffects.CalculateEffects(realm.laws);
			_cachedEffects[realm.realmId] = lawEffectData;
			_lastFingerprint[realm.realmId] = Fingerprint(realm.laws);
			ApplyLawEffects(realm, lawEffectData);
			Debug.Log((object)$"[LawSystem] 政权 {realm.realmName} 法律初始化: 扩张性={lawEffectData.expansionism:F2}, 集权度={lawEffectData.centralization:F2}");
		}
	}

	private void ApplyLawEffects(RealmData realm, LawEffects.LawEffectData effects)
	{
		realm.centralization = Mathf.Clamp(effects.centralization, 0f, 1f);
	}

	public void Tick(float deltaTime)
	{
		if ((Object)(object)_world == (Object)null || _world.realms == null)
		{
			return;
		}
		_refreshTimer += deltaTime;
		if (_refreshTimer < 30f)
		{
			return;
		}
		_refreshTimer = 0f;
		foreach (KeyValuePair<int, RealmData> realm in _world.realms)
		{
			RealmData value = realm.Value;
			if (value != null)
			{
				RefreshRealmEffects(value);
			}
		}
	}

	private void RefreshRealmEffects(RealmData realm)
	{
		if (realm.laws != null)
		{
			LawEffects.LawEffectData lawEffectData = LawEffects.CalculateEffects(realm.laws);
			_cachedEffects[realm.realmId] = lawEffectData;
			string text = Fingerprint(realm.laws);
			if (_lastFingerprint.GetValueOrDefault(realm.realmId) != text)
			{
				ApplyLawEffects(realm, lawEffectData);
				_lastFingerprint[realm.realmId] = text;
			}
		}
	}

	private static string Fingerprint(LawComposition c)
	{
		if (c == null)
		{
			return string.Empty;
		}
		return string.Join("|", c.supremeSuccession?.primary ?? (-1), c.supremeScope?.primary ?? (-1), c.centralSuccession?.primary ?? (-1), c.centralInstitution?.primary ?? (-1), c.localSuccession?.primary ?? (-1), c.localScope?.primary ?? (-1), c.spatialStructure?.primary ?? (-1));
	}

	public LawEffects.LawEffectData GetRealmEffects(int realmId)
	{
		if (!_cachedEffects.TryGetValue(realmId, out var value))
		{
			return null;
		}
		return value;
	}

	public void ChangeLaws(int realmId, LawComposition newComposition)
	{
		if (!((Object)(object)_world == (Object)null) && _world.realms != null && realmId >= 0 && realmId < _world.realms.Count)
		{
			RealmData realmData = _world.realms[realmId];
			if (realmData != null)
			{
				realmData.laws = newComposition;
				LawEffects.LawEffectData lawEffectData = LawEffects.CalculateEffects(newComposition);
				_cachedEffects[realmId] = lawEffectData;
				_lastFingerprint[realmId] = Fingerprint(newComposition);
				ApplyLawEffects(realmData, lawEffectData);
				RealmStability.Adjust(realmData, -20f);
				Debug.Log((object)("[LawSystem] 政权 " + realmData.realmName + " 法律手动变更"));
			}
		}
	}

	public bool CheckInnovationRequirements(int realmId, HashSet<string> unlockedInnovations)
	{
		LawEffects.LawEffectData realmEffects = GetRealmEffects(realmId);
		if (realmEffects == null)
		{
			return true;
		}
		foreach (string requiredInnovation in realmEffects.requiredInnovations)
		{
			if (!unlockedInnovations.Contains(requiredInnovation))
			{
				return false;
			}
		}
		return true;
	}

	public List<string> GetMissingInnovations(int realmId, HashSet<string> unlockedInnovations)
	{
		List<string> list = new List<string>();
		LawEffects.LawEffectData realmEffects = GetRealmEffects(realmId);
		if (realmEffects == null)
		{
			return list;
		}
		foreach (string requiredInnovation in realmEffects.requiredInnovations)
		{
			if (!unlockedInnovations.Contains(requiredInnovation))
			{
				list.Add(requiredInnovation);
			}
		}
		return list;
	}
}
}
