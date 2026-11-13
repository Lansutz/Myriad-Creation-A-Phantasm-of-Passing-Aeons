using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public struct Deity
{
	public string name;

	public string domain;

	public float importance;

	public string symbol;

	public List<string> festivals;
}
}
