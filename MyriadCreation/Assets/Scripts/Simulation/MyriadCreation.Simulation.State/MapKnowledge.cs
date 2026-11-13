using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;

namespace MyriadCreation.Simulation.State
{
/// <summary>
/// 地图知识层（S19 第一刀）：每个政权各自记录“哪些地块已知”。
/// 位图按地块数增长；写入只来自所有权、军队斥候范围与后续的商队/探险，读取不写状态。
/// 存档用 Base64 位图（每 8 格 1 字节），不存逐格列表。
/// </summary>
[Serializable]
public sealed class MapKnowledge
{
	private readonly Dictionary<int, byte[]> _bits = new Dictionary<int, byte[]>();

	private int _tileCount;

	public int TileCount => _tileCount;

	public IReadOnlyDictionary<int, byte[]> Realms => _bits;

	public void Clear()
	{
		_bits.Clear();
		_tileCount = 0;
	}

	public void EnsureCapacity(int tileCount)
	{
		if (tileCount > _tileCount) _tileCount = tileCount;
	}

	public void MarkKnown(int realmId, int tileIndex)
	{
		if (realmId < 0 || tileIndex < 0) return;
		EnsureCapacity(tileIndex + 1);
		byte[] bits = GetOrCreate(realmId);
		int byteIndex = tileIndex >> 3;
		if (byteIndex >= bits.Length) Array.Resize(ref bits, byteIndex + 1);
		bits[byteIndex] |= (byte)(1 << (tileIndex & 7));
		_bits[realmId] = bits;
	}

	public bool IsKnown(int realmId, int tileIndex)
	{
		if (realmId < 0 || tileIndex < 0 || !_bits.TryGetValue(realmId, out byte[] bits)) return false;
		int byteIndex = tileIndex >> 3;
		return byteIndex < bits.Length && (bits[byteIndex] & (1 << (tileIndex & 7))) != 0;
	}

	/// <summary>以 center 为圆心、切比雪夫半径 radius 揭示（斥候/行军视野用方形网格近似）。</summary>
	public void RevealRadius(int realmId, int centerTile, int radius, int width, int height)
	{
		if (realmId < 0 || centerTile < 0 || width <= 0 || height <= 0) return;
		int cx = centerTile % width;
		int cy = centerTile / width;
		if (cy >= height) return;
		int r = Math.Max(0, radius);
		for (int y = Math.Max(0, cy - r); y <= Math.Min(height - 1, cy + r); y++)
		{
			for (int x = Math.Max(0, cx - r); x <= Math.Min(width - 1, cx + r); x++)
			{
				MarkKnown(realmId, y * width + x);
			}
		}
	}

	/// <summary>政权对自己拥有的地块天然已知（含一圈邻地）。</summary>
	public void SeedOwnership(TileData[] tiles, int width, int height)
	{
		if (tiles == null || width <= 0 || height <= 0) return;
		for (int i = 0; i < tiles.Length; i++)
		{
			if (!tiles[i].exists) continue;
			int owner = tiles[i].ownerRealmId;
			if (owner < 0) continue;
			RevealRadius(owner, i, 1, width, height);
		}
	}

	public int KnownCount(int realmId)
	{
		if (!_bits.TryGetValue(realmId, out byte[] bits)) return 0;
		int count = 0;
		for (int i = 0; i < bits.Length; i++)
		{
			byte b = bits[i];
			while (b != 0)
			{
				count += b & 1;
				b >>= 1;
			}
		}
		return count;
	}

	public List<RealmKnowledgeSaveData> Capture()
	{
		List<RealmKnowledgeSaveData> result = new List<RealmKnowledgeSaveData>(_bits.Count);
		foreach (KeyValuePair<int, byte[]> pair in _bits)
		{
			result.Add(new RealmKnowledgeSaveData
			{
				realmId = pair.Key,
				bitsBase64 = pair.Value == null || pair.Value.Length == 0 ? "" : Convert.ToBase64String(pair.Value)
			});
		}
		return result;
	}

	public void Restore(IEnumerable<RealmKnowledgeSaveData> data, int tileCount)
	{
		Clear();
		EnsureCapacity(tileCount);
		if (data == null) return;
		foreach (RealmKnowledgeSaveData item in data)
		{
			if (item == null || item.realmId < 0 || string.IsNullOrEmpty(item.bitsBase64)) continue;
			try
			{
				_bits[item.realmId] = Convert.FromBase64String(item.bitsBase64);
			}
			catch (FormatException)
			{
				// 坏档段：该政权退回“全未知”，不让整档读失败。
			}
		}
	}

	private byte[] GetOrCreate(int realmId)
	{
		if (_bits.TryGetValue(realmId, out byte[] bits)) return bits;
		bits = new byte[Math.Max(1, (_tileCount + 7) / 8)];
		_bits[realmId] = bits;
		return bits;
	}
}

[Serializable]
public sealed class RealmKnowledgeSaveData
{
	public int realmId;
	public string bitsBase64;
}
}
