using System;
using System.Collections.Generic;

namespace MyriadCreation.Core.Data
{
/// <summary>
/// 人口块的唯一写入口（P2.5 C）：增删、缩放、改文化/信仰、碎片合并。
/// 地块人口块的权威仍是 `TileData.populationBlocks`，这里不持有第二份人口。
/// </summary>
public static class PopulationLedger
{
	public static List<PopulationBlock> Ensure(ref TileData tile)
	{
		if (tile.populationBlocks == null)
		{
			tile.populationBlocks = new List<PopulationBlock>();
		}
		return tile.populationBlocks;
	}

	public static float Total(TileData tile)
	{
		if (tile.populationBlocks == null)
		{
			return 0f;
		}
		float sum = 0f;
		for (int i = 0; i < tile.populationBlocks.Count; i++)
		{
			sum += tile.populationBlocks[i].count;
		}
		return sum;
	}

	/// <summary>增减总人口：正数并入首块（沿用旧行为），负数从尾部逐块扣除。</summary>
	public static void AddCount(ref TileData tile, float amount)
	{
		if (amount == 0f)
		{
			return;
		}
		List<PopulationBlock> blocks = Ensure(ref tile);
		if (amount > 0f)
		{
			if (blocks.Count > 0)
			{
				PopulationBlock first = blocks[0];
				first.count += amount;
				blocks[0] = first;
			}
			else
			{
				blocks.Add(new PopulationBlock { count = amount });
			}
			return;
		}
		float remaining = -amount;
		for (int i = blocks.Count - 1; i >= 0 && remaining > 0f; i--)
		{
			PopulationBlock block = blocks[i];
			if (block.count <= remaining)
			{
				remaining -= block.count;
				blocks.RemoveAt(i);
			}
			else
			{
				block.count -= remaining;
				blocks[i] = block;
				remaining = 0f;
			}
		}
	}

	/// <summary>并入一个块：同属性块合并，否则追加。</summary>
	public static void Add(List<PopulationBlock> blocks, PopulationBlock block)
	{
		if (blocks == null || block.count <= 0f)
		{
			return;
		}
		for (int i = 0; i < blocks.Count; i++)
		{
			PopulationBlock existing = blocks[i];
			if (existing.cultureId == block.cultureId
				&& existing.faithId == block.faithId
				&& existing.raceId == block.raceId
				&& existing.socialClass == block.socialClass
				&& existing.profession == block.profession
				&& existing.economicClass == block.economicClass
				&& existing.stratum == block.stratum
				&& existing.occupations == block.occupations)
			{
				existing.count += block.count;
				existing.culturePenetration = Math.Max(existing.culturePenetration, block.culturePenetration);
				blocks[i] = existing;
				return;
			}
		}
		blocks.Add(block);
	}

	public static void Scale(List<PopulationBlock> blocks, int index, float factor)
	{
		if (blocks == null || index < 0 || index >= blocks.Count || factor < 0f)
		{
			return;
		}
		PopulationBlock block = blocks[index];
		block.count *= factor;
		blocks[index] = block;
	}

	public static void RemoveFromIndex(List<PopulationBlock> blocks, int index, float amount)
	{
		if (blocks == null || index < 0 || index >= blocks.Count || amount <= 0f)
		{
			return;
		}
		PopulationBlock block = blocks[index];
		block.count = Math.Max(0f, block.count - amount);
		blocks[index] = block;
	}

	/// <summary>从某块拆出 amount 移到新块（按 template 的属性）；拆分后自动合并同属性块。</summary>
	public static void SplitToNewBlock(List<PopulationBlock> blocks, int index, float amount, PopulationBlock template)
	{
		if (blocks == null || index < 0 || index >= blocks.Count || amount <= 0f)
		{
			return;
		}
		PopulationBlock source = blocks[index];
		if (source.count <= amount)
		{
			return;
		}
		source.count -= amount;
		blocks[index] = source;
		template.count = amount;
		Add(blocks, template);
	}

	/// <summary>改文化：并入同属性目标文化块，否则原位改写。</summary>
	public static void ChangeCulture(List<PopulationBlock> blocks, int index, int targetCultureId, float penetration = -1f)
	{
		ChangeIdentity(blocks, index, targetCultureId, null, penetration);
	}

	public static void ChangeFaith(List<PopulationBlock> blocks, int index, int targetFaithId)
	{
		ChangeIdentity(blocks, index, null, targetFaithId, -1f);
	}

	private static void ChangeIdentity(List<PopulationBlock> blocks, int index, int? targetCultureId, int? targetFaithId, float penetration)
	{
		if (blocks == null || index < 0 || index >= blocks.Count)
		{
			return;
		}
		PopulationBlock converted = blocks[index];
		if (targetCultureId.HasValue) converted.cultureId = targetCultureId.Value;
		if (targetFaithId.HasValue) converted.faithId = targetFaithId.Value;
		if (penetration >= 0f) converted.culturePenetration = penetration;
		for (int i = 0; i < blocks.Count; i++)
		{
			if (i == index)
			{
				continue;
			}
			PopulationBlock other = blocks[i];
			if (SamePopulation(other, converted))
			{
				other.count += converted.count;
				other.culturePenetration = Math.Max(other.culturePenetration, converted.culturePenetration);
				blocks[i] = other;
				blocks.RemoveAt(index);
				return;
			}
		}
		blocks[index] = converted;
	}

	public static void Prune(List<PopulationBlock> blocks, float minCount = 0.02f)
	{
		if (blocks == null)
		{
			return;
		}
		for (int i = blocks.Count - 1; i >= 0; i--)
		{
			if (blocks[i].count < minCount)
			{
				blocks.RemoveAt(i);
			}
		}
	}

	private static bool SamePopulation(PopulationBlock a, PopulationBlock b)
	{
		return a.raceId == b.raceId
			&& a.faithId == b.faithId
			&& a.cultureId == b.cultureId
			&& a.socialClass == b.socialClass
			&& a.profession == b.profession
			&& a.economicClass == b.economicClass
			&& a.stratum == b.stratum
			&& a.occupations == b.occupations;
	}
}
}
