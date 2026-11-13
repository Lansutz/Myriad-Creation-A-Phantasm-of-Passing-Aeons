namespace MyriadCreation.Simulation.State
{
/// <summary>
/// 政权货币余额的唯一读写入口（P2.5 C 第一步）：`RealmData.treasury` 仍是存档字段，
/// 但运行时代码经这里增减，便于审计“谁动了钱”并统一非负/预检语义。
/// </summary>
public static class RealmTreasury
{
	public static float Get(RealmData realm)
	{
		return realm != null ? realm.treasury : 0f;
	}

	/// <summary>预检并扣款；余额不足返回 false，不做部分扣款。</summary>
	public static bool TrySpend(RealmData realm, float amount)
	{
		if (realm == null || amount <= 0f || realm.treasury < amount)
		{
			return false;
		}
		realm.treasury -= amount;
		return true;
	}

	/// <summary>直接扣款（允许负余额；仅用于旧行为必须保持的路径，如外交赔款）。</summary>
	public static void Spend(RealmData realm, float amount)
	{
		if (realm != null && amount > 0f)
		{
			realm.treasury -= amount;
		}
	}

	public static void Add(RealmData realm, float amount)
	{
		if (realm != null && amount != 0f)
		{
			realm.treasury += amount;
		}
	}

	public static void Set(RealmData realm, float value)
	{
		if (realm != null)
		{
			realm.treasury = value;
		}
	}
}
}
