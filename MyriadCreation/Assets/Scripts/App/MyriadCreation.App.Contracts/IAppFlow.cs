using System;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.App.Contracts
{
    /// <summary>应用级流程状态（与 SceneFlowController.FlowState 对应，供 App/UI 跨程序集使用）。</summary>
    public enum AppFlowState
    {
        MainMenu = 0,
        WorldGeneration = 1,
        Playing = 2,
        Editor = 3
    }

    /// <summary>
    /// 应用流程契约：主菜单 → 世界生成 → 游戏 → 地图编辑器 → 返回主菜单。
    /// 实现者：MyriadCreation.UI.SceneFlowController（阶段 2 迁入 MyriadCreation.App）。
    /// 4d-UI 后新游戏参数使用 Core NewGameRequestDto；WorldSetupOptions 仅保留在具体实现。
    /// </summary>
    public interface IAppFlow
    {
        AppFlowState State { get; }
        event Action<AppFlowState> StateChanged;
        event Action<bool> LoadingChanged;

        void StartNewGame();
        void StartNewGame(NewGameRequestDto request);
        void LoadGame(string saveName);
        void EnterWorldGeneration();
        void EnterPlaying();
        void EnterMapEditor();
        void EnterMapEditorWithSave(string saveFileName);
        void ReturnToMainMenu();
        void GenerateWorldWithLoading(Action onComplete);
        void GenerateWorldWithLoading(NewGameRequestDto request, Action onComplete);
    }
}
