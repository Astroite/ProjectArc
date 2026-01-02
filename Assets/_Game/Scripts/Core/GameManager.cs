using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace _Game.Scripts.Core
{
    public enum GameState
    {
        Boot,       // 启动中
        Menu,       // 主菜单
        Gameplay,   // 战斗中
        Paused,     // 暂停
        GameOver    // 结算
    }

    /// <summary>
    /// 全局游戏管理器 (单例)
    /// 负责：游戏状态切换、全局生命周期管理
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }

        // 定义状态变更事件
        public event Action<GameState> OnStateChanged;

        [FormerlySerializedAs("_targetFrameRate60")]
        [Header("Settings")]
        [SerializeField] private bool targetFrameRate60 = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        private void InitializeGame()
        {
            // 移动端性能设置
            Application.targetFrameRate = targetFrameRate60 ? 60 : -1;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            ChangeState(GameState.Boot);
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            Debug.Log($"[GameManager] Switch State: {newState}");
            
            OnStateChanged?.Invoke(newState);

            // 简单的状态处理逻辑
            switch (newState)
            {
                case GameState.Menu:
                    Time.timeScale = 1f;
                    break;
                case GameState.Gameplay:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    // 可以在这里做慢动作效果
                    Time.timeScale = 0.5f; 
                    break;
            }
        }

        /// <summary>
        /// 简单的场景加载封装
        /// </summary>
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}