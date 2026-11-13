using System;
using System.Collections.Generic;
using MyriadCreation.World.Climate;
using MyriadCreation.World.Terrain;

namespace MyriadCreation.Simulation.WorldState
{
public sealed class WorldMapSimulationSystem
{
	private SeaLandGenerator _seaLandGenerator;

	private readonly PlanetClimateSimulator _climateSimulator;

	private readonly HashSet<int> _terrainDirtyTiles = new HashSet<int>();

	private readonly HashSet<int> _climateDirtyTiles = new HashSet<int>();

	private bool _configDirty;

	/// <summary>
	/// 全量重算前的刷新回调：GameWorld 用它把当前世界 seed 同步给海陆生成器，
	/// 保证即使由调度器直接触发全量重算（config dirty 路径）也能用当前世界 seed。
	/// </summary>
	public Action BeforeFullRecalculate { get; set; }

	public bool IsDirty
	{
		get
		{
			if (!_configDirty && _terrainDirtyTiles.Count <= 0)
			{
				return _climateDirtyTiles.Count > 0;
			}
			return true;
		}
	}

	public WorldMapSimulationSystem(SeaLandGenerator seaLandGenerator, PlanetClimateSimulator climateSimulator)
	{
		_seaLandGenerator = seaLandGenerator ?? throw new ArgumentNullException("seaLandGenerator");
		_climateSimulator = climateSimulator ?? throw new ArgumentNullException("climateSimulator");
	}

	/// <summary>
	/// 用当前世界 seed 重建后的海陆生成器替换旧实例（保持同一 WorldMapSimulationSystem / 调度注册不失效，
	/// 仅更换持有者，避免整对象替换导致调度器仍指向旧实例）。
	/// </summary>
	public void ReplaceSeaLandGenerator(SeaLandGenerator seaLandGenerator)
	{
		_seaLandGenerator = seaLandGenerator ?? throw new ArgumentNullException("seaLandGenerator");
	}

	public void MarkAllTerrainDirty(int tileCount)
	{
		for (int i = 0; i < tileCount; i++)
		{
			_terrainDirtyTiles.Add(i);
		}
	}

	/// <summary>
	/// 清空待重算的地形脏块（不触发重算）。初始生成/破碎化完成后调用：此时地形已在生成阶段全量重算过，
	/// 生成期批量 MarkAllTerrainDirty 留下的全表脏块若不清，首个 Daily tick 会用已设置的 _cachedSeaThreshold
	/// 对全表再做一次增量重算（elevation01 二次减阈值、isLand/海岸/海域连通二次改写）。
	/// 只清调用瞬间已存在的脏块；之后地图编辑器/运行时的显式 MarkTileDirty 仍正常进入重算队列。
	/// </summary>
	public void ClearTerrainDirty()
	{
		_terrainDirtyTiles.Clear();
	}

	public void MarkClimateDirty(int tileIndex)
	{
		if (tileIndex >= 0)
		{
			_climateDirtyTiles.Add(tileIndex);
		}
	}

	public void MarkTileDirty(int tileIndex)
	{
		if (tileIndex < 0)
		{
			return;
		}
		_terrainDirtyTiles.Add(tileIndex);
		foreach (int neighbourIndex in _seaLandGenerator.GetNeighbourIndices(tileIndex))
		{
			_terrainDirtyTiles.Add(neighbourIndex);
		}
	}

	public void MarkConfigDirty()
	{
		_configDirty = true;
	}

	public void RecalculateAll()
	{
		BeforeFullRecalculate?.Invoke();
		_seaLandGenerator.RecalculateAll();
		_climateSimulator.RecalculateAll();
		_terrainDirtyTiles.Clear();
		_climateDirtyTiles.Clear();
		_configDirty = false;
	}

	public void RecalculateDirty()
	{
		if (_configDirty)
		{
			RecalculateAll();
			return;
		}
		if (_terrainDirtyTiles.Count > 0)
		{
			_seaLandGenerator.RecalculateDirty(_terrainDirtyTiles);
			foreach (int terrainDirtyTile in _terrainDirtyTiles)
			{
				_climateDirtyTiles.Add(terrainDirtyTile);
			}
			_terrainDirtyTiles.Clear();
		}
		if (_climateDirtyTiles.Count > 0)
		{
			_climateSimulator.RecalculateDirty(_climateDirtyTiles);
			_climateDirtyTiles.Clear();
		}
	}
}
}
