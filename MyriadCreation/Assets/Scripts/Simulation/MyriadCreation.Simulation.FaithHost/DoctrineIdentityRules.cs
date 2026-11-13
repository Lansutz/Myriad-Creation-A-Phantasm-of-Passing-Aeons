using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.FaithHost
{
/// <summary>
/// P1.3 合并接线点：教职规则 × 身份旗标。
/// 教职性别、教职婚姻、君权神授只在这里判定一次；任官/婚配/宗教领袖遴选共用，
/// 避免每个消费者各写一遍 switch（RELIGION_V2 §3.2「一个接线点一个实现」）。
/// </summary>
public static class DoctrineIdentityRules
{
	/// <summary>教职资格：男性专属档拒绝女性，其余档（含未指定）放行。</summary>
	public static bool CanHoldReligiousOffice(DoctrineBindings bindings, bool isMale)
	{
		if (bindings == null)
		{
			return true;
		}
		return bindings.clericalGender != DoctrineClericalGender.MaleOnly || isMale;
	}

	/// <summary>独身档只约束教士身份；平民婚配不受教职婚姻教义影响。</summary>
	public static bool CanMarry(DoctrineBindings bindings, SocialIdentityFlags identity)
	{
		if (bindings == null)
		{
			return true;
		}
		bool clergy = (identity & SocialIdentityFlags.Clergy) != 0;
		return !clergy || bindings.clericalMarriage != DoctrineClericalMarriage.Celibate;
	}

	/// <summary>独身档：在任宗教领袖不婚配，遴选时也跳过已婚者。</summary>
	public static bool RequiresCelibacy(DoctrineBindings bindings)
	{
		return bindings != null && bindings.clericalMarriage == DoctrineClericalMarriage.Celibate;
	}

	/// <summary>君权神授：宗教领袖恒为君主，不另选教士。</summary>
	public static bool IsSacredKingship(DoctrineBindings bindings)
	{
		return bindings != null && bindings.sacredKingship;
	}
}
}
