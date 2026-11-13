using System.Collections.Generic;
using MyriadCreation.Core.Data;

namespace MyriadCreation.World.Editing
{
public sealed class MapEditHistory
{
	private readonly List<IMapEditCommand> _undo = new List<IMapEditCommand>();

	private readonly List<IMapEditCommand> _redo = new List<IMapEditCommand>();

	public int MaxUndo = 64;

	public void Execute(TileData[] tiles, IMapEditCommand command)
	{
		if (command != null)
		{
			command.Execute(tiles);
			_undo.Add(command);
			_redo.Clear();
			while (_undo.Count > MaxUndo)
			{
				_undo.RemoveAt(0);
			}
		}
	}

	public bool Undo(TileData[] tiles)
	{
		if (_undo.Count == 0)
		{
			return false;
		}
		IMapEditCommand mapEditCommand = _undo[_undo.Count - 1];
		_undo.RemoveAt(_undo.Count - 1);
		mapEditCommand.Undo(tiles);
		_redo.Add(mapEditCommand);
		return true;
	}

	public bool Redo(TileData[] tiles)
	{
		if (_redo.Count == 0)
		{
			return false;
		}
		IMapEditCommand mapEditCommand = _redo[_redo.Count - 1];
		_redo.RemoveAt(_redo.Count - 1);
		mapEditCommand.Execute(tiles);
		_undo.Add(mapEditCommand);
		return true;
	}
}
}
