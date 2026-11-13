using System;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.CultureHost
{
/// <summary>
/// 文化系统契约适配器（B5）：实现 Core.Contracts 的 ICultureQuery/ICultureCommand，
/// 内部转调现有 CultureData / RealmData.primaryCultureId；旧 API 全部保留。
/// </summary>
public sealed class CultureContracts : ICultureQuery, ICultureCommand
{
	private readonly GameWorld _world;

	public CultureContracts(GameWorld world)
	{
		_world = world;
	}

	private RealmData GetRealm(int realmId)
	{
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out RealmData value))
		{
			return null;
		}
		return value;
	}

	private CultureData GetCulture(int cultureId)
	{
		if (_world?.cultures == null || !_world.cultures.TryGetValue(cultureId, out CultureData value))
		{
			return null;
		}
		return value;
	}

	// ===== ICultureQuery =====

	public int GetPrimaryCultureId(int realmId)
	{
		return GetRealm(realmId)?.primaryCultureId ?? -1;
	}

	public string GetCultureName(int cultureId)
	{
		return GetCulture(cultureId)?.cultureName ?? "";
	}

	public int GetCultureStage(int cultureId)
	{
		CultureData culture = GetCulture(cultureId);
		return ((culture != null) ? ((int)culture.stage) : (-1));
	}

	public string GetCultureStageKey(int stage)
	{
		if (Enum.IsDefined(typeof(GameEnums.CultureStage), stage))
		{
			return ((GameEnums.CultureStage)stage).ToString();
		}
		return "Unknown";
	}

	// ===== ICultureCommand =====

	public bool SetPrimaryCulture(int realmId, int cultureId, out string reason)
	{
		reason = "";
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			reason = "政权不存在";
			return false;
		}
		if (GetCulture(cultureId) == null)
		{
			reason = "文化不存在：" + cultureId;
			return false;
		}
		realm.primaryCultureId = cultureId;
		return true;
	}

	public bool SetCultureStage(int cultureId, int stage, out string reason)
	{
		reason = "";
		CultureData culture = GetCulture(cultureId);
		if (culture == null)
		{
			reason = "文化不存在：" + cultureId;
			return false;
		}
		if (!Enum.IsDefined(typeof(GameEnums.CultureStage), stage))
		{
			reason = "文化阶段非法：" + stage;
			return false;
		}
		culture.stage = (GameEnums.CultureStage)stage;
		return true;
	}
}
}
