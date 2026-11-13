using UnityEngine;

namespace MyriadCreation.UI.Common
{
/// <summary>
/// 单个 UI 面板的契约（P1.6）：一个面板由一个文件负责构建/数据/交互，
/// 通过 <see cref="UiPanelRegistry"/> 注册到管理器；管理器只负责开关与转调，不再持有一堆字段。
/// </summary>
public interface IUserInterfacePanel
{
	string PanelId { get; }

	bool IsOpen { get; }

	void Build(Transform parent);

	void Show();

	void Hide();
}
}
