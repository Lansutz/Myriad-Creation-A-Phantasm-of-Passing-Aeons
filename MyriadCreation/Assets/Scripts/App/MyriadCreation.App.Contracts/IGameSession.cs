using System;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.App.Contracts
{
    /// <summary>
    /// 游戏会话契约：状态/速度、开新档/读档/存档。
    /// 实现者：MyriadCreation.UI.GameManager（阶段 2 迁入 MyriadCreation.App）。
    /// 4d-UI 后契约不暴露 GameWorld/WorldSetupOptions；新游戏参数走 Core NewGameRequestDto。
    /// </summary>
    public interface IGameSession
    {
        bool HasWorld { get; }
        bool IsPlaying { get; }
        bool IsPaused { get; }
        float GameSpeed { get; }
        GameState CurrentState { get; }

        event Action<GameState> OnGameStateChanged;

        /// <summary>Bootstrap 在创建会话后注入默认地图参数（主菜单开始游戏沿用该配置）。</summary>
        void ConfigureNewGameDefaults(int mapWidth, int mapHeight, int seed, MapWrapMode wrapMode);

        void StartNewGame(NewGameRequestDto request);
        void StartNewGameAsync(NewGameRequestDto request, Action onComplete);
        void StartNewGameAsync(Action onComplete);

        void SaveGame(string saveName);
        void LoadGame(string saveName);
        void SetGameSpeed(float speed);
        void ChangeState(GameState state);
        void ReturnToMainMenu();
    }
}
