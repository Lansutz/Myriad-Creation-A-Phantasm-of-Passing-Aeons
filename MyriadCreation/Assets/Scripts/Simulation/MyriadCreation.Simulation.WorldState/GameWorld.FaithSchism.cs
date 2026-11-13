using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.Faith;

namespace MyriadCreation.Simulation.WorldState
{
public partial class GameWorld
{
	/// <summary>
	/// P2.2 六层运行时：从某条 faith 分出一个宗派，并把该 faith 挂过去。
	/// 宗派必挂同一 religion（D29）；父宗派必须同教。返回新宗派 id，失败 -1。
	/// 这是「分裂」的唯一写入口——不新建平行教团实体。
	/// </summary>
	public int CreateDenomination(int faithId, string name, int parentDenominationId = -1)
	{
		FaithSystem faith = GetFaithSystem(faithId);
		if (faith == null || faith.religionId < 0)
		{
			return -1;
		}
		if (parentDenominationId >= 0)
		{
			Denomination parent = GetDenomination(parentDenominationId);
			if (parent == null || parent.religionId != faith.religionId)
			{
				return -1;
			}
		}
		Denomination denomination = new Denomination
		{
			denominationId = NextDenominationId(),
			religionId = faith.religionId,
			parentDenominationId = parentDenominationId,
			denominationName = string.IsNullOrWhiteSpace(name) ? faith.faithName + "宗" : name,
			isDefault = false,
			status = DenominationStatus.Tolerated,
			sinceDay = currentDay
		};
		if (!RegisterDenomination(denomination))
		{
			return -1;
		}
		faith.denominationId = denomination.denominationId;
		RebuildDenominationMembership();
		_chronicle?.Add("religion", faith.faithName + " 分出 " + denomination.denominationName, true);
		return denomination.denominationId;
	}

	/// <summary>把一条 faith 挂到已存在的宗派（同教校验）；用于分裂后邀请同阵营 faith 加入。</summary>
	public bool AttachFaithToDenomination(int faithId, int denominationId)
	{
		FaithSystem faith = GetFaithSystem(faithId);
		Denomination denomination = GetDenomination(denominationId);
		if (faith == null || denomination == null || faith.religionId < 0 || faith.religionId != denomination.religionId)
		{
			return false;
		}
		if (faith.denominationId == denominationId)
		{
			return true;
		}
		faith.denominationId = denominationId;
		RebuildDenominationMembership();
		_chronicle?.Add("religion", faith.faithName + " 归于 " + denomination.denominationName, true);
		return true;
	}

	/// <summary>
	/// P2.2 宗派状态月结：声明性正统越界 → 异端；否则分歧度 ≥ 容忍阈值 → 分裂容忍；否则主流。
	/// 只读内容定义与 divergence，不改 faith 的教义选择。
	/// </summary>
	private void TickDenominationStatuses()
	{
		if (_denominations == null || _faithSystems == null)
		{
			return;
		}
		List<string> violations = new List<string>();
		for (int i = 0; i < _denominations.Count; i++)
		{
			Denomination denomination = _denominations[i];
			if (denomination == null || denomination.isDefault || denomination.faithIds == null || denomination.faithIds.Count == 0)
			{
				continue;
			}
			float maxDivergence = 0f;
			int violationCount = 0;
			for (int f = 0; f < denomination.faithIds.Count; f++)
			{
				FaithSystem faith = GetFaithSystem(denomination.faithIds[f]);
				if (faith == null || faith.religionId < 0)
				{
					continue;
				}
				if (!ContentResolvers.Religions.TryGet(faith.faithId, out ReligionDef faithDef) || faithDef == null)
				{
					continue;
				}
				if (!ContentResolvers.Religions.TryGet(faith.religionId, out ReligionDef orthodox) || orthodox == null)
				{
					continue;
				}
				float divergence = ReligionCatalog.GetDivergence(orthodox, faithDef);
				if (divergence > maxDivergence)
				{
					maxDivergence = divergence;
				}
				violations.Clear();
				if (OrthodoxyEvaluator.Evaluate(orthodox, faithDef.selectedDoctrines, violations) == OrthodoxyVerdict.OutOfBounds)
				{
					violationCount++;
				}
			}
			DenominationStatus status = DenominationStatusRules.Evaluate(maxDivergence, violationCount);
			if (status == denomination.status)
			{
				continue;
			}
			denomination.status = status;
			_chronicle?.Add("religion", denomination.denominationName + " 状态转为 " + status, true);
		}
	}

	private int NextDenominationId()
	{
		int next = 1;
		for (int i = 0; i < _denominations.Count; i++)
		{
			Denomination denomination = _denominations[i];
			if (denomination != null && denomination.denominationId >= next)
			{
				next = denomination.denominationId + 1;
			}
		}
		return next;
	}
}
}
