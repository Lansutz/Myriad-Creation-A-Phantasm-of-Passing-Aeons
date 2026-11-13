using System;

namespace MyriadCreation.Core.Contracts
{
public sealed class MapSelection
{
	public readonly int width;

	public readonly int height;

	private readonly bool[] _mask;

	public bool this[int tile]
	{
		get
		{
			if (tile >= 0 && tile < _mask.Length)
			{
				return _mask[tile];
			}
			return false;
		}
		set
		{
			if (tile >= 0 && tile < _mask.Length)
			{
				_mask[tile] = value;
			}
		}
	}

	public int Count
	{
		get
		{
			int num = 0;
			for (int i = 0; i < _mask.Length; i++)
			{
				if (_mask[i])
				{
					num++;
				}
			}
			return num;
		}
	}

	public MapSelection(int width, int height)
	{
		this.width = Math.Max(1, width);
		this.height = Math.Max(1, height);
		_mask = new bool[this.width * this.height];
	}

	public void SelectRect(int x0, int y0, int x1, int y1)
	{
		int num = Math.Max(0, Math.Min(x0, x1));
		int num2 = Math.Min(width - 1, Math.Max(x0, x1));
		int num3 = Math.Max(0, Math.Min(y0, y1));
		int num4 = Math.Min(height - 1, Math.Max(y0, y1));
		for (int i = num3; i <= num4; i++)
		{
			for (int j = num; j <= num2; j++)
			{
				_mask[i * width + j] = true;
			}
		}
	}

	public void Clear()
	{
		Array.Clear(_mask, 0, _mask.Length);
	}
}
}
