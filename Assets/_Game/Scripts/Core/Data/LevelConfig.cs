using System.Collections.Generic;
using UnityEngine;
using ProjectArc.Gameplay.Spawning;

namespace ProjectArc.Core.Data
{
    public enum WinConditionType
    {
        ClearAllWaves,  // 打完所有波次（且消灭所有敌人）
        SurvivalTime,   // 存活指定时间
        ScoreTarget     // 达到指定分数
    }

    [CreateAssetMenu(fileName = "NewLevelConfig", menuName = "Project Arc/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("General Info")]
        public string levelName = "Level 1";
        [TextArea] public string levelDescription = "The journey begins.";

        [Header("Aesthetics")]
        public LevelTheme theme;
        // public AudioClip backgroundMusic; // 稍后接入Audio

        [Header("Rules")]
        public WinConditionType winCondition = WinConditionType.ClearAllWaves;
        [Tooltip("根据胜利条件填写：存活秒数 或 目标分数。如果是ClearAllWaves则忽略此项。")]
        public float winConditionValue = 0f;

        [Header("Script (Waves)")]
        public List<WaveDefinition> waves;
        public bool loopWaves = false;
    }
}