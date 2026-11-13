using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class PropertyDefinition
{
	public string id;

	public string name;

	public string unit;

	public string kind;

	public string aggregation;

	public bool higherIsBetter;

	public List<string> tags = new List<string>();
}
}
