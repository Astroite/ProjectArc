using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectArc.Core;

namespace ProjectArc.Gameplay.Spawning
{
    public class WaveManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private List<WaveDefinition> waves;
        [SerializeField] private bool loopLevels = false;

        private int currentWaveIndex = 0;

        private void Start()
        {
            if (waves != null && waves.Count > 0)
            {
                StartCoroutine(RunLevel());
            }
        }

        private IEnumerator RunLevel()
        {
            while (true)
            {
                if (currentWaveIndex >= waves.Count)
                {
                    if (loopLevels)
                    {
                        currentWaveIndex = 0;
                        Debug.Log("WaveManager: Looping...");
                    }
                    else
                    {
                        yield break;
                    }
                }

                WaveDefinition currentWave = waves[currentWaveIndex];
                yield return StartCoroutine(SpawnWave(currentWave));
                currentWaveIndex++;
            }
        }

        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            Debug.Log($"WaveManager: Starting Wave {currentWaveIndex + 1}");

            for (int i = 0; i < wave.count; i++)
            {
                SpawnEnemy(wave);
                yield return new WaitForSeconds(wave.spawnInterval);
            }

            yield return new WaitForSeconds(wave.waitTimeAfterWave);
        }

        private void SpawnEnemy(WaveDefinition wave)
        {
            if (ObjectPoolManager.Instance == null || wave.enemyPrefab == null) return;

            Vector3 randomPos = new Vector3(
                Random.Range(-wave.randomRange.x, wave.randomRange.x),
                Random.Range(-wave.randomRange.y, wave.randomRange.y),
                Random.Range(-wave.randomRange.z, wave.randomRange.z)
            ) * 0.5f;

            Vector3 spawnPosition = transform.position + wave.baseOffset + randomPos;
            Quaternion spawnRotation = Quaternion.Euler(0, 180, 0); // 假设敌人模型需要转180度面向摄像机

            // 使用新接口
            ObjectPoolManager.Instance.Spawn(wave.enemyPrefab, spawnPosition, spawnRotation);
        }
        
        public void RestartLevel()
        {
            StopAllCoroutines();
            currentWaveIndex = 0;
            StartCoroutine(RunLevel());
        }
    }
}