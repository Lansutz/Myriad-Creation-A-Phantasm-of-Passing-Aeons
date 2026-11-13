using UnityEngine;

namespace MyriadCreation.Simulation.State
{
/// <summary>
/// 政权稳定度的唯一读写入口（P2.5 C 第二步）：`RealmData.stability` 仍是存档字段，
/// 运行时统一经此增减并钳制在 0–100，避免各处只钳一边（有的只 Max(0)、有的只 Min(100)）。
/// </summary>
public static class RealmStability
{
	public static float Get(RealmData realm)
	{
		return realm != null ? realm.stability : 0f;
	}

	public static void Adjust(RealmData realm, float delta)
	{
		if (realm != null)
		{
			realm.stability = Mathf.Clamp(realm.stability + delta, 0f, 100f);
		}
	}

	public static void Set(RealmData realm, float value)
	{
		if (realm != null)
		{
			realm.stability = Mathf.Clamp(value, 0f, 100f);
		}
	}

	/// <summary>向基准值缓慢靠拢（月结/日结恢复用）；rate 为本次比例的 0–1。</summary>
	public static void Drift(RealmData realm, float baseline, float rate)
	{
		if (realm != null)
		{
			realm.stability = Mathf.Clamp(Mathf.Lerp(realm.stability, baseline, Mathf.Clamp01(rate)), 0f, 100f);
		}
	}
}
}
