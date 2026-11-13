using System.Collections.Generic;
using MyriadCreation.Core.Data;
using UnityEngine;

namespace MyriadCreation.Rendering
{
/// <summary>
/// 城市俯视 Sprite（P4.2）：把 CityLayoutPlan 光栅化成 Texture2D。
/// 不参与主 tick；按需生成，按 key 缓存（LRU 8），同布局同图。
/// </summary>
public static class CityLayoutRenderer
{
	public const int DefaultCapacity = 8;

	private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();
	private static readonly List<string> _order = new List<string>();

	private static readonly Color Background = new Color(0.10f, 0.11f, 0.13f, 1f);
	private static readonly Color Road = new Color(0.16f, 0.17f, 0.20f, 1f);
	private static readonly Color Wall = new Color(0.55f, 0.50f, 0.38f, 1f);

	public static Texture2D Render(CityLayoutPlan plan)
	{
		if (plan == null) return null;
		int width = Mathf.Max(1, plan.width);
		int height = Mathf.Max(1, plan.height);
		var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
		{
			filterMode = FilterMode.Point,
			wrapMode = TextureWrapMode.Clamp,
			name = "CityLayout_" + width + "x" + height
		};
		Color[] pixels = new Color[width * height];
		for (int i = 0; i < pixels.Length; i++) pixels[i] = Background;
		for (int i = 0; i < plan.lots.Count; i++)
		{
			CityLot lot = plan.lots[i];
			Color color = DistrictColor(lot.districtType);
			int x0 = Mathf.Clamp(lot.rect.x, 0, width);
			int x1 = Mathf.Clamp(lot.rect.x + lot.rect.width, 0, width);
			int y0 = Mathf.Clamp(lot.rect.y, 0, height);
			int y1 = Mathf.Clamp(lot.rect.y + lot.rect.height, 0, height);
			for (int y = y0; y < y1; y++)
			{
				for (int x = x0; x < x1; x++) pixels[y * width + x] = color;
			}
		}
		if (plan.hasWall)
		{
			DrawRect(pixels, width, height, plan.wall.x, plan.wall.y, plan.wall.width, plan.wall.height, Wall, 2);
		}
		texture.SetPixels(pixels);
		texture.Apply(false, false);
		return texture;
	}

	public static Texture2D RenderCached(string key, CityLayoutPlan plan, int capacity = DefaultCapacity)
	{
		if (string.IsNullOrEmpty(key)) return Render(plan);
		if (_cache.TryGetValue(key, out Texture2D cached) && cached != null)
		{
			_order.Remove(key);
			_order.Add(key);
			return cached;
		}
		Texture2D texture = Render(plan);
		if (texture == null) return null;
		_cache[key] = texture;
		_order.Add(key);
		while (_order.Count > Mathf.Max(1, capacity))
		{
			string oldest = _order[0];
			_order.RemoveAt(0);
			if (_cache.TryGetValue(oldest, out Texture2D evicted))
			{
				_cache.Remove(oldest);
				Destroy(evicted);
			}
		}
		return texture;
	}

	public static void ClearCache()
	{
		foreach (Texture2D texture in _cache.Values) Destroy(texture);
		_cache.Clear();
		_order.Clear();
	}

	private static Color DistrictColor(int districtType)
	{
		switch (districtType)
		{
			case 1: return new Color(0.55f, 0.42f, 0.22f); // Commercial
			case 2: return new Color(0.40f, 0.38f, 0.30f); // Artisanal
			case 3: return new Color(0.36f, 0.28f, 0.28f); // Military
			case 4: return new Color(0.42f, 0.34f, 0.48f); // Noble
			case 5: return new Color(0.30f, 0.38f, 0.48f); // Administrative
			case 6: return new Color(0.48f, 0.42f, 0.30f); // Religious
			case 7: return new Color(0.24f, 0.36f, 0.42f); // Port
			case 8: return new Color(0.28f, 0.28f, 0.30f); // Slum
			case 9: return new Color(0.40f, 0.34f, 0.50f); // Cultural
			case 10: return new Color(0.30f, 0.42f, 0.26f); // Agricultural
			default: return new Color(0.36f, 0.38f, 0.34f);
		}
	}

	private static void DrawRect(Color[] pixels, int width, int height, int x, int y, int w, int h, Color color, int thickness)
	{
		for (int t = 0; t < thickness; t++)
		{
			int left = Mathf.Clamp(x + t, 0, width - 1);
			int right = Mathf.Clamp(x + w - 1 - t, 0, width - 1);
			int bottom = Mathf.Clamp(y + t, 0, height - 1);
			int top = Mathf.Clamp(y + h - 1 - t, 0, height - 1);
			for (int px = left; px <= right; px++)
			{
				pixels[bottom * width + px] = color;
				pixels[top * width + px] = color;
			}
			for (int py = bottom; py <= top; py++)
			{
				pixels[py * width + left] = color;
				pixels[py * width + right] = color;
			}
		}
	}

	private static void Destroy(Object obj)
	{
		if (obj == null) return;
		if (Application.isPlaying) Object.Destroy(obj);
		else Object.DestroyImmediate(obj);
	}
}
}
