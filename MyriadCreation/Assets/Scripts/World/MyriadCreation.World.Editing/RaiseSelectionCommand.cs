using System;
using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.World.Editing
{
public sealed class RaiseSelectionCommand : IMapEditCommand
{
	private readonly MapSelection _selection;

	private readonly float _delta;

	private readonly List<(int index, float oldElevation)> _snapshot = new List<(int, float)>();

	public string Description => $"抬升选区 {_delta:+#.###;-#.###;0}";

	public RaiseSelectionCommand(MapSelection selection, float delta)
	{
		_selection = selection ?? throw new ArgumentNullException("selection");
		_delta = delta;
	}

	public void Execute(TileData[] tiles)
	{
		_snapshot.Clear();
		for (int i = 0; i < tiles.Length; i++)
		{
			if (_selection[i] && tiles[i].exists)
			{
				_snapshot.Add((i, tiles[i].elevation01));
				tiles[i].elevation01 = Clamp(tiles[i].elevation01 + _delta, -1f, 1f);
				tiles[i].isLand = tiles[i].elevation01 > 0f;
			}
		}
	}

	public void Undo(TileData[] tiles)
	{
		foreach (var item in _snapshot)
		{
			tiles[item.index].elevation01 = item.oldElevation;
			tiles[item.index].isLand = item.oldElevation > 0f;
		}
	}

	private static float Clamp(float value, float min, float max)
	{
		if (!(value < min))
		{
			if (!(value > max))
			{
				return value;
			}
			return max;
		}
		return min;
	}
}
}
