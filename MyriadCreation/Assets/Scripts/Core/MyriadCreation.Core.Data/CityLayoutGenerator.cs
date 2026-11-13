using System;
using System.Collections.Generic;

namespace MyriadCreation.Core.Data
{
public struct CityRect
{
	public int x;
	public int y;
	public int width;
	public int height;
}

public sealed class CityLot
{
	public string blockId = "";
	public int districtType;
	public CityRect rect;
}

/// <summary>俯视图布局输入：从 SettlementBlock 解耦，Rendering/UI 只依赖 Core。</summary>
public struct CityBlockSpec
{
	public string blockId;
	public float areaRatio;
	public int districtType;
	public bool enabled;
	public bool isWall;
}

/// <summary>
/// 城市俯视图布局（P4.2）：不存档、读档按区块数据重算；只给渲染层提供矩形分区与城墙外环。
/// 确定性 = 按 blockId 稳定排序 + 面积比例切片，不依赖随机数。
/// </summary>
public sealed class CityLayoutPlan
{
	public int width;
	public int height;
	public bool hasWall;
	public CityRect wall;
	public List<CityLot> lots = new List<CityLot>();
}

public static class CityLayoutGenerator
{
	public const int DefaultWidth = 100;
	public const int DefaultHeight = 100;
	public const int RoadGap = 3;

	/// <summary>按 areaRatio 把城市切成竖条街区，街区之间留道路；有任一带墙区块则加外环。</summary>
	public static CityLayoutPlan Build(IReadOnlyList<CityBlockSpec> blocks, int width = DefaultWidth, int height = DefaultHeight)
	{
		var plan = new CityLayoutPlan { width = width, height = height };
		if (blocks == null || blocks.Count == 0) return plan;

		List<CityBlockSpec> ordered = new List<CityBlockSpec>(blocks.Count);
		for (int i = 0; i < blocks.Count; i++)
		{
			CityBlockSpec block = blocks[i];
			if (block.enabled) ordered.Add(block);
		}
		if (ordered.Count == 0) return plan;
		ordered.Sort((a, b) => string.CompareOrdinal(a.blockId, b.blockId));

		float totalWeight = 0f;
		for (int i = 0; i < ordered.Count; i++)
		{
			totalWeight += Math.Max(0.0001f, ordered[i].areaRatio);
		}
		int usableWidth = Math.Max(1, width - RoadGap * (ordered.Count - 1));
		int cursor = 0;
		for (int i = 0; i < ordered.Count; i++)
		{
			CityBlockSpec block = ordered[i];
			float share = Math.Max(0.0001f, block.areaRatio) / totalWeight;
			int lotWidth = i == ordered.Count - 1 ? Math.Max(1, width - cursor) : Math.Max(1, (int)Math.Round(usableWidth * share));
			lotWidth = Math.Min(lotWidth, Math.Max(1, width - cursor));
			plan.lots.Add(new CityLot
			{
				blockId = block.blockId,
				districtType = block.districtType,
				rect = new CityRect { x = cursor, y = 0, width = lotWidth, height = height }
			});
			cursor += lotWidth;
			if (i < ordered.Count - 1) cursor += RoadGap;
			if (cursor >= width) break;
		}

		for (int i = 0; i < ordered.Count; i++)
		{
			if (ordered[i].isWall)
			{
				plan.hasWall = true;
				plan.wall = new CityRect { x = 0, y = 0, width = width, height = height };
				break;
			}
		}
		return plan;
	}
}
}
