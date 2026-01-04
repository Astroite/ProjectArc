using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectArc.Core;
using ProjectArc.Core.Data;
using ProjectArc.Gameplay.Spawning;

namespace ProjectArc.Gameplay.Managers
{
    public enum LevelState
    {
        Loading,    // 初始化中
        Intro,      // 开场动画/倒计时
        Playing,    // 游戏中
        Victory,    // 胜利结算
        Defeat      // 失败结算
    }

    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("拖入这一关的剧本文件")]
        [SerializeField] private LevelConfig config;
        
        [Header("Setup")]
        [Tooltip("敌人生成的基准位置")]
        [SerializeField] private Transform spawnOrigin;

        // --- 运行时状态 ---
        public LevelState CurrentState { get; private set; }
        public float LevelTimer { get; private set; } // 游戏进行时长
        
        private int currentWaveIndex = 0;
        private Coroutine gameLoopCoroutine;

        // 事件：UI可以通过订阅这个来显示“Victory”或“Defeat”
        public System.Action<LevelState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        private void Start()
        {
            if (config == null)
            {
                Debug.LogError("LevelManager: No LevelConfig assigned! Please create and assign one.");
                return;
            }

            StartLevel();
        }

        private void Update()
        {
            if (CurrentState == LevelState.Playing)
            {
                LevelTimer += Time.deltaTime;
                CheckWinConditions();
            }
        }

        // --- 核心流程 ---

        public void StartLevel()
        {
            if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine);
            gameLoopCoroutine = StartCoroutine(GameLoopRoutine());
        }

        private IEnumerator GameLoopRoutine()
        {
            // 1. Loading 阶段：初始化资源
            SetState(LevelState.Loading);
            InitializeSystem();
            yield return null; // 等待一帧确保初始化完成

            // 2. Intro 阶段：给玩家准备时间
            SetState(LevelState.Intro);
            Debug.Log($"<color=yellow>Level Start: {config.levelName}</color>");
            // 这里未来可以通知 UI 显示 "Ready... GO!"
            yield return new WaitForSeconds(2.0f); 

            // 3. Playing 阶段：开始刷怪
            SetState(LevelState.Playing);
            LevelTimer = 0f;
            currentWaveIndex = 0;
            
            // 启动刷怪逻辑 (不阻塞主协程，单独运行)
            StartCoroutine(WaveSpawningRoutine());

            // 主协程在这里挂起，等待游戏结束
            while (CurrentState == LevelState.Playing)
            {
                yield return null;
            }

            // 4. 结束阶段
            Debug.Log($"Level Ended with state: {CurrentState}");
            // 这里未来可以调用 UIManager 显示结算面板
        }

        private void InitializeSystem()
        {
            if (ObjectPoolManager.Instance == null) return;

            // 应用主题配置
            if (config.theme != null)
            {
                ObjectPoolManager.Instance.InitializeTheme(config.theme);
                
                // 应用环境光照
                if (config.theme.skyboxMaterial != null)
                {
                    RenderSettings.skybox = config.theme.skyboxMaterial;
                    RenderSettings.fogColor = config.theme.fogColor;
                    RenderSettings.fogDensity = config.theme.fogDensity;
                    DynamicGI.UpdateEnvironment();
                }
            }
        }

        // --- 刷怪逻辑 ---

        private IEnumerator WaveSpawningRoutine()
        {
            while (CurrentState == LevelState.Playing)
            {
                // 检查波次是否耗尽
                if (currentWaveIndex >= config.waves.Count)
                {
                    if (config.loopWaves)
                    {
                        currentWaveIndex = 0;
                        Debug.Log("LevelManager: Waves looping...");
                    }
                    else
                    {
                        // 所有波次生成完毕，但这不代表胜利（可能场上还有怪）
                        // 刷怪协程结束，等待 Update 中的 CheckWinConditions 判定胜利
                        yield break; 
                    }
                }

                // 执行当前波次
                WaveDefinition currentWave = config.waves[currentWaveIndex];
                yield return StartCoroutine(ExecuteWave(currentWave));

                currentWaveIndex++;
            }
        }

        private IEnumerator ExecuteWave(WaveDefinition wave)
        {
            Debug.Log($"LevelManager: Spawning Wave {currentWaveIndex + 1}");

            for (int i = 0; i < wave.count; i++)
            {
                if (CurrentState != LevelState.Playing) yield break; // 游戏若中途结束则停止生成

                SpawnEnemy(wave);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            yield return new WaitForSeconds(wave.waitTimeAfterWave);
        }

        private void SpawnEnemy(WaveDefinition wave)
        {
            if (ObjectPoolManager.Instance == null || wave.enemyPrefab == null) return;

            Vector3 originPos = spawnOrigin != null ? spawnOrigin.position : transform.position;
            
            // 计算随机偏移
            Vector3 randomOffset = new Vector3(
                Random.Range(-wave.randomRange.x, wave.randomRange.x),
                Random.Range(-wave.randomRange.y, wave.randomRange.y),
                Random.Range(-wave.randomRange.z, wave.randomRange.z)
            ) * 0.5f;

            Vector3 finalPos = originPos + wave.baseOffset + randomOffset;
            Quaternion spawnRot = Quaternion.Euler(0, 0, 0); // 默认面向 Z 负方向

            ObjectPoolManager.Instance.Spawn(wave.enemyPrefab, finalPos, spawnRot);
        }

        // --- 胜负判定 ---

        private void CheckWinConditions()
        {
            bool isWin = false;

            switch (config.winCondition)
            {
                case WinConditionType.SurvivalTime:
                    if (LevelTimer >= config.winConditionValue) isWin = true;
                    break;

                case WinConditionType.ClearAllWaves:
                    // 简单判定：只要波次走完就算赢
                    // 进阶判定（TODO）：需要检查 ObjectPoolManager 或 EnemyManager 确定场上没有敌人存活
                    if (currentWaveIndex >= config.waves.Count) isWin = true; 
                    break;
                
                // ScoreTarget 判定将在 ScoreManager 实现后加入
            }

            if (isWin)
            {
                TriggerLevelEnd(true);
            }
        }

        // --- 公共控制 ---

        public void TriggerPlayerDeath()
        {
            if (CurrentState == LevelState.Playing)
            {
                TriggerLevelEnd(false);
            }
        }

        private void TriggerLevelEnd(bool isVictory)
        {
            SetState(isVictory ? LevelState.Victory : LevelState.Defeat);
            StopAllCoroutines(); // 停止刷怪
            
            if (isVictory)
            {
                Debug.Log("<color=green>VICTORY!</color>");
                // TODO: 消除全屏子弹
            }
            else
            {
                Debug.Log("<color=red>DEFEAT!</color>");
                // TODO: 慢动作效果
            }
        }

        private void SetState(LevelState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
        
        public void RestartLevel()
        {
            StopAllCoroutines();
            StartLevel();
        }
    }
}