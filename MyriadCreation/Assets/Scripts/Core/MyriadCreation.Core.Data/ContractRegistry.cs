using System.Collections.Generic;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Core.Data
{
public static class ContractRegistry
{
	private static readonly Dictionary<string, Contract> _contracts = new Dictionary<string, Contract>();

	public static int Count => _contracts.Count;

	public static void Clear()
	{
		_contracts.Clear();
	}

	public static void Add(Contract contract)
	{
		if (contract != null && !string.IsNullOrEmpty(contract.contractId))
		{
			_contracts[contract.contractId] = contract;
		}
	}

	public static bool TryGet(string contractId, out Contract contract)
	{
		return _contracts.TryGetValue(contractId, out contract);
	}

	public static bool Remove(string contractId)
	{
		return _contracts.Remove(contractId);
	}

	public static List<Contract> GetByParty(EntityKind kind, int id)
	{
		List<Contract> list = new List<Contract>();
		foreach (Contract value in _contracts.Values)
		{
			foreach (EntityRef party in value.parties)
			{
				if (party.kind == kind && party.id == id)
				{
					list.Add(value);
					break;
				}
			}
		}
		return list;
	}

	public static List<Contract> Capture()
	{
		List<Contract> list = new List<Contract>();
		foreach (Contract value in _contracts.Values)
		{
			list.Add(value.Clone());
		}
		return list;
	}

	public static void Restore(IEnumerable<Contract> contracts)
	{
		Clear();
		if (contracts == null)
		{
			return;
		}
		foreach (Contract contract in contracts)
		{
			Add(contract);
		}
	}
}
}
