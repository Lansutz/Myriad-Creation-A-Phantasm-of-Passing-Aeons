using MyriadCreation.Core.Data;

namespace MyriadCreation.World.Editing
{
public interface IMapEditCommand
{
	string Description { get; }

	void Execute(TileData[] tiles);

	void Undo(TileData[] tiles);
}
}
