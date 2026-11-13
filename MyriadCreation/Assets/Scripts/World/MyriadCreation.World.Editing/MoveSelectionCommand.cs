using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.World.Editing
{
public sealed class MoveSelectionCommand : IMapEditCommand
{
	private readonly MapSelection _selection;

	private readonly int _dx;

	private readonly int _dy;

	private readonly int _width;

	private readonly int _height;

	private readonly List<(int index, TileData oldTile)> _snapshot = new List<(int, TileData)>();

	public string Description => $"移动选区 ({_dx},{_dy})";

	public MoveSelectionCommand(MapSelection selection, int dx, int dy)
	{
		_selection = selection ?? throw new ArgumentNullException("selection");
		_dx = dx;
		_dy = dy;
		_width = selection.width;
		_height = selection.height;
	}

	public void Execute(TileData[] tiles)
	{
		_snapshot.Clear();
		Dictionary<int, TileData> dictionary = new Dictionary<int, TileData>();
		for (int i = 0; i < _height; i++)
		{
			for (int j = 0; j < _width; j++)
			{
				int num = i * _width + j;
				if (_selection[num])
				{
					int num2 = j + _dx;
					int num3 = i + _dy;
					if (num2 >= 0 && num2 < _width && num3 >= 0 && num3 < _height)
					{
						dictionary[num3 * _width + num2] = tiles[num];
					}
				}
			}
		}
		for (int k = 0; k < _height; k++)
		{
			for (int l = 0; l < _width; l++)
			{
				int num4 = k * _width + l;
				if (_selection[num4])
				{
					_ = 1;
				}
				else
					dictionary.ContainsKey(num4);
				if (_selection[num4] || IsTarget(l, k))
				{
					_snapshot.Add((num4, tiles[num4]));
				}
			}
		}
		for (int m = 0; m < _snapshot.Count; m++)
		{
			int item = _snapshot[m].index;
			if (_selection[item])
			{
				tiles[item].exists = false;
				tiles[item].isLand = false;
				tiles[item].populationBlocks = null;
			}
		}
		foreach (KeyValuePair<int, TileData> item2 in dictionary)
		{
			tiles[item2.Key] = item2.Value;
			tiles[item2.Key].tileIndex = item2.Key;
		}
	}

	public void Undo(TileData[] tiles)
	{
		foreach (var item in _snapshot)
		{
			tiles[item.index] = item.oldTile;
		}
	}

	private bool IsTarget(int x, int y)
	{
		int num = x - _dx;
		int num2 = y - _dy;
		if (num >= 0 && num < _width && num2 >= 0 && num2 < _height)
		{
			return _selection[num2 * _width + num];
		}
		return false;
	}
}
}
