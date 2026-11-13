using UnityEngine;
using MyriadCreation.Save.Contracts;

namespace MyriadCreation.App.Contracts
{
    /// <summary>
    /// App 层服务定位器（避免 App 直接依赖具体 UI/设置类）。
    /// UI 在 Awake 时注册实现；App 只读取接口。所有读取都会过滤 Unity 已销毁对象（fake-null）。
    /// </summary>
    public static class AppServices
    {
        private static IGameUiHost _uiHost;
        private static IMainMenuUi _mainMenuUi;
        private static IUiFactory _uiFactory;
        private static ISettingsService _settings;
        private static IAppFlow _appFlow;
        private static IGameSession _gameSession;
        private static ISaveSlotService _saveSlots;

        public static IGameUiHost UiHost => Alive(_uiHost);
        public static IMainMenuUi MainMenuUi => Alive(_mainMenuUi);
        public static IUiFactory UiFactory => Alive(_uiFactory);
        public static ISettingsService Settings => Alive(_settings);
        public static IAppFlow AppFlow => Alive(_appFlow);
        public static IGameSession GameSession => Alive(_gameSession);
        public static ISaveSlotService SaveSlots => _saveSlots;

        public static void RegisterUiHost(IGameUiHost value) => _uiHost = value;
        public static void RegisterMainMenuUi(IMainMenuUi value) => _mainMenuUi = value;
        public static void RegisterUiFactory(IUiFactory value) => _uiFactory = value;
        public static void RegisterSettings(ISettingsService value) => _settings = value;
        public static void RegisterAppFlow(IAppFlow value) => _appFlow = value;
        public static void RegisterGameSession(IGameSession value) => _gameSession = value;
        public static void RegisterSaveSlots(ISaveSlotService value) => _saveSlots = value;

        public static void UnregisterUiHost(IGameUiHost value) { if (ReferenceEquals(_uiHost, value)) _uiHost = null; }
        public static void UnregisterMainMenuUi(IMainMenuUi value) { if (ReferenceEquals(_mainMenuUi, value)) _mainMenuUi = null; }
        public static void UnregisterUiFactory(IUiFactory value) { if (ReferenceEquals(_uiFactory, value)) _uiFactory = null; }
        public static void UnregisterSettings(ISettingsService value) { if (ReferenceEquals(_settings, value)) _settings = null; }
        public static void UnregisterAppFlow(IAppFlow value) { if (ReferenceEquals(_appFlow, value)) _appFlow = null; }
        public static void UnregisterGameSession(IGameSession value) { if (ReferenceEquals(_gameSession, value)) _gameSession = null; }
        public static void UnregisterSaveSlots(ISaveSlotService value) { if (ReferenceEquals(_saveSlots, value)) _saveSlots = null; }

        /// <summary>场景切换/测试清理用（正常流程不需要；注册会自动覆盖）。</summary>
        public static void Reset()
        {
            _uiHost = null;
            _mainMenuUi = null;
            _uiFactory = null;
            _settings = null;
            _appFlow = null;
            _gameSession = null;
            _saveSlots = null;
        }

        private static T Alive<T>(T value) where T : class
        {
            // Unity 对象被销毁后 != null 仍为 true（fake-null），这里按 Unity 语义过滤。
            if (value is Object unityObject && unityObject == null)
            {
                return null;
            }
            return value;
        }
    }
}
