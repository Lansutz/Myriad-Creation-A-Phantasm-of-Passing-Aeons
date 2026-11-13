using System.Collections.Generic;

namespace MyriadCreation.World.Anchor
{
public class VirtualControlResult
{
	public List<ControlTriangle> triangles = new List<ControlTriangle>();

	public Dictionary<int, int> virtualControlTiles = new Dictionary<int, int>();

	public List<(int a, int b)> connections = new List<(int, int)>();
}
}
