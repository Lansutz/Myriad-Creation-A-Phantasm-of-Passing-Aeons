using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using UnityEngine;

namespace MyriadCreation.World.Hydrology
{
public class HydrologySystem
{
	private class MinHeap
	{
		private struct HeapNode
		{
			public int item;

			public float priority;
		}

		private readonly List<HeapNode> _nodes = new List<HeapNode>();

		public int Count => _nodes.Count;

		public void Enqueue(int item, float priority)
		{
			_nodes.Add(new HeapNode
			{
				item = item,
				priority = priority
			});
			int num = _nodes.Count - 1;
			while (num > 0)
			{
				int num2 = (num - 1) / 2;
				if (!(_nodes[num2].priority <= _nodes[num].priority))
				{
					List<HeapNode> nodes = _nodes;
					int index = num2;
					List<HeapNode> nodes2 = _nodes;
					int index2 = num;
					HeapNode value = _nodes[num];
					HeapNode value2 = _nodes[num2];
					nodes[index] = value;
					nodes2[index2] = value2;
					num = num2;
					continue;
				}
				break;
			}
		}

		public int Dequeue()
		{
			int item = _nodes[0].item;
			int index = _nodes.Count - 1;
			_nodes[0] = _nodes[index];
			_nodes.RemoveAt(index);
			int num = 0;
			while (true)
			{
				int num2 = 2 * num + 1;
				int num3 = 2 * num + 2;
				int num4 = num;
				if (num2 < _nodes.Count && _nodes[num2].priority < _nodes[num4].priority)
				{
					num4 = num2;
				}
				if (num3 < _nodes.Count && _nodes[num3].priority < _nodes[num4].priority)
				{
					num4 = num3;
				}
				if (num4 == num)
				{
					break;
				}
				List<HeapNode> nodes = _nodes;
				int index2 = num4;
				List<HeapNode> nodes2 = _nodes;
				int index3 = num;
				HeapNode value = _nodes[num];
				HeapNode value2 = _nodes[num4];
				nodes[index2] = value;
				nodes2[index3] = value2;
				num = num4;
			}
			return item;
		}
	}

	private static readonly int[] DX = new int[8] { 0, 1, 1, 1, 0, -1, -1, -1 };

	private static readonly int[] DY = new int[8] { -1, -1, 0, 1, 1, 1, 0, -1 };

	private readonly int _width;

	private readonly int _height;

	private readonly bool _wrapX;

	public float[] FilledElevation { get; private set; }

	public int[] FlowDirection { get; private set; }

	public float[] FlowAccumulation { get; private set; }

	public bool[] IsRiver { get; private set; }

	public int[] StreamOrder { get; private set; }

	public HydrologySystem(int width, int height, bool wrapX = true)
	{
		_width = width;
		_height = height;
		_wrapX = wrapX;
	}

	public void Run(float[] elevation, bool[] isLand, float riverThreshold = 100f)
	{
		int num = _width * _height;
		FilledElevation = new float[num];
		FlowDirection = new int[num];
		FlowAccumulation = new float[num];
		IsRiver = new bool[num];
		StreamOrder = new int[num];
		PriorityFlood(elevation, isLand);
		CalculateFlowAccumulation();
		ExtractRivers(riverThreshold);
		CalculateStreamOrder();
	}

	private void PriorityFlood(float[] elevation, bool[] isLand)
	{
		int num = _width * _height;
		Array.Copy(elevation, FilledElevation, num);
		Array.Fill(FlowDirection, -1);
		MinHeap minHeap = new MinHeap();
		bool[] array = new bool[num];
		for (int i = 0; i < _height; i++)
		{
			for (int j = 0; j < _width; j++)
			{
				int num2 = i * _width + j;
				bool flag = j == 0 || j == _width - 1 || i == 0 || i == _height - 1;
				if (!isLand[num2] | flag)
				{
					array[num2] = true;
					minHeap.Enqueue(num2, FilledElevation[num2]);
				}
			}
		}
		while (minHeap.Count > 0)
		{
			int num3 = minHeap.Dequeue();
			int num4 = num3 % _width;
			int num5 = num3 / _width;
			float num6 = FilledElevation[num3];
			for (int k = 0; k < 8; k++)
			{
				int num7 = num4 + DX[k];
				int num8 = num5 + DY[k];
				if (_wrapX)
				{
					if (num7 < 0)
					{
						num7 = _width - 1;
					}
					if (num7 >= _width)
					{
						num7 = 0;
					}
				}
				else if (num7 < 0 || num7 >= _width)
				{
					continue;
				}
				if (num8 < 0 || num8 >= _height)
				{
					continue;
				}
				int num9 = num8 * _width + num7;
				if (!array[num9])
				{
					array[num9] = true;
					if (FilledElevation[num9] < num6)
					{
						FilledElevation[num9] = num6;
					}
					FlowDirection[num9] = (k + 4) % 8;
					minHeap.Enqueue(num9, FilledElevation[num9]);
				}
			}
		}
	}

	private void CalculateFlowAccumulation()
	{
		int num = _width * _height;
		Array.Fill(FlowAccumulation, 1f);
		int[] array = new int[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = i;
		}
		Array.Sort(array, (int a, int b) => FilledElevation[b].CompareTo(FilledElevation[a]));
		int[] array2 = array;
		foreach (int num3 in array2)
		{
			int num4 = FlowDirection[num3];
			if (num4 < 0)
			{
				continue;
			}
			int num5 = num3 % _width;
			int num6 = num3 / _width;
			int num7 = num5 + DX[num4];
			int num8 = num6 + DY[num4];
			if (_wrapX)
			{
				if (num7 < 0)
				{
					num7 = _width - 1;
				}
				if (num7 >= _width)
				{
					num7 = 0;
				}
			}
			if (num8 >= 0 && num8 < _height)
			{
				int num9 = num8 * _width + num7;
				FlowAccumulation[num9] += FlowAccumulation[num3];
			}
		}
	}

	private void ExtractRivers(float threshold)
	{
		int num = _width * _height;
		for (int i = 0; i < num; i++)
		{
			IsRiver[i] = FlowAccumulation[i] >= threshold && FlowDirection[i] >= 0;
		}
	}

	private void CalculateStreamOrder()
	{
		int num = _width * _height;
		Array.Fill(StreamOrder, 0);
		int[] array = new int[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = i;
		}
		Array.Sort(array, (int a, int b) => FlowAccumulation[a].CompareTo(FlowAccumulation[b]));
		int[] array2 = array;
		foreach (int num3 in array2)
		{
			if (!IsRiver[num3])
			{
				continue;
			}
			if (StreamOrder[num3] == 0)
			{
				StreamOrder[num3] = 1;
			}
			int num4 = FlowDirection[num3];
			if (num4 < 0)
			{
				continue;
			}
			int num5 = num3 % _width;
			int num6 = num3 / _width;
			int num7 = num5 + DX[num4];
			int num8 = num6 + DY[num4];
			if (_wrapX)
			{
				if (num7 < 0)
				{
					num7 = _width - 1;
				}
				if (num7 >= _width)
				{
					num7 = 0;
				}
			}
			if (num8 < 0 || num8 >= _height)
			{
				continue;
			}
			int num9 = num8 * _width + num7;
			if (IsRiver[num9])
			{
				if (StreamOrder[num9] == 0)
				{
					StreamOrder[num9] = StreamOrder[num3];
				}
				else if (StreamOrder[num9] == StreamOrder[num3])
				{
					StreamOrder[num9]++;
				}
				else
				{
					StreamOrder[num9] = Mathf.Max(StreamOrder[num9], StreamOrder[num3]);
				}
			}
		}
	}

	/// <summary>
	/// 把河网写回地块：除 isRiver 外，同时写河序、汇流面积与水力几何量（宽/深/流速）。
	/// 宽度/深度/流速用 Leopold &amp; Maddock 水力几何的经验指数（W∝Q^0.5、D∝Q^0.35、V∝Q^0.1），
	/// 系数按本作地块尺度标定；没有这些字段，可通行判定与河运会永远读到 0。
	/// </summary>
	public void ApplyToTiles(TileData[] tiles)
	{
		int num = Math.Min(tiles.Length, IsRiver.Length);
		for (int i = 0; i < num; i++)
		{
			bool river = IsRiver[i];
			tiles[i].isRiver = river;
			if (!river)
			{
				tiles[i].riverStreamOrder = 0;
				tiles[i].riverFlowAccumulation = 0f;
				tiles[i].riverWidthMeters = 0f;
				tiles[i].riverDepthMeters = 0f;
				tiles[i].riverCurrentMps = 0f;
				continue;
			}
			float q = Mathf.Max(1f, FlowAccumulation[i]);
			tiles[i].riverStreamOrder = StreamOrder[i];
			tiles[i].riverFlowAccumulation = q;
			tiles[i].riverWidthMeters = 1.2f * Mathf.Sqrt(q);
			tiles[i].riverDepthMeters = 0.25f * Mathf.Pow(q, 0.35f);
			tiles[i].riverCurrentMps = 0.5f * Mathf.Pow(q, 0.1f);
		}
	}

	public int GetRiverTileCount()
	{
		int num = 0;
		bool[] isRiver = IsRiver;
		for (int i = 0; i < isRiver.Length; i++)
		{
			if (isRiver[i])
			{
				num++;
			}
		}
		return num;
	}

	public int GetMaxStreamOrder()
	{
		int num = 0;
		int[] streamOrder = StreamOrder;
		foreach (int num2 in streamOrder)
		{
			if (num2 > num)
			{
				num = num2;
			}
		}
		return num;
	}
}
}
