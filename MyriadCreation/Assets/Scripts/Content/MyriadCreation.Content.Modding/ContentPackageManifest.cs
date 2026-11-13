using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
[Serializable]
public sealed class ContentPackageManifest
{
	public string formatVersion = "1";

	public string packageId = string.Empty;

	public string displayName = string.Empty;

	public string author = string.Empty;

	public string description = string.Empty;

	public string gameVersion = string.Empty;

	public List<string> contentTypes = new List<string>();

	/// <summary>启动期是否加载该 mod（缺省 true；`enabled=false` 时整包跳过）。</summary>
	public bool enabled = true;

	/// <summary>依赖的 packageId 列表；启动期只记录，不自动排序/拉取。</summary>
	public List<string> dependencies = new List<string>();
}
}
