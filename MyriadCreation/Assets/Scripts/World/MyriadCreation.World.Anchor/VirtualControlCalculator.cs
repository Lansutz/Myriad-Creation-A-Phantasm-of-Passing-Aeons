using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.World.Settlement;
using UnityEngine;

namespace MyriadCreation.World.Anchor
{
public static class VirtualControlCalculator
{
	public const float MinTriangleAngleDeg = 15f;

	public static VirtualControlResult Calculate(Dictionary<int, SettlementData> settlements, TileData[] tiles, int mapWidth, int mapHeight)
	{
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		VirtualControlResult virtualControlResult = new VirtualControlResult();
		if (settlements == null || settlements.Count == 0 || tiles == null)
		{
			return virtualControlResult;
		}
		Dictionary<int, List<(int, Vector2)>> dictionary = new Dictionary<int, List<(int, Vector2)>>();
		foreach (KeyValuePair<int, SettlementData> settlement in settlements)
		{
			SettlementData value = settlement.Value;
			if (!value.IsMajorSettlement || value.tileIndex < 0 || value.tileIndex >= tiles.Length)
			{
				continue;
			}
			int ownerRealmId = tiles[value.tileIndex].ownerRealmId;
			if (ownerRealmId >= 0)
			{
				if (!dictionary.ContainsKey(ownerRealmId))
				{
					dictionary[ownerRealmId] = new List<(int, Vector2)>();
				}
				int num = value.tileIndex % mapWidth;
				int num2 = value.tileIndex / mapWidth;
				dictionary[ownerRealmId].Add((settlement.Key, new Vector2((float)num, (float)num2)));
			}
		}
		foreach (KeyValuePair<int, List<(int, Vector2)>> item in dictionary)
		{
			int key = item.Key;
			List<(int, Vector2)> value2 = item.Value;
			if (value2.Count < 3)
			{
				continue;
			}
			Vector2[] array = new Vector2[value2.Count];
			for (int i = 0; i < value2.Count; i++)
			{
				array[i] = value2[i].Item2;
			}
			foreach (ControlTriangle item2 in BowyerWatson(array))
			{
				if (item2.isValid && !(item2.GetMinAngleDeg(array) < 15f))
				{
					ControlTriangle controlTriangle = item2;
					controlTriangle.ownerRealmId = key;
					virtualControlResult.triangles.Add(controlTriangle);
					MarkTriangleInteriorTiles(controlTriangle, array, mapWidth, mapHeight, key, virtualControlResult);
				}
			}
			CalculateConnections(value2, tiles, mapWidth, mapHeight, key, virtualControlResult);
		}
		return virtualControlResult;
	}

	private static List<ControlTriangle> BowyerWatson(Vector2[] points)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		List<ControlTriangle> list = new List<ControlTriangle>();
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		foreach (Vector2 val in points)
		{
			num = Mathf.Min(num, val.x);
			num2 = Mathf.Min(num2, val.y);
			num3 = Mathf.Max(num3, val.x);
			num4 = Mathf.Max(num4, val.y);
		}
		float num5 = num3 - num;
		float num6 = num4 - num2;
		float num7 = Mathf.Max(num5, num6) * 10f;
		float num8 = (num + num3) * 0.5f;
		float num9 = (num2 + num4) * 0.5f;
		Vector2[] points2 = new List<Vector2>(points)
		{
			new Vector2(num8 - num7, num9 - num7),
			new Vector2(num8 + num7, num9 - num7),
			new Vector2(num8, num9 + num7)
		}.ToArray();
		int n = points.Length;
		list.Add(new ControlTriangle(n, n + 1, n + 2, points2));
		for (int j = 0; j < n; j++)
		{
			List<ControlTriangle> list2 = new List<ControlTriangle>();
			foreach (ControlTriangle item3 in list)
			{
				if (item3.InCircumcircle(points[j]))
				{
					list2.Add(item3);
				}
			}
			List<(int, int)> list3 = new List<(int, int)>();
			foreach (ControlTriangle item4 in list2)
			{
				(int, int)[] array = new (int, int)[3]
				{
					(item4.a, item4.b),
					(item4.b, item4.c),
					(item4.c, item4.a)
				};
				for (int i = 0; i < array.Length; i++)
				{
					(int, int) item = array[i];
					bool flag = false;
					foreach (ControlTriangle item5 in list2)
					{
						if (item5.Equals(item4))
						{
							continue;
						}
						(int, int)[] array2 = new (int, int)[3]
						{
							(item5.a, item5.b),
							(item5.b, item5.c),
							(item5.c, item5.a)
						};
						for (int k = 0; k < array2.Length; k++)
						{
							(int, int) tuple = array2[k];
							if ((item.Item1 == tuple.Item1 && item.Item2 == tuple.Item2) || (item.Item1 == tuple.Item2 && item.Item2 == tuple.Item1))
							{
								flag = true;
								break;
							}
						}
						if (flag)
						{
							break;
						}
					}
					if (!flag)
					{
						list3.Add(item);
					}
				}
			}
			foreach (ControlTriangle item6 in list2)
			{
				list.Remove(item6);
			}
			foreach (var item7 in list3)
			{
				ControlTriangle item2 = new ControlTriangle(item7.Item1, item7.Item2, j, points2);
				if (item2.isValid)
				{
					list.Add(item2);
				}
			}
		}
		list.RemoveAll((ControlTriangle t) => t.a >= n || t.b >= n || t.c >= n);
		return list;
	}

	private static void MarkTriangleInteriorTiles(ControlTriangle tri, Vector2[] points, int mapWidth, int mapHeight, int realmId, VirtualControlResult result)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = points[tri.a];
		Vector2 val2 = points[tri.b];
		Vector2 val3 = points[tri.c];
		int num = Mathf.Max(0, (int)Mathf.Min(new float[3] { val.x, val2.x, val3.x }) - 1);
		int num2 = Mathf.Min(mapWidth - 1, (int)Mathf.Max(new float[3] { val.x, val2.x, val3.x }) + 1);
		int num3 = Mathf.Max(0, (int)Mathf.Min(new float[3] { val.y, val2.y, val3.y }) - 1);
		int num4 = Mathf.Min(mapHeight - 1, (int)Mathf.Max(new float[3] { val.y, val2.y, val3.y }) + 1);
		for (int i = num3; i <= num4; i++)
		{
			for (int j = num; j <= num2; j++)
			{
				if (PointInTriangle(new Vector2((float)j + 0.5f, (float)i + 0.5f), val, val2, val3))
				{
					int key = i * mapWidth + j;
					if (!result.virtualControlTiles.ContainsKey(key))
					{
						result.virtualControlTiles[key] = realmId;
					}
				}
			}
		}
	}

	private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		float num = Sign(p, a, b);
		float num2 = Sign(p, b, c);
		float num3 = Sign(p, c, a);
		bool flag = num < 0f || num2 < 0f || num3 < 0f;
		bool flag2 = num > 0f || num2 > 0f || num3 > 0f;
		return !(flag & flag2);
	}

	private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}

	private static void CalculateConnections(List<(int anchorId, Vector2 pos)> points, TileData[] tiles, int mapWidth, int mapHeight, int realmId, VirtualControlResult result)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < points.Count; i++)
		{
			for (int j = i + 1; j < points.Count; j++)
			{
				if (HasOccupiedTileAlongLine(points[i].pos, points[j].pos, tiles, mapWidth, mapHeight, realmId))
				{
					result.connections.Add((points[i].anchorId, points[j].anchorId));
				}
			}
		}
	}

	private static bool HasOccupiedTileAlongLine(Vector2 p1, Vector2 p2, TileData[] tiles, int mapWidth, int mapHeight, int realmId)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		float num = Vector2.Distance(p1, p2);
		int num2 = Mathf.Max(2, (int)num);
		for (int i = 1; i < num2; i++)
		{
			float num3 = (float)i / (float)num2;
			int num4 = (int)(p1.x + (p2.x - p1.x) * num3);
			int num5 = (int)(p1.y + (p2.y - p1.y) * num3);
			if (num4 >= 0 && num4 < mapWidth && num5 >= 0 && num5 < mapHeight)
			{
				int num6 = num5 * mapWidth + num4;
				if (num6 >= 0 && num6 < tiles.Length && tiles[num6].ownerRealmId == realmId)
				{
					return true;
				}
			}
		}
		return false;
	}
}
}
