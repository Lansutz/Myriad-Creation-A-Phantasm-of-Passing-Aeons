namespace MyriadCreation.App.Contracts
{
    /// <summary>
    /// 设置服务契约：包装 MyriadCreation.App.Settings.SettingsService 的读取/应用能力，
    /// 让 App/UI 只依赖接口而非静态类。实现者：MyriadCreation.App.Settings.AppSettingsService。
    /// </summary>
    public interface ISettingsService
    {
        float UiScale { get; }
        string Language { get; }

        void SetUiScale(float scale);
        void ApplySaved();
    }
}
