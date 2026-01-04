using System.Collections.Generic;
using UnityEngine;

namespace ProjectArc.Core.Data
{
    [CreateAssetMenu(fileName = "NewLevelTheme", menuName = "Project Arc/Level Theme")]
    public class LevelTheme : ScriptableObject
    {
        [Header("Environment")]
        public Material skyboxMaterial;
        public Color fogColor = Color.black;
        public float fogDensity = 0.01f;

        [Header("Pre-warm Pools")]
        [Tooltip("这个主题关卡会用到的所有敌人Prefab")]
        public List<GameObject> enemyPrefabs;

        [Tooltip("这个主题会用到的所有子弹Prefab")]
        public List<GameObject> projectilePrefabs;

        [Tooltip("这个主题会用到的特效Prefab")]
        public List<GameObject> vfxPrefabs;
    }
}