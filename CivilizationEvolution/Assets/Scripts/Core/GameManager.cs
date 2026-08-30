using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Core
{
    /// <summary>
    /// 游戏全局管理器
    /// 管理游戏状态、场景切换、全局配置
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            MainMenu,
            MapEditor,
            Loading,
            Playing,
            Paused,
            GameOver
        }

        [Header("游戏速度")]
        [Range(0f, 5f)]
        [SerializeField] private float gameSpeed = 1f;

        [Header("世界引用")]
        [SerializeField] private GameWorld world;

        /// <summary>当前游戏状态（只读）</summary>
        public GameState CurrentState => currentState;
        /// <summary>游戏速度</summary>
        public float GameSpeed => gameSpeed;
        /// <summary>是否暂停（只读）</summary>
        public bool IsPaused => isPaused;
        /// <summary>当前世界（只读外部访问）</summary>
        public GameWorld World => world;

        // 运行时状态
        private GameState currentState = GameState.MainMenu;
        private bool isPaused = false;

        // 全局事件
        public event Action<GameState> OnGameStateChanged;
        public event Action<float> OnGameSpeedChanged;
        public event Action OnNewGameStarted;
        public event Action OnGameLoaded;
        public event Action OnGameSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            ChangeState(GameState.MainMenu);
        }

        /// <summary>切换游戏状态</summary>
        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnGameStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Playing:
                    isPaused = false;
                    Time.timeScale = gameSpeed;
                    break;
                case GameState.Paused:
                    isPaused = true;
                    Time.timeScale = 0f;
                    break;
                case GameState.MapEditor:
                    isPaused = true;
                    Time.timeScale = 0f;
                    break;
            }
        }

        /// <summary>开始新游戏</summary>
        public void StartNewGame(int mapWidth = 128, int mapHeight = 64, int seed = 42)
        {
            ChangeState(GameState.Loading);

            if (world == null)
            {
                // 优先复用场景中已存在的 GameWorld（MapRenderer/UIManager/MapEditor 都接线到它），
                // 避免再新建第二个 GameWorld 去生成地形、而渲染器仍对着空世界导致黑屏。
                world = FindAnyObjectByType<GameWorld>();
                if (world == null)
                {
                    var go = new GameObject("GameWorld");
                    world = go.AddComponent<GameWorld>();
                }
            }

            world.mapWidth = mapWidth;
            world.mapHeight = mapHeight;
            world.InitializeWorld();
            world.GenerateTerrain(seed);

            ChangeState(GameState.Playing);
            OnNewGameStarted?.Invoke();
        }

        /// <summary>保存游戏</summary>
        public void SaveGame(string saveName)
        {
            if (world == null) return;
            SaveSystem.SaveGame(world, saveName);
            OnGameSaved?.Invoke();
        }

        /// <summary>加载游戏</summary>
        public void LoadGame(string saveName)
        {
            ChangeState(GameState.Loading);
            var loadedWorld = SaveSystem.LoadGame(saveName);
            if (loadedWorld != null)
            {
                if (world != null) Destroy(world.gameObject);
                world = loadedWorld;
                ChangeState(GameState.Playing);
                OnGameLoaded?.Invoke();
            }
            else
            {
                ChangeState(GameState.MainMenu);
                Debug.LogError($"[GameManager] 加载存档失败: {saveName}");
            }
        }

        /// <summary>设置游戏速度</summary>
        public void SetGameSpeed(float speed)
        {
            gameSpeed = Mathf.Clamp(speed, 0f, 5f);
            if (!isPaused && currentState == GameState.Playing)
                Time.timeScale = gameSpeed;
            OnGameSpeedChanged?.Invoke(gameSpeed);
        }

        /// <summary>暂停/继续</summary>
        public void TogglePause()
        {
            if (currentState == GameState.Playing)
                ChangeState(GameState.Paused);
            else if (currentState == GameState.Paused)
                ChangeState(GameState.Playing);
        }

        /// <summary>返回主菜单</summary>
        public void ReturnToMainMenu()
        {
            if (world != null)
            {
                Destroy(world.gameObject);
                world = null;
            }
            ChangeState(GameState.MainMenu);
        }

        private void Update()
        {
            // 空格键暂停
            if (Input.GetKeyDown(KeyCode.Space) &&
                (currentState == GameState.Playing || currentState == GameState.Paused))
            {
                TogglePause();
            }
        }
    }
}
