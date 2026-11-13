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
				IReadOnlyList<string> effectiveDoctrines = FaithDoctrine.GetIds(faith, faithDef);
				float divergence = ReligionCatalog.GetDivergence(orthodox.selectedDoctrines, effectiveDoctrines);
				if (divergence > maxDivergence)
				{
					maxDivergence = divergence;
				}
				violations.Clear();
				if (OrthodoxyEvaluator.Evaluate(orthodox, effectiveDoctrines, violations) == OrthodoxyVerdict.OutOfBounds)
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

	/// <summary>
	/// P2.2 宗教局势月结：同一 religion 下诸 faith 的最大分歧度决定阶段；
	/// 达到分裂阈值时，把分歧最大的 faith 分出宗派（只在该 faith 仍挂默认宗派时触发一次）。
	/// </summary>
	private void TickReligionStages()
	{
		if (_faithSystems == null || _faithSystems.Count == 0)
		{
			return;
		}
		Dictionary<int, List<FaithSystem>> byReligion = new Dictionary<int, List<FaithSystem>>();
		for (int i = 0; i < _faithSystems.Count; i++)
		{
			FaithSystem faith = _faithSystems[i];
			if (faith == null || faith.religionId < 0)
			{
				continue;
			}
			if (!byReligion.TryGetValue(faith.religionId, out List<FaithSystem> list))
			{
				list = new List<FaithSystem>();
				byReligion[faith.religionId] = list;
			}
			list.Add(faith);
		}
		foreach (KeyValuePair<int, List<FaithSystem>> pair in byReligion)
		{
			if (pair.Value.Count < 2)
			{
				continue;
			}
			float maxDivergence = 0f;
			FaithSystem worst = null;
			for (int i = 0; i < pair.Value.Count; i++)
			{
				FaithSystem faith = pair.Value[i];
				if (!ContentResolvers.Religions.TryGet(faith.faithId, out ReligionDef faithDef) || faithDef == null)
				{
					continue;
				}
				if (!ContentResolvers.Religions.TryGet(faith.religionId, out ReligionDef orthodox) || orthodox == null)
				{
					continue;
				}
				IReadOnlyList<string> effectiveDoctrines = FaithDoctrine.GetIds(faith, faithDef);
				float divergence = ReligionCatalog.GetDivergence(orthodox.selectedDoctrines, effectiveDoctrines);
				if (divergence > maxDivergence)
				{
					maxDivergence = divergence;
					worst = faith;
				}
			}
			if (worst == null)
			{
				continue;
			}
			ReligionStage stage = ReligionStageRules.StageFor(maxDivergence);
			bool allCommunion = true;
			for (int i = 0; i < pair.Value.Count; i++)
			{
				if (pair.Value[i].communionUntilDay <= currentDay)
				{
					allCommunion = false;
					break;
				}
			}
			// 妥协期：保留分歧但不上敌意/分裂。
			if (allCommunion && stage > ReligionStage.Compromise)
			{
				stage = ReligionStage.Compromise;
			}
			if (stage != ReligionStage.Schism)
			{
				continue;
			}
			Denomination current = GetDenomination(worst.denominationId);
			if (current != null && !current.isDefault)
			{
				continue;
			}
			CreateDenomination(worst.faithId, worst.faithName + "宗");
		}
	}

	/// <summary>
	/// 合一：同教各 faith 采用同一套教义（取当前最常见的集合，平票取上位 religion 的正统集合），
	/// 清除共融期限。返回是否执行。
	/// </summary>
	public bool TryUnifyReligion(int religionId)
	{
		List<FaithSystem> group = FaithsOfReligion(religionId);
		if (group.Count < 2)
		{
			return false;
		}
		ReligionDef orthodox = null;
		ContentResolvers.Religions.TryGet(religionId, out orthodox);
		Dictionary<string, int> counts = new Dictionary<string, int>();
		Dictionary<string, List<string>> sets = new Dictionary<string, List<string>>();
		List<string> best = orthodox?.selectedDoctrines != null ? new List<string>(orthodox.selectedDoctrines) : new List<string>();
		int bestCount = 0;
		for (int i = 0; i < group.Count; i++)
		{
			ReligionDef faithDef = null;
			ContentResolvers.Religions.TryGet(group[i].faithId, out faithDef);
			List<string> ids = new List<string>(FaithDoctrine.GetIds(group[i], faithDef));
			string key = string.Join("|", ids);
			counts.TryGetValue(key, out int count);
			counts[key] = count + 1;
			sets[key] = ids;
			if (count + 1 > bestCount)
			{
				bestCount = count + 1;
				best = ids;
			}
		}
		for (int i = 0; i < group.Count; i++)
		{
			FaithDoctrine.SetIds(group[i], best);
			group[i].communionUntilDay = -1;
		}
		_chronicle?.Add("religion", "宗教 " + religionId + " 达成合一：诸信仰采用同一教义集合", true);
		return true;
	}

	/// <summary>妥协：保留各自教义，但约定共融期内不升级为敌意/分裂。</summary>
	public bool TryCompromiseReligion(int religionId, int days)
	{
		List<FaithSystem> group = FaithsOfReligion(religionId);
		if (group.Count < 2 || days <= 0)
		{
			return false;
		}
		int until = currentDay + days;
		for (int i = 0; i < group.Count; i++)
		{
			group[i].communionUntilDay = until;
		}
		_chronicle?.Add("religion", "宗教 " + religionId + " 达成妥协：共融至第 " + until + " 日", true);
		return true;
	}

	/// <summary>分裂：把当前分歧最大的 faith 分出宗派；返回新宗派 id（失败 -1）。</summary>
	public int TrySchismReligion(int religionId)
	{
		List<FaithSystem> group = FaithsOfReligion(religionId);
		FaithSystem worst = FindMostDivergentFaith(group);
		if (worst == null)
		{
			return -1;
		}
		Denomination current = GetDenomination(worst.denominationId);
		if (current != null && !current.isDefault)
		{
			return -1;
		}
		return CreateDenomination(worst.faithId, worst.faithName + "宗");
	}

	private List<FaithSystem> FaithsOfReligion(int religionId)
	{
		List<FaithSystem> group = new List<FaithSystem>();
		if (_faithSystems == null)
		{
			return group;
		}
		for (int i = 0; i < _faithSystems.Count; i++)
		{
			FaithSystem faith = _faithSystems[i];
			if (faith != null && faith.religionId == religionId)
			{
				group.Add(faith);
			}
		}
		return group;
	}

	private FaithSystem FindMostDivergentFaith(List<FaithSystem> group)
	{
		FaithSystem worst = null;
		float maxDivergence = 0f;
		for (int i = 0; i < group.Count; i++)
		{
			FaithSystem faith = group[i];
			ReligionDef faithDef = null;
			ReligionDef orthodox = null;
			if (!ContentResolvers.Religions.TryGet(faith.faithId, out faithDef) || faithDef == null)
			{
				continue;
			}
			if (!ContentResolvers.Religions.TryGet(faith.religionId, out orthodox) || orthodox == null)
			{
				continue;
			}
			float divergence = ReligionCatalog.GetDivergence(orthodox.selectedDoctrines, FaithDoctrine.GetIds(faith, faithDef));
			if (divergence > maxDivergence)
			{
				maxDivergence = divergence;
				worst = faith;
			}
		}
		return worst;
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
