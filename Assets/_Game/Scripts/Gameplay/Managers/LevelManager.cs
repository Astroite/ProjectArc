using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectArc.Core;
using ProjectArc.Core.Data;
using ProjectArc.Gameplay.Spawning;

namespace ProjectArc.Gameplay.Managers
{
    public class LevelManager : MonoBehaviour
    {
        [Header("1. Theme & Environment")]
        [Tooltip("当前关卡的主题配置（包含资源池和环境设置）")]
        [SerializeField] private LevelTheme levelTheme;

        [Header("2. Wave Configuration")]
        [Tooltip("关卡波次列表")]
        [SerializeField] private List<WaveDefinition> waves;
        [SerializeField] private bool loopLevels = false;

        [Header("Spawning Area")]
        [Tooltip("敌人生成的基准位置")]
        [SerializeField] private Transform spawnOrigin; // 替代之前的 transform.position，更灵活

        // 运行时状态
        private int currentWaveIndex = 0;
        private bool isLevelActive = false;

        private void Start()
        {
            // 第一步：初始化核心系统 (必须最先执行)
            InitializeLevel();

            // 第二步：开始游戏循环
            if (waves != null && waves.Count > 0)
            {
                isLevelActive = true;
                StartCoroutine(RunLevelLogic());
            }
            else
            {
                Debug.LogWarning("LevelManager: No waves defined!");
            }
        }

        private void InitializeLevel()
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogError("LevelManager: Missing ObjectPoolManager in scene!");
                return;
            }

            if (levelTheme != null)
            {
                // 1. 初始化对象池 (加载敌人、子弹、特效)
                ObjectPoolManager.Instance.InitializeTheme(levelTheme);

                // 2. 应用环境设置 (天空盒、雾效)
                ApplyEnvironmentSettings();
            }
        }

        private void ApplyEnvironmentSettings()
        {
            if (levelTheme.skyboxMaterial != null)
            {
                RenderSettings.skybox = levelTheme.skyboxMaterial;
            }
            
            RenderSettings.fog = true;
            RenderSettings.fogColor = levelTheme.fogColor;
            RenderSettings.fogDensity = levelTheme.fogDensity;
            
            DynamicGI.UpdateEnvironment();
        }

        private IEnumerator RunLevelLogic()
        {
            yield return new WaitForSeconds(1f); // 稍微等待一秒作为缓冲

            while (isLevelActive)
            {
                // 检查是否所有波次已结束
                if (currentWaveIndex >= waves.Count)
                {
                    if (loopLevels)
                    {
                        Debug.Log("LevelManager: Loop reset.");
                        currentWaveIndex = 0;
                    }
                    else
                    {
                        Debug.Log("LevelManager: Level Complete!");
                        // 这里可以触发胜利UI
                        yield break;
                    }
                }

                // 执行当前波次
                WaveDefinition currentWave = waves[currentWaveIndex];
                yield return StartCoroutine(SpawnWaveRoutine(currentWave));

                // 准备下一波
                currentWaveIndex++;
            }
        }

        private IEnumerator SpawnWaveRoutine(WaveDefinition wave)
        {
            Debug.Log($"LevelManager: Starting Wave {currentWaveIndex + 1} - {wave.name}");

            for (int i = 0; i < wave.count; i++)
            {
                SpawnSingleEnemy(wave);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            // 等待波次间隔
            yield return new WaitForSeconds(wave.waitTimeAfterWave);
        }

        private void SpawnSingleEnemy(WaveDefinition wave)
        {
            if (wave.enemyPrefab == null) return;

            // 计算生成位置
            // 优先使用 spawnOrigin (如果配置了)，否则使用当前物体位置
            Vector3 originPos = spawnOrigin != null ? spawnOrigin.position : transform.position;

            Vector3 randomOffset = new Vector3(
                Random.Range(-wave.randomRange.x, wave.randomRange.x),
                Random.Range(-wave.randomRange.y, wave.randomRange.y),
                Random.Range(-wave.randomRange.z, wave.randomRange.z)
            ) * 0.5f;

            Vector3 finalPos = originPos + wave.baseOffset + randomOffset;
            
            // 假设这是俯视视角，敌人面朝摄像机(Z反向)
            Quaternion spawnRot = Quaternion.Euler(0, 0, 0); 

            ObjectPoolManager.Instance.Spawn(wave.enemyPrefab, finalPos, spawnRot);
        }

        // --- 外部控制 ---
        public void RestartLevel()
        {
            StopAllCoroutines();
            currentWaveIndex = 0;
            // 也可以选择在这里清理当前场景的所有敌人
            StartCoroutine(RunLevelLogic());
        }
    }
}