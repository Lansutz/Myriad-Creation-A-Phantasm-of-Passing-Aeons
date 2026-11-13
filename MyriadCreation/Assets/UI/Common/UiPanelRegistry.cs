using System.Collections.Generic;

namespace MyriadCreation.UI.Common
{
/// <summary>
/// 面板注册表：管理器按 PanelId 注册/查找面板，避免每个面板都在管理器里占一组字段。
/// 注册是唯一入口；未注册的面板不可被管理器打开。
/// </summary>
public sealed class UiPanelRegistry
{
	private readonly Dictionary<string, IUserInterfacePanel> _panels =
		new Dictionary<string, IUserInterfacePanel>();

	public int Count => _panels.Count;

	public void Register(IUserInterfacePanel panel)
	{
		if (panel == null || string.IsNullOrEmpty(panel.PanelId))
		{
			return;
		}
		_panels[panel.PanelId] = panel;
	}

	public bool TryGet(string panelId, out IUserInterfacePanel panel)
	{
		panel = null;
		return !string.IsNullOrEmpty(panelId) && _panels.TryGetValue(panelId, out panel);
	}

	public T Get<T>(string panelId) where T : class, IUserInterfacePanel
	{
		return TryGet(panelId, out IUserInterfacePanel panel) ? panel as T : null;
	}

	public void HideAll()
	{
		foreach (IUserInterfacePanel panel in _panels.Values)
		{
			panel?.Hide();
		}
	}
}
}
