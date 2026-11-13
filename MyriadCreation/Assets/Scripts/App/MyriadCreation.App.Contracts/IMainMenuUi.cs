namespace MyriadCreation.App.Contracts
{
    /// <summary>
    /// 主菜单 UI 反向契约。实现者：MyriadCreation.UI.Panels.MainMenuUI。
    /// </summary>
    public interface IMainMenuUi
    {
        void Build();
        void Show();
        void Hide();
        bool IsVisible { get; }
    }
}
