using UnityEngine;

namespace ProjectArc.Gameplay.Spawning
{
    [CreateAssetMenu(fileName = "NewWave", menuName = "Project Arc/Wave Definition")]
    public class WaveDefinition : ScriptableObject
    {
        [Header("Enemy Settings")]
        [Tooltip("直接拖入敌人预制体，不再使用字符串Tag")]
        public GameObject enemyPrefab; // 修改点

        [Tooltip("这一波生成的敌人总数")]
        public int count = 5;

        [Tooltip("每个敌人生成的时间间隔")]
        public float spawnInterval = 1.0f;

        [Header("Positioning")]
        [Tooltip("生成位置相对于 Spawner 的偏移量")]
        public Vector3 baseOffset = new Vector3(0, 0, 0);

        [Tooltip("随机生成范围（例如 X=10 表示在左右 -5 到 5 之间随机）")]
        public Vector3 randomRange = new Vector3(10f, 0f, 0f);

        [Header("Transition")]
        [Tooltip("这波敌人生成完后，等待多少秒再开始下一波")]
        public float waitTimeAfterWave = 3.0f;
    }
}