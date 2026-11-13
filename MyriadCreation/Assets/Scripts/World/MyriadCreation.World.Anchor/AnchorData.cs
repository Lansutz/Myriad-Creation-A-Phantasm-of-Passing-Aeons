using System;
using System.Collections.Generic;

namespace MyriadCreation.World.Anchor
{
[Serializable]
public class AnchorData
{
	public int anchorId;

	public int provinceId;

	public int tileIndex;

	public float x;

	public float y;

	public int controllerAnchorId = -1;

	[NonSerialized]
	public List<int> controlledAnchorIds = new List<int>();

	[NonSerialized]
	public Dictionary<int, float> controlProgress = new Dictionary<int, float>();

	public int influenceRadius = 1;

	public bool isControlStable;

	[NonSerialized]
	public int influenceTier;

	public int bottleneckTileIndex = -1;
}
}
