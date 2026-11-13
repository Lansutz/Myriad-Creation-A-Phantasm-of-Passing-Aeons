using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
/// <summary>生态物种表：按栖息地聚池；承载/捕猎系数由池内物种性状推导，不存逐格物种数组。</summary>
public static class EcologySpeciesCatalog
{
	private static Dictionary<string, EcologySpeciesDef> _byId;
	private static Dictionary<string, List<EcologySpeciesDef>> _byHabitat;

	public static bool IsLoaded => _byId != null && _byId.Count > 0;

	public static IReadOnlyDictionary<string, EcologySpeciesDef> All
	{
		get { EnsureEmpty(); return _byId; }
	}

	public static void LoadFromContent(IEnumerable<EcologySpeciesDef> defs)
	{
		_byId = new Dictionary<string, EcologySpeciesDef>();
		_byHabitat = new Dictionary<string, List<EcologySpeciesDef>>();
		if (defs == null) return;
		foreach (EcologySpeciesDef def in defs)
		{
			if (def == null || string.IsNullOrEmpty(def.speciesId)) continue;
			_byId[def.speciesId] = def;
			string habitat = string.IsNullOrEmpty(def.habitat) ? "generic" : def.habitat;
			if (!_byHabitat.TryGetValue(habitat, out List<EcologySpeciesDef> list))
			{
				_byHabitat[habitat] = list = new List<EcologySpeciesDef>();
			}
			list.Add(def);
		}
	}

	public static void ResetContent()
	{
		_byId = null;
		_byHabitat = null;
	}

	public static IReadOnlyList<EcologySpeciesDef> PoolFor(string habitat)
	{
		EnsureEmpty();
		if (!string.IsNullOrEmpty(habitat) && _byHabitat.TryGetValue(habitat, out List<EcologySpeciesDef> list)) return list;
		return _byHabitat.TryGetValue("generic", out List<EcologySpeciesDef> generic) ? generic : new List<EcologySpeciesDef>();
	}

	/// <summary>草食/鱼群承载系数：池内体型与繁殖率越高，承载越高；空表返回 1（旧行为）。</summary>
	public static float CarryingFactor(string habitat, string role)
	{
		IReadOnlyList<EcologySpeciesDef> pool = PoolFor(habitat);
		float sum = 0f;
		int count = 0;
		for (int i = 0; i < pool.Count; i++)
		{
			EcologySpeciesDef def = pool[i];
			if (def == null || (!string.IsNullOrEmpty(role) && def.role != role)) continue;
			float herbivory = def.dietHerbivore < 0f ? 0f : (def.dietHerbivore > 1f ? 1f : def.dietHerbivore);
			sum += (0.5f + def.bodySize * 0.2f + def.fecundity * 0.3f) * (0.5f + 0.5f * herbivory);
			count++;
		}
		return count == 0 ? 1f : sum / count;
	}

	/// <summary>捕猎产出系数：池内最大体型决定可猎取的肉量上限；空表返回 1。</summary>
	public static float HuntYieldFactor(string habitat)
	{
		IReadOnlyList<EcologySpeciesDef> pool = PoolFor(habitat);
		float maxSize = 0f;
		for (int i = 0; i < pool.Count; i++)
		{
			EcologySpeciesDef def = pool[i];
			if (def != null && def.role == "herbivore" && def.bodySize > maxSize) maxSize = def.bodySize;
		}
		return maxSize <= 0f ? 1f : 0.5f + maxSize * 0.5f;
	}

	private static void EnsureEmpty()
	{
		if (_byId == null)
		{
			_byId = new Dictionary<string, EcologySpeciesDef>();
			_byHabitat = new Dictionary<string, List<EcologySpeciesDef>>();
		}
	}
}
}
