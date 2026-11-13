using System;
using System.Collections.Generic;
using System.IO;
using MyriadCreation.Core;
using UnityEngine;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Simulation.LawHost
{
/// <summary>
/// StreamingAssets/Base/Laws/Laws.json 的文件结构。
/// JsonUtility 不支持 Dictionary，因此全部用数组；字段名与 LawGroupDef/LawDef/DecreeDef 的 public 字段一一对应。
/// </summary>
[Serializable]
public sealed class LawCatalogFile
{
	public List<LawGroupDef> groups = new List<LawGroupDef>();

	public List<DecreeDef> decrees = new List<DecreeDef>();
}

/// <summary>
/// 法律定义内容加载器（"定义是定义，脚本是脚本"）：从 StreamingAssets JSON 读取法律/法令定义。
/// JSON 是**唯一完整内容来源**（12 组/50 法/2 法令）；首次访问 DefaultLawCatalog 时懒加载一次
/// （启动/读档第一次取目录即触发）。文件缺失或解析失败时自动回退 DefaultLawCatalog 的
/// **最小回退集**（taxation/military 核心法 + 1 法令），保证无 JSON 的旧环境/旧档仍可启动并给出明确告警。
/// 只读取定义数据，不改变法律逻辑/存档语义；mod 可通过修改 Base/Laws/Laws.json 覆盖默认目录。
/// </summary>
public static class LawContentLoader
{
	/// <summary>相对 StreamingAssets 的默认法律定义路径。</summary>
	public const string RelativePath = ContentPaths.Base + "/Laws/Laws.json";

	private static bool _attempted;

	private static List<LawGroupDef> _groups;

	private static List<DecreeDef> _decrees;

	private static string _lastError = "";

	/// <summary>是否成功加载了 JSON 内容（false = 使用 DefaultLawCatalog 最小回退集）。</summary>
	public static bool HasContent => _groups != null && _groups.Count > 0;

	/// <summary>最近一次加载失败原因（调试/测试用；成功后为空）。</summary>
	public static string LastError => _lastError;

	/// <summary>已加载的组定义（未加载时为空表）。</summary>
	public static IReadOnlyList<LawGroupDef> Groups => ((IReadOnlyList<LawGroupDef>)_groups) ?? Array.Empty<LawGroupDef>();

	/// <summary>已加载的法令定义（未加载时为空表）。</summary>
	public static IReadOnlyList<DecreeDef> Decrees => ((IReadOnlyList<DecreeDef>)_decrees) ?? Array.Empty<DecreeDef>();

	/// <summary>启动/首次访问时的懒加载入口：只尝试一次，返回是否成功拿到 JSON 内容。</summary>
	public static bool EnsureLoaded()
	{
		if (!_attempted)
		{
			LoadFromStreamingAssets();
		}
		return HasContent;
	}

	/// <summary>默认路径（Editor/PC 为 Assets/StreamingAssets/Base/Laws/Laws.json）。</summary>
	public static string ResolveDefaultPath()
	{
		string root = Application.streamingAssetsPath;
		return (string.IsNullOrEmpty(root) ? "" : Path.Combine(root, RelativePath));
	}

	public static bool LoadFromStreamingAssets()
	{
		return LoadFromFile(ResolveDefaultPath());
	}

	/// <summary>从指定 JSON 文件加载；失败时清空已加载内容（DefaultLawCatalog 最小回退），不抛异常。</summary>
	public static bool LoadFromFile(string path)
	{
		_attempted = true;
		try
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				return Fail("找不到法律定义文件：" + path);
			}
			return ApplyJson(File.ReadAllText(path));
		}
		catch (Exception ex)
		{
			return Fail("读取法律定义失败：" + ex.Message);
		}
	}

	/// <summary>解析并应用 JSON 字符串（测试/mod 注入用）；失败时清空已加载内容并返回 false。</summary>
	public static bool ApplyJson(string json)
	{
		_attempted = true;
		try
		{
			if (string.IsNullOrEmpty(json))
			{
				return Fail("法律定义 JSON 为空");
			}
			LawCatalogFile file = JsonUtility.FromJson<LawCatalogFile>(json);
			if (file == null || file.groups == null || file.groups.Count == 0 || file.decrees == null)
			{
				return Fail("法律定义 JSON 缺少 groups/decrees");
			}
			int num = 0;
			foreach (LawGroupDef group in file.groups)
			{
				if (group == null || string.IsNullOrEmpty(group.groupId))
				{
					return Fail("法律定义 JSON 存在空组或空 groupId");
				}
				if (group.laws == null)
				{
					continue;
				}
				foreach (LawDef law in group.laws)
				{
					if (law == null || string.IsNullOrEmpty(law.lawId) || string.IsNullOrEmpty(law.groupId))
					{
						return Fail("法律定义 JSON 存在空法或空 lawId/groupId");
					}
					num++;
				}
			}
			if (num <= 0)
			{
				return Fail("法律定义 JSON 不含任何法律");
			}
			foreach (DecreeDef decree in file.decrees)
			{
				if (decree == null || string.IsNullOrEmpty(decree.decreeId))
				{
					return Fail("法令定义 JSON 存在空 decreeId");
				}
			}
			_groups = file.groups;
			_decrees = file.decrees;
			_lastError = "";
			// 目录变了：让效果消费层重建 law/decree 索引。
			LawEffectRuntime.ResetCatalogs();
			return true;
		}
		catch (Exception ex)
		{
			return Fail("解析法律定义失败：" + ex.Message);
		}
	}

	/// <summary>清空加载状态（测试隔离/热重载用）；之后 DefaultLawCatalog 回退最小回退集。</summary>
	public static void Reset()
	{
		_attempted = false;
		_groups = null;
		_decrees = null;
		_lastError = "";
		LawEffectRuntime.ResetCatalogs();
	}

	private static bool Fail(string error)
	{
		_groups = null;
		_decrees = null;
		_lastError = error ?? "";
		LawEffectRuntime.ResetCatalogs();
		Debug.LogWarning("[LawContentLoader] " + _lastError + "，回退 DefaultLawCatalog 最小回退目录（taxation/military 核心法 + 1 法令）");
		return false;
	}
}
}
