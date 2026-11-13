using UnityEngine;

namespace MyriadCreation.World.Anchor
{
public struct ControlTriangle
{
	public int a;

	public int b;

	public int c;

	public Vector2 circumcenter;

	public float circumradiusSq;

	public int ownerRealmId;

	public bool isValid;

	public ControlTriangle(int a_, int b_, int c_, Vector2[] points)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		a = a_;
		b = b_;
		c = c_;
		circumcenter = Vector2.zero;
		circumradiusSq = 0f;
		ownerRealmId = -1;
		isValid = false;
		CalculateCircumcircle(points);
	}

	private void CalculateCircumcircle(Vector2[] points)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = points[a];
		Vector2 val2 = points[b];
		Vector2 val3 = points[c];
		float x = val.x;
		float y = val.y;
		float x2 = val2.x;
		float y2 = val2.y;
		float x3 = val3.x;
		float y3 = val3.y;
		float num = 2f * (x * (y2 - y3) + x2 * (y3 - y) + x3 * (y - y2));
		if (Mathf.Abs(num) < 1E-10f)
		{
			isValid = false;
			return;
		}
		float num2 = ((x * x + y * y) * (y2 - y3) + (x2 * x2 + y2 * y2) * (y3 - y) + (x3 * x3 + y3 * y3) * (y - y2)) / num;
		float num3 = ((x * x + y * y) * (x3 - x2) + (x2 * x2 + y2 * y2) * (x - x3) + (x3 * x3 + y3 * y3) * (x2 - x)) / num;
		circumcenter = new Vector2(num2, num3);
		circumradiusSq = (num2 - x) * (num2 - x) + (num3 - y) * (num3 - y);
		isValid = true;
	}

	public bool InCircumcircle(Vector2 p)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		float num = p.x - circumcenter.x;
		float num2 = p.y - circumcenter.y;
		return num * num + num2 * num2 <= circumradiusSq + 1E-06f;
	}

	public float GetMinAngleDeg(Vector2[] points)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = points[a];
		Vector2 val2 = points[b];
		Vector2 val3 = points[c];
		Vector2 val4 = val2 - val;
		float magnitude = val4.magnitude;
		val4 = val3 - val2;
		float magnitude2 = val4.magnitude;
		val4 = val - val3;
		float magnitude3 = val4.magnitude;
		float num = Mathf.Acos(Mathf.Clamp((magnitude * magnitude + magnitude3 * magnitude3 - magnitude2 * magnitude2) / (2f * magnitude * magnitude3), -1f, 1f)) * 57.29578f;
		float num2 = Mathf.Acos(Mathf.Clamp((magnitude * magnitude + magnitude2 * magnitude2 - magnitude3 * magnitude3) / (2f * magnitude * magnitude2), -1f, 1f)) * 57.29578f;
		return Mathf.Min(new float[3]
		{
			num,
			num2,
			180f - num - num2
		});
	}
}
}
