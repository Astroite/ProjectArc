using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectArc.Core;

namespace ProjectArc.Gameplay.Spawning
{
    public class WaveManager : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("按顺序把做好的波次文件拖进去")]
        [SerializeField] private List<WaveDefinition> waves;
        
        [Tooltip("循环模式：打完最后一波是否从头开始")]
        [SerializeField] private bool loopLevels = false;

        [Header("State")]
        [SerializeField] private bool isSpawning = false;

        private int currentWaveIndex = 0;

        private void Start()
        {
            if (waves != null && waves.Count > 0)
            {
                StartCoroutine(RunLevel());
            }
            else
            {
                Debug.LogWarning("WaveManager: No waves defined!");
            }
        }

        private IEnumerator RunLevel()
        {
            while (true)
            {
                // 如果当前索引超出范围
                if (currentWaveIndex >= waves.Count)
                {
                    if (loopLevels)
                    {
                        currentWaveIndex = 0;
                        Debug.Log("WaveManager: Level Complete. Looping...");
                    }
                    else
                    {
                        Debug.Log("WaveManager: All waves complete.");
                        yield break; // 结束协程
                    }
                }

                // 获取当前波次数据
                WaveDefinition currentWave = waves[currentWaveIndex];
                
                // 执行这一波
                yield return StartCoroutine(SpawnWave(currentWave));

                // 准备下一波
                currentWaveIndex++;
            }
        }

        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            Debug.Log($"WaveManager: Starting Wave {currentWaveIndex + 1} ({wave.name})");

            for (int i = 0; i < wave.count; i++)
            {
                SpawnEnemy(wave);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            // 等待波次间隔
            yield return new WaitForSeconds(wave.waitTimeAfterWave);
        }

        private void SpawnEnemy(WaveDefinition wave)
        {
            if (ObjectPoolManager.Instance == null) return;

            // 计算随机位置
            Vector3 randomPos = new Vector3(
                Random.Range(-wave.randomRange.x, wave.randomRange.x),
                Random.Range(-wave.randomRange.y, wave.randomRange.y),
                Random.Range(-wave.randomRange.z, wave.randomRange.z)
            ) * 0.5f; // 乘0.5是因为Range是从负到正，全宽是randomRange

            // 最终位置 = Spawner位置 + 基础偏移 + 随机偏移
            Vector3 spawnPosition = transform.position + wave.baseOffset + randomPos;
            Quaternion spawnRotation = Quaternion.identity; 
            // 如果你的敌人模型默认朝向不对，可以在这里调整 spawnRotation，例如 Quaternion.Euler(0, 180, 0)

            // 从对象池生成
            ObjectPoolManager.Instance.SpawnObject(wave.enemyTag, spawnPosition, spawnRotation);
        }

        // --- 公共控制接口 ---
        
        public void StopSpawning()
        {
            StopAllCoroutines();
            isSpawning = false;
        }
        
        public void RestartLevel()
        {
            StopAllCoroutines();
            currentWaveIndex = 0;
            StartCoroutine(RunLevel());
        }
    }
}