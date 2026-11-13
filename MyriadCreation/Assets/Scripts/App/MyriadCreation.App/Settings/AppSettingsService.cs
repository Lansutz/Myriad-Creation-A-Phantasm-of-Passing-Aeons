using MyriadCreation.App.Contracts;

namespace MyriadCreation.App.Settings
{
    /// <summary>
    /// <see cref="ISettingsService"/> 的 App 层实现：委托现有静态 <see cref="SettingsService"/>，
    /// 让 UI/App 通过 AppServices.Settings 使用接口而不再直接依赖静态类。
    /// </summary>
    public sealed class AppSettingsService : ISettingsService
    {
        public float UiScale => SettingsService.Current.UiScale;
        public string Language => SettingsService.Current.Language;

        public void SetUiScale(float scale) => SettingsService.ApplyUiScale(scale);
        public void ApplySaved() => SettingsService.ApplySaved();
    }
}
