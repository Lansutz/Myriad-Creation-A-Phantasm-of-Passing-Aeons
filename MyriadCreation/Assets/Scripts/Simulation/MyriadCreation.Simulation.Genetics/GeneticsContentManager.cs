using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MyriadCreation.Simulation.Genetics
{
public sealed class GeneticsContentManager
{
	private readonly Dictionary<string, GenomeDef> _genomes = new Dictionary<string, GenomeDef>();

	private readonly Dictionary<string, SpeciesDef> _species = new Dictionary<string, SpeciesDef>();

	private readonly Dictionary<string, AppearanceStyleDef> _styles = new Dictionary<string, AppearanceStyleDef>();

	public IReadOnlyDictionary<string, GenomeDef> Genomes => _genomes;

	public IReadOnlyDictionary<string, SpeciesDef> Species => _species;

	public IReadOnlyDictionary<string, AppearanceStyleDef> AppearanceStyles => _styles;

	public static GeneticsContentManager CreateDefault()
	{
		GeneticsContentManager geneticsContentManager = new GeneticsContentManager();
		GenomeDef genome = GeneticsCatalog.CreateHumanGenome();
		geneticsContentManager.Register(genome);
		foreach (SpeciesDef item in GeneticsCatalog.CreateDefaultSpecies())
		{
			geneticsContentManager.Register(item);
		}
		return geneticsContentManager;
	}

	public bool Register(GenomeDef genome)
	{
		if (genome == null || string.IsNullOrEmpty(genome.genomeId))
		{
			return false;
		}
		_genomes[genome.genomeId] = genome;
		return true;
	}

	public bool Register(SpeciesDef species)
	{
		if (species == null || string.IsNullOrEmpty(species.speciesId))
		{
			return false;
		}
		_species[species.speciesId] = species;
		return true;
	}

	public bool Register(AppearanceStyleDef style)
	{
		if (style == null || string.IsNullOrEmpty(style.styleId))
		{
			return false;
		}
		_styles[style.styleId] = style;
		return true;
	}

	public bool TryGetGenome(string id, out GenomeDef genome)
	{
		return _genomes.TryGetValue(id ?? "", out genome);
	}

	public bool TryGetSpecies(string id, out SpeciesDef species)
	{
		return _species.TryGetValue(id ?? "", out species);
	}

	public bool TryGetStyle(string id, out AppearanceStyleDef style)
	{
		return _styles.TryGetValue(id ?? "", out style);
	}

	public List<string> Validate()
	{
		List<string> list = new List<string>();
		foreach (GenomeDef value in _genomes.Values)
		{
			if (string.IsNullOrEmpty(value.genomeId))
			{
				list.Add("基因组缺少 id");
			}
			if (value.loci == null || value.loci.Count == 0)
			{
				list.Add("基因组 " + value.genomeId + " 没有基因座");
			}
			if (value.loci == null)
			{
				continue;
			}
			foreach (GenomeLocusDef item in value.loci)
			{
				if (item.alleles == null || item.alleles.Count == 0)
				{
					list.Add("基因座 " + item.locusId + " 没有等位基因");
				}
			}
		}
		foreach (SpeciesDef value2 in _species.Values)
		{
			if (!string.IsNullOrEmpty(value2.genomeId) && !_genomes.ContainsKey(value2.genomeId))
			{
				list.Add("物种 " + value2.speciesId + " 引用不存在的基因组 " + value2.genomeId);
			}
		}
		return list;
	}

	public string ToJson()
	{
		return JsonUtility.ToJson((object)new GeneticsContentPack
		{
			genomes = new List<GenomeDef>(_genomes.Values),
			species = new List<SpeciesDef>(_species.Values),
			appearanceStyles = new List<AppearanceStyleDef>(_styles.Values)
		}, true);
	}

	public bool LoadFromJson(string json, out string error)
	{
		error = "";
		if (string.IsNullOrWhiteSpace(json))
		{
			error = "JSON 为空";
			return false;
		}
		try
		{
			GeneticsContentPack geneticsContentPack = JsonUtility.FromJson<GeneticsContentPack>(json);
			if (geneticsContentPack == null)
			{
				error = "解析失败";
				return false;
			}
			_genomes.Clear();
			_species.Clear();
			_styles.Clear();
			if (geneticsContentPack.genomes != null)
			{
				foreach (GenomeDef genome in geneticsContentPack.genomes)
				{
					Register(genome);
				}
			}
			if (geneticsContentPack.species != null)
			{
				foreach (SpeciesDef item in geneticsContentPack.species)
				{
					Register(item);
				}
			}
			if (geneticsContentPack.appearanceStyles != null)
			{
				foreach (AppearanceStyleDef appearanceStyle in geneticsContentPack.appearanceStyles)
				{
					Register(appearanceStyle);
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	public bool SaveToFile(string path, out string error)
	{
		error = "";
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
			File.WriteAllText(path, ToJson());
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	public bool LoadFromFile(string path, out string error)
	{
		error = "";
		try
		{
			if (!File.Exists(path))
			{
				error = "文件不存在";
				return false;
			}
			return LoadFromJson(File.ReadAllText(path), out error);
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}
}
}
