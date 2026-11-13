using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using UnityEngine;

namespace MyriadCreation.World.Terrain;

public class SeaLandGenerator
{
	private enum AdjacencyMode
	{
		None,
		All,
		LandFragmentCandidates
	}

	private readonly WorldConfig _config;

	private readonly TileData[] _tiles;

	private readonly int _width;

	private readonly int _height;

	private Dictionary<int, List<int>> _seaConnectGroups = new Dictionary<int, List<int>>();

	private int _nextSeaConnectId;

	private float[] _fragmentNoise;

	private float _cachedSeaThreshold;

	private int[] _neighbourStart;

	private int[] _neighbours;

	private bool _neighbourTableWrapX;

	private bool _neighbourTableWrapY;

	private float[] _thresholdBuffer;

	private bool[] _connectVisited;

	private int[] _bfsQueue;

	private int[] _changedStamp;

	private int _changedStampVersion;

	private int[] _affectedStamp;

	private int _affectedStampVersion;

	private readonly List<int> _lastChangedTiles = new List<int>();

	private readonly List<int> _affectedTiles = new List<int>();

	private readonly List<int> _fragChangedTiles = new List<int>();

	private static readonly int[] EvenHexDx = new int[6] { -1, 0, -1, 1, -1, 0 };

	private static readonly int[] EvenHexDy = new int[6] { -1, -1, 0, 0, 1, 1 };

	private static readonly int[] OddHexDx = new int[6] { 0, 1, -1, 1, 0, 1 };

	private static readonly int[] OddHexDy = new int[6] { -1, -1, 0, 0, 1, 1 };

	private const uint LandFragmentSeedSalt = 0x9E3779B9u;

	private const uint CoastFragmentLandToSeaSalt = 0x85EBCA6Bu;

	private const uint CoastFragmentSeaToLandSalt = 0xC2B2AE35u;

	/// <summary>
	/// 重算路径的随机种子：主生成路径（噪声/板块构造）本就按世界 seed 确定，而 RecalculateAll/画笔重算路径
	/// 以前用未播种的 UnityEngine.Random，导致同 seed 全量重算不可复现。这里保存重算种子，只供重算路径的
	/// 局部确定性散列使用，不触碰 UnityEngine.Random 的全局状态（不影响其它系统的随机）。
	/// </summary>
	private int _recalcSeed;

	/// <param name="recalcSeed">世界 seed：既驱动重算路径的确定性散列，也派生出破碎噪声（默认 0；GameWorld 传入 randomSeed）。</param>
	public SeaLandGenerator(WorldConfig config, TileData[] tiles, int width, int height, int recalcSeed = 0)
	{
		_config = config;
		_tiles = tiles;
		_width = width;
		_height = height;
		_recalcSeed = recalcSeed;
		// 破碎化（landFragment/coastFragment）依赖逐地块噪声：默认全 0 会让两条分支永远不触发（死路径）。
		// 这里按世界 seed 生成确定性值噪声；测试/画笔仍可通过 SetFragmentNoise 显式覆盖。
		_fragmentNoise = BuildFragmentNoise(tiles.Length, width, height, recalcSeed);
		BuildNeighbourTable();
	}

	/// <summary>
	/// 最近一次 RecalculateAll / RecalculateDirty 中被改写过的地块索引（按首次改写顺序、无重复）。
	/// 语义：该地块的 isLand / isCoast / oceanTier / oceanDepth01 / waterAdjacentWeight / seaConnectId /
	/// elevation01 中任意一项在本次重算的某个写入步骤发生变化（后续步骤可能再次覆盖同一地块）。
	/// 注意：初始开图时 elevation01 会被相对海平面整体折算，因此几乎全部地块都会出现在集合里；
	/// 它表示"本次重算改写了哪些地块"，不是"仅破碎化改了哪些地块"。
	/// 供一致性校验（如 task-51）判断重算前后哪些地块需要复核；跨线程只读，重算时勿并发访问。
	/// </summary>
	public IReadOnlyList<int> LastChangedTileIndices => _lastChangedTiles;

	public void SetFragmentNoise(float[] noise)
	{
		if (noise != null && noise.Length == _tiles.Length)
		{
			_fragmentNoise = noise;
		}
	}

	private const int FragmentNoiseOctaves = 3;

	private const int FragmentNoiseOctaveSeedStride = 1013;

	/// <summary>
	/// 按世界 seed 派生逐地块破碎噪声（0~1）：2D 平滑值噪声（整数格点哈希 + smoothstep 双线性插值），
	/// 叠加 <see cref="FragmentNoiseOctaves"/> 层分形；纯整数/浮点运算，不使用 UnityEngine.Random，
	/// 同一 (tileCount,width,height,seed) 多次构造结果逐位一致，跨平台确定。
	/// </summary>
	private static float[] BuildFragmentNoise(int tileCount, int width, int height, int seed)
	{
		var noise = new float[tileCount];
		if (tileCount <= 0)
		{
			return noise;
		}
		int safeWidth = width > 0 ? width : tileCount;
		int safeHeight = height > 0 ? height : 1;
		int shortSide = Mathf.Min(safeWidth, safeHeight);
		// 噪声格点基准尺度：短边的 1/8（至少 4 地块），形成大陆尺度的块状起伏而非逐格白噪声。
		float cellSize = Mathf.Max(4f, shortSide * 0.125f);
		for (int i = 0; i < noise.Length; i++)
		{
			int x = i % safeWidth;
			int y = i / safeWidth;
			if (y >= safeHeight)
			{
				y = safeHeight - 1; // width*height 与 tiles.Length 不一致时防御
			}
			noise[i] = SampleFractalValueNoise(x / cellSize, y / cellSize, seed);
		}
		return noise;
	}

	private static float SampleFractalValueNoise(float x, float y, int seed)
	{
		float sum = 0f;
		float amplitude = 0.5f;
		float frequency = 1f;
		float normalizer = 0f;
		for (int octave = 0; octave < FragmentNoiseOctaves; octave++)
		{
			sum += amplitude * SampleValueNoise(x * frequency, y * frequency, seed + octave * FragmentNoiseOctaveSeedStride);
			normalizer += amplitude;
			amplitude *= 0.5f;
			frequency *= 2f;
		}
		return normalizer > 0f ? sum / normalizer : 0.5f;
	}

	private static float SampleValueNoise(float x, float y, int seed)
	{
		int x0 = Mathf.FloorToInt(x);
		int y0 = Mathf.FloorToInt(y);
		float fx = x - x0;
		float fy = y - y0;
		float sx = fx * fx * (3f - 2f * fx);
		float sy = fy * fy * (3f - 2f * fy);
		float n00 = HashToUnit01(HashFragmentLattice(x0, y0, seed));
		float n10 = HashToUnit01(HashFragmentLattice(x0 + 1, y0, seed));
		float n01 = HashToUnit01(HashFragmentLattice(x0, y0 + 1, seed));
		float n11 = HashToUnit01(HashFragmentLattice(x0 + 1, y0 + 1, seed));
		float nx0 = n00 + (n10 - n00) * sx;
		float nx1 = n01 + (n11 - n01) * sx;
		return nx0 + (nx1 - nx0) * sy;
	}

	private static uint HashFragmentLattice(int x, int y, int seed)
	{
		unchecked
		{
			uint hash = (uint)seed * 2166136261u;
			hash = (hash ^ (uint)x) * 16777619u;
			hash = (hash ^ (uint)y) * 16777619u;
			hash ^= hash >> 13;
			hash *= 0x5BD1E995u;
			hash ^= hash >> 15;
			return hash;
		}
	}

	private static float HashToUnit01(uint hash)
	{
		return (hash & 0xFFFFFFu) / 16777216f; // [0, 1)
	}

	public void RecalculateAll()
	{
		ResetChangedTiles();
		EnsureRecalcState();
		float seaThreshold = CalculateSeaLevelThresholdFromDistribution();
		// 初始逐地块重算：只有"陆地破碎化候选"需要 waterAdjacentWeight（ApplyLandFragmentation 会读）；
		// 其余地块的邻接字段随后会被 ApplyLandFragmentation 里的全量邻接重算覆盖，跳过可省一遍邻域遍历。
		// 例外：landFragment 关闭时没有那次全量邻接重算，必须像旧实现一样逐地块算全，保证结果逐位一致。
		AdjacencyMode adjacencyMode = (_config.landFragment > 0.01f) ? AdjacencyMode.LandFragmentCandidates : AdjacencyMode.All;
		for (int i = 0; i < _tiles.Length; i++)
		{
			RecalculateSingleTile(i, seaThreshold, adjacencyMode);
		}
		ApplyLandFragmentation(seaThreshold);
		ApplyCoastFragmentation();
		RecalculateSeaConnectivity();
		Debug.Log((object)$"[SeaLand] 全量重算：陆地{GetTotalLandTiles()}/海洋{GetTotalSeaTiles()}/连通海域{_seaConnectGroups.Count}");
	}

	public void RecalculateDirty(HashSet<int> dirtyIndices)
	{
		if (dirtyIndices == null || dirtyIndices.Count == 0)
		{
			return;
		}
		ResetChangedTiles();
		EnsureRecalcState();
		float cachedSeaThreshold = _cachedSeaThreshold;
		bool flag = false;
		HashSet<int> hashSet = new HashSet<int>(dirtyIndices);
		foreach (int dirtyIndex in dirtyIndices)
		{
			ReadOnlySpan<int> neighbourSpan = GetNeighbourSpanUnchecked(dirtyIndex);
			for (int k = 0; k < neighbourSpan.Length; k++)
			{
				hashSet.Add(neighbourSpan[k]);
			}
		}
		foreach (int item in hashSet)
		{
			bool isLand = _tiles[item].isLand;
			RecalculateSingleTile(item, cachedSeaThreshold, AdjacencyMode.None);
			if (isLand != _tiles[item].isLand)
			{
				flag = true;
			}
		}
		foreach (int item2 in hashSet)
		{
			RecalculateAdjacencyForTile(item2);
		}
		if (flag)
		{
			RecalculateSeaConnectivity();
		}
	}

	private float CalculateSeaLevelThresholdFromDistribution()
	{
		int count = 0;
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists)
			{
				_thresholdBuffer[count++] = _tiles[i].elevation01;
			}
		}
		if (count == 0)
		{
			_cachedSeaThreshold = 0f;
			return 0f;
		}
		Array.Sort(_thresholdBuffer, 0, count);
		float num = Mathf.Clamp(_config.landAmount + (0.5f - _config.seaLevel) * 0.5f, 0.02f, 0.95f);
		int index = Mathf.Clamp(Mathf.RoundToInt((1f - num) * (float)(count - 1)), 0, count - 1);
		_cachedSeaThreshold = _thresholdBuffer[index];
		return _cachedSeaThreshold;
	}

	private void RecalculateSingleTile(int index, float seaThreshold, AdjacencyMode adjacencyMode)
	{
		ref TileData reference = ref _tiles[index];
		float oldElevation01 = reference.elevation01;
		bool oldIsLand = reference.isLand;
		bool oldIsCoast = reference.isCoast;
		GameEnums.OceanTier oldOceanTier = reference.oceanTier;
		float oldOceanDepth01 = reference.oceanDepth01;
		float oldWaterAdjacentWeight = reference.waterAdjacentWeight;
		int oldSeaConnectId = reference.seaConnectId;
		if (!reference.exists)
		{
			reference.isLand = false;
			reference.isCoast = false;
			reference.oceanTier = GameEnums.OceanTier.None;
			reference.oceanDepth01 = 0f;
			reference.waterAdjacentWeight = 0f;
			reference.seaConnectId = -1;
			MarkChangedIfDifferent(index, oldElevation01, oldIsLand, oldIsCoast, oldOceanTier, oldOceanDepth01, oldWaterAdjacentWeight, oldSeaConnectId);
			return;
		}
		float num = reference.elevation01 - seaThreshold;
		reference.elevation01 = Mathf.Clamp(num, -1f, 1f);
		bool isLand = num > 0f;
		reference.isLand = isLand;
		reference.oceanDepth01 = (isLand ? 0f : Mathf.Clamp01(Mathf.Abs(num) / 0.7f));
		bool computeAdjacency = adjacencyMode == AdjacencyMode.All
			|| (adjacencyMode == AdjacencyMode.LandFragmentCandidates && isLand && !(reference.elevation01 > 0.15f));
		int num2 = 0;
		int num3 = 0;
		if (computeAdjacency)
		{
			ReadOnlySpan<int> neighbourSpan = GetNeighbourSpanUnchecked(index);
			for (int k = 0; k < neighbourSpan.Length; k++)
			{
				int neighbourIndex = neighbourSpan[k];
				if (_tiles[neighbourIndex].exists)
				{
					if (_tiles[neighbourIndex].isLand)
					{
						num3++;
					}
					else
					{
						num2++;
					}
				}
			}
		}
		int num4 = num2 + num3;
		if (isLand)
		{
			if (computeAdjacency)
			{
				reference.waterAdjacentWeight = ((num4 > 0) ? ((float)num2 / (float)num4) : 0f);
				reference.isCoast = num2 > 0;
			}
			reference.oceanTier = GameEnums.OceanTier.Land;
		}
		else
		{
			float num5 = 0.05f + _config.oceanBuffer * 0.35f;
			if (computeAdjacency)
			{
				reference.isCoast = reference.oceanDepth01 < 0.08f || num3 > 0;
				reference.waterAdjacentWeight = ((num4 > 0) ? (1f - (float)num3 / (float)num4) : 1f);
			}
			reference.oceanTier = ((reference.oceanDepth01 <= num5) ? GameEnums.OceanTier.NearSea : GameEnums.OceanTier.DeepSea);
		}
		MarkChangedIfDifferent(index, oldElevation01, oldIsLand, oldIsCoast, oldOceanTier, oldOceanDepth01, oldWaterAdjacentWeight, oldSeaConnectId);
	}

	/// <summary>
	/// 重算路径的确定性随机值（0~1）：由 _recalcSeed + 地块索引 + 用途盐散列得到。
	/// 同一 seed/同一输入必得同一结果，不同 seed 得到不同分布；不修改 UnityEngine.Random 全局状态。
	/// </summary>
	private float GetDeterministicRecalcValue(int tileIndex, uint salt)
	{
		unchecked
		{
			uint hash = (uint)_recalcSeed * 2166136261u;
			hash = (hash ^ (uint)tileIndex) * 16777619u;
			hash = (hash ^ salt) * 16777619u;
			hash ^= hash >> 13;
			hash *= 0x5BD1E995u;
			hash ^= hash >> 15;
			return (hash & 0xFFFFFFu) / 16777216f;
		}
	}

	private void ApplyLandFragmentation(float seaThreshold)
	{
		if (_config.landFragment <= 0.01f)
		{
			return;
		}
		_fragChangedTiles.Clear();
		for (int i = 0; i < _tiles.Length; i++)
		{
			ref TileData tile = ref _tiles[i];
			if (tile.exists && tile.isLand && !(tile.elevation01 > 0.15f))
			{
				float num = _fragmentNoise[i];
				float num2 = _config.landFragment * 0.6f * num * tile.waterAdjacentWeight;
				if (GetDeterministicRecalcValue(i, LandFragmentSeedSalt) < num2)
				{
					tile.isLand = false;
					tile.elevation01 = (0f - Mathf.Abs(tile.elevation01)) * 0.5f;
					tile.oceanDepth01 = Mathf.Clamp01(Mathf.Abs(tile.elevation01) / 0.7f);
					tile.oceanTier = GameEnums.OceanTier.NearSea;
					_fragChangedTiles.Add(i);
					MarkChanged(i);
				}
			}
		}
		// 全量邻接重算：初始逐地块重算的邻接是"边算边更新 isLand"的混合状态（依赖处理顺序），
		// 必须在这里一致化，供随后 ApplyCoastFragmentation 读取 isCoast。
		RecalculateAdjacencyForAll();
	}

	private void ApplyCoastFragmentation()
	{
		if (_config.coastFragment <= 0.01f)
		{
			return;
		}
		_fragChangedTiles.Clear();
		for (int i = 0; i < _tiles.Length; i++)
		{
			ref TileData tile = ref _tiles[i];
			if (tile.exists && tile.isCoast)
			{
				float num = _fragmentNoise[i];
				float num2 = _config.coastFragment * 0.4f * num;
				if (tile.isLand && GetDeterministicRecalcValue(i, CoastFragmentLandToSeaSalt) < num2)
				{
					tile.isLand = false;
					tile.elevation01 = -0.03f;
					tile.oceanDepth01 = 0.05f;
					tile.oceanTier = GameEnums.OceanTier.NearSea;
					_fragChangedTiles.Add(i);
					MarkChanged(i);
				}
				else if (!tile.isLand && GetDeterministicRecalcValue(i, CoastFragmentSeaToLandSalt) < num2 * 0.5f)
				{
					tile.isLand = true;
					tile.elevation01 = 0.02f;
					tile.oceanDepth01 = 0f;
					tile.oceanTier = GameEnums.OceanTier.Land;
					_fragChangedTiles.Add(i);
					MarkChanged(i);
				}
			}
		}
		// 局部邻接重算：landFragment 开启时上方已做过一次全量邻接一致化，因此这里只需刷新
		// 被海岸破碎改写地块及其邻格；landFragment 关闭时没有那种一致化，必须保持旧实现的全量重算。
		if (_config.landFragment > 0.01f)
		{
			RecalculateAdjacencyForAffected(_fragChangedTiles);
		}
		else
		{
			RecalculateAdjacencyForAll();
		}
	}

	private void RecalculateAdjacencyForAll()
	{
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists)
			{
				RecalculateAdjacencyForTile(i);
			}
		}
	}

	private void RecalculateAdjacencyForTile(int index)
	{
		ref TileData reference = ref _tiles[index];
		if (!reference.exists)
		{
			return;
		}
		bool oldIsCoast = reference.isCoast;
		float oldWaterAdjacentWeight = reference.waterAdjacentWeight;
		GameEnums.OceanTier oldOceanTier = reference.oceanTier;
		int num = 0;
		int num2 = 0;
		ReadOnlySpan<int> neighbourSpan = GetNeighbourSpanUnchecked(index);
		for (int k = 0; k < neighbourSpan.Length; k++)
		{
			int neighbourIndex = neighbourSpan[k];
			if (_tiles[neighbourIndex].exists)
			{
				if (_tiles[neighbourIndex].isLand)
				{
					num2++;
				}
				else
				{
					num++;
				}
			}
		}
		int num3 = num + num2;
		if (reference.isLand)
		{
			reference.waterAdjacentWeight = ((num3 > 0) ? ((float)num / (float)num3) : 0f);
			reference.isCoast = num > 0;
			MarkChangedIfDifferent(index, oldIsCoast, oldWaterAdjacentWeight, oldOceanTier);
			return;
		}
		reference.isCoast = reference.oceanDepth01 < 0.08f || num2 > 0;
		reference.waterAdjacentWeight = ((num3 > 0) ? (1f - (float)num2 / (float)num3) : 1f);
		float num4 = 0.05f + _config.oceanBuffer * 0.35f;
		reference.oceanTier = ((reference.oceanDepth01 <= num4) ? GameEnums.OceanTier.NearSea : GameEnums.OceanTier.DeepSea);
		MarkChangedIfDifferent(index, oldIsCoast, oldWaterAdjacentWeight, oldOceanTier);
	}

	private void RecalculateAdjacencyForAffected(List<int> changedTiles)
	{
		if (changedTiles.Count == 0)
		{
			return;
		}
		_affectedStampVersion++;
		if (_affectedStampVersion == int.MaxValue)
		{
			Array.Clear(_affectedStamp, 0, _affectedStamp.Length);
			_affectedStampVersion = 1;
		}
		_affectedTiles.Clear();
		for (int i = 0; i < changedTiles.Count; i++)
		{
			AddAffectedTile(changedTiles[i]);
			ReadOnlySpan<int> neighbourSpan = GetNeighbourSpanUnchecked(changedTiles[i]);
			for (int k = 0; k < neighbourSpan.Length; k++)
			{
				AddAffectedTile(neighbourSpan[k]);
			}
		}
		for (int i = 0; i < _affectedTiles.Count; i++)
		{
			RecalculateAdjacencyForTile(_affectedTiles[i]);
		}
	}

	private void AddAffectedTile(int index)
	{
		if (_affectedStamp[index] != _affectedStampVersion)
		{
			_affectedStamp[index] = _affectedStampVersion;
			_affectedTiles.Add(index);
		}
	}

	private void RecalculateSeaConnectivity()
	{
		_seaConnectGroups.Clear();
		_nextSeaConnectId = 0;
		bool[] visited = _connectVisited;
		Array.Clear(visited, 0, _tiles.Length);
		int[] queue = _bfsQueue;
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (!_tiles[i].exists || _tiles[i].isLand || visited[i])
			{
				continue;
			}
			List<int> list = new List<int>();
			int head = 0;
			int tail = 0;
			queue[tail++] = i;
			visited[i] = true;
			int num = _nextSeaConnectId++;
			while (head < tail)
			{
				int num2 = queue[head++];
				list.Add(num2);
				if (_tiles[num2].seaConnectId != num)
				{
					_tiles[num2].seaConnectId = num;
					MarkChanged(num2);
				}
				ReadOnlySpan<int> neighbourSpan = GetNeighbourSpanUnchecked(num2);
				for (int k = 0; k < neighbourSpan.Length; k++)
				{
					int neighbourIndex = neighbourSpan[k];
					if (!visited[neighbourIndex] && _tiles[neighbourIndex].exists && !_tiles[neighbourIndex].isLand)
					{
						visited[neighbourIndex] = true;
						queue[tail++] = neighbourIndex;
					}
				}
			}
			_seaConnectGroups[num] = list;
		}
	}

	/// <summary>
	/// 预计算邻居表的只读视图（热路径首选：无分配；顺序与 <see cref="GetNeighbourIndices"/> 完全一致）。
	/// </summary>
	public ReadOnlySpan<int> GetNeighbourSpan(int index)
	{
		EnsureNeighbourTable();
		return GetNeighbourSpanUnchecked(index);
	}

	private ReadOnlySpan<int> GetNeighbourSpanUnchecked(int index)
	{
		int start = _neighbourStart[index];
		return new ReadOnlySpan<int>(_neighbours, start, _neighbourStart[index + 1] - start);
	}

	public IEnumerable<int> GetNeighbourIndices(int index)
	{
		EnsureNeighbourTable();
		for (int k = _neighbourStart[index]; k < _neighbourStart[index + 1]; k++)
		{
			yield return _neighbours[k];
		}
	}

	private void EnsureNeighbourTable()
	{
		if (_neighbourStart == null || _neighbourTableWrapX != _config.wrapX || _neighbourTableWrapY != _config.wrapY)
		{
			BuildNeighbourTable();
		}
	}

	/// <summary>
	/// 构造时一次性把每格邻居压平成 <see cref="_neighbourStart"/> + <see cref="_neighbours"/>，
	/// 避免热路径每次 foreach 分配 int[6]/迭代器（1024×512 全量重算原先约 260 万次分配）。
	/// 邻居顺序与原实现（偶数行/奇数行两套六方向）完全一致，保证重算结果逐位不变。
	/// </summary>
	private void BuildNeighbourTable()
	{
		int count = _tiles.Length;
		if (_neighbourStart == null || _neighbourStart.Length != count + 1)
		{
			_neighbourStart = new int[count + 1];
		}
		int capacity = count * 6;
		if (_neighbours == null || _neighbours.Length != capacity)
		{
			_neighbours = new int[capacity];
		}
		_neighbourTableWrapX = _config.wrapX;
		_neighbourTableWrapY = _config.wrapY;
		int cursor = 0;
		for (int index = 0; index < count; index++)
		{
			_neighbourStart[index] = cursor;
			int x = index % _width;
			int y = index / _width;
			int[] dx = (y % 2 == 0) ? EvenHexDx : OddHexDx;
			int[] dy = (y % 2 == 0) ? EvenHexDy : OddHexDy;
			for (int direction = 0; direction < 6; direction++)
			{
				int num = x + dx[direction];
				int num2 = y + dy[direction];
				if (_config.wrapX)
				{
					num = (num % _width + _width) % _width;
				}
				else if (num < 0 || num >= _width)
				{
					continue;
				}
				if (_config.wrapY)
				{
					num2 = (num2 % _height + _height) % _height;
				}
				else if (num2 < 0 || num2 >= _height)
				{
					continue;
				}
				_neighbours[cursor++] = num2 * _width + num;
			}
		}
		_neighbourStart[count] = cursor;
	}

	private void EnsureRecalcState()
	{
		EnsureNeighbourTable();
		int count = _tiles.Length;
		if (_thresholdBuffer == null || _thresholdBuffer.Length < count)
		{
			_thresholdBuffer = new float[count];
		}
		if (_connectVisited == null || _connectVisited.Length < count)
		{
			_connectVisited = new bool[count];
		}
		if (_bfsQueue == null || _bfsQueue.Length < count)
		{
			_bfsQueue = new int[count];
		}
		if (_changedStamp == null || _changedStamp.Length < count)
		{
			_changedStamp = new int[count];
		}
		if (_affectedStamp == null || _affectedStamp.Length < count)
		{
			_affectedStamp = new int[count];
		}
	}

	private void ResetChangedTiles()
	{
		_lastChangedTiles.Clear();
		_changedStampVersion++;
		if (_changedStampVersion == int.MaxValue)
		{
			Array.Clear(_changedStamp, 0, _changedStamp.Length);
			_changedStampVersion = 1;
		}
	}

	private void MarkChanged(int index)
	{
		if (_changedStamp[index] != _changedStampVersion)
		{
			_changedStamp[index] = _changedStampVersion;
			_lastChangedTiles.Add(index);
		}
	}

	private void MarkChangedIfDifferent(int index, float oldElevation01, bool oldIsLand, bool oldIsCoast, GameEnums.OceanTier oldOceanTier, float oldOceanDepth01, float oldWaterAdjacentWeight, int oldSeaConnectId)
	{
		ref TileData reference = ref _tiles[index];
		if (oldElevation01 != reference.elevation01
			|| oldIsLand != reference.isLand
			|| oldIsCoast != reference.isCoast
			|| oldOceanTier != reference.oceanTier
			|| oldOceanDepth01 != reference.oceanDepth01
			|| oldWaterAdjacentWeight != reference.waterAdjacentWeight
			|| oldSeaConnectId != reference.seaConnectId)
		{
			MarkChanged(index);
		}
	}

	private void MarkChangedIfDifferent(int index, bool oldIsCoast, float oldWaterAdjacentWeight, GameEnums.OceanTier oldOceanTier)
	{
		ref TileData reference = ref _tiles[index];
		if (oldIsCoast != reference.isCoast
			|| oldWaterAdjacentWeight != reference.waterAdjacentWeight
			|| oldOceanTier != reference.oceanTier)
		{
			MarkChanged(index);
		}
	}

	public int GetHexDistance(int a, int b)
	{
		int num = a % _width;
		int num2 = a / _width;
		int num3 = b % _width;
		int num4 = b / _width;
		int num5 = num - (num2 - (num2 & 1)) / 2;
		int num6 = num2;
		int num7 = -num5 - num6;
		int num8 = num3 - (num4 - (num4 & 1)) / 2;
		int num9 = num4;
		int num10 = -num8 - num9;
		return (Mathf.Abs(num5 - num8) + Mathf.Abs(num7 - num10) + Mathf.Abs(num6 - num9)) / 2;
	}

	public int GetTotalLandTiles()
	{
		int num = 0;
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists && _tiles[i].isLand)
			{
				num++;
			}
		}
		return num;
	}

	public int GetTotalSeaTiles()
	{
		int num = 0;
		for (int i = 0; i < _tiles.Length; i++)
		{
			if (_tiles[i].exists && !_tiles[i].isLand)
			{
				num++;
			}
		}
		return num;
	}

	public int GetConnectedSeaCount()
	{
		return _seaConnectGroups.Count;
	}

	public int GetSeaGroupSize(int connectId)
	{
		if (!_seaConnectGroups.TryGetValue(connectId, out var value))
		{
			return 0;
		}
		return value.Count;
	}

	public bool AreSeaTilesConnected(int a, int b)
	{
		if (_tiles[a].isLand || _tiles[b].isLand)
		{
			return false;
		}
		return _tiles[a].seaConnectId == _tiles[b].seaConnectId;
	}
}
