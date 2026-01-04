using System.Collections.Generic;
using UnityEngine;
using ProjectArc.Core.Data;

namespace ProjectArc.Core
{
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        // Key: Prefab的InstanceID (唯一标识)
        // Value: 该Prefab对应的对象队列
        private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
        
        // Key: 场景里的实例物体
        // Value: 它所属的池子ID (Prefab InstanceID)
        private Dictionary<GameObject, int> objectToPoolIdMap = new Dictionary<GameObject, int>();
        
        // Key: Prefab InstanceID
        // Value: 原始Prefab的引用 (用于自动扩容)
        private Dictionary<int, GameObject> idToPrefabMap = new Dictionary<int, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 如果需要跨场景保持
        }

        /// <summary>
        /// 核心方法：根据主题配置，一次性初始化所有需要的池子
        /// </summary>
        public void InitializeTheme(LevelTheme theme)
        {
            // 1. 清理旧池子（销毁所有旧对象，释放内存）
            foreach(var queue in poolDictionary.Values)
            {
                while(queue.Count > 0) 
                {
                    GameObject obj = queue.Dequeue();
                    if(obj != null) Destroy(obj); 
                }
            }
            poolDictionary.Clear();
            objectToPoolIdMap.Clear();
            idToPrefabMap.Clear();

            // 2. 初始化各类列表
            // 数量可以根据需求调整，或者在Theme里定义
            if(theme.enemyPrefabs != null) CreatePoolsFromList(theme.enemyPrefabs, 10);
            if(theme.projectilePrefabs != null) CreatePoolsFromList(theme.projectilePrefabs, 50);
            if(theme.vfxPrefabs != null) CreatePoolsFromList(theme.vfxPrefabs, 20);
            
            Debug.Log($"<color=cyan>ObjectPool: Theme '{theme.name}' Initialized.</color>");
        }

        private void CreatePoolsFromList(List<GameObject> prefabs, int count)
        {
            foreach (var prefab in prefabs)
            {
                if (prefab == null) continue;
                
                int poolKey = prefab.GetInstanceID(); // 获取 Prefab 资源的唯一ID

                if (!poolDictionary.ContainsKey(poolKey))
                {
                    poolDictionary.Add(poolKey, new Queue<GameObject>());
                    idToPrefabMap.Add(poolKey, prefab); // 记录原始Prefab以便扩容

                    for (int i = 0; i < count; i++)
                    {
                        CreateNewObject(poolKey, prefab);
                    }
                }
            }
        }

        private GameObject CreateNewObject(int poolKey, GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            objectToPoolIdMap[obj] = poolKey; // 标记身份：这个物体属于哪个池子
            poolDictionary[poolKey].Enqueue(obj);
            return obj;
        }

        /// <summary>
        /// 新的生成接口：直接传 Prefab 引用
        /// </summary>
        /// <param name="prefab">你想生成的 Prefab 资源引用</param>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            int poolKey = prefab.GetInstanceID();

            // 如果池子不存在（比如忘了在Theme里配），尝试动态创建（有性能警告）
            if (!poolDictionary.ContainsKey(poolKey))
            {
                Debug.LogWarning($"Pool for '{prefab.name}' not found in Theme. Creating dynamically (Performance Hit).");
                List<GameObject> tempList = new List<GameObject> { prefab };
                CreatePoolsFromList(tempList, 1);
            }

            Queue<GameObject> queue = poolDictionary[poolKey];
            GameObject obj;

            if (queue.Count == 0)
            {
                // 池子空了，自动扩容
                obj = CreateNewObject(poolKey, idToPrefabMap[poolKey]);
            }
            else
            {
                obj = queue.Dequeue();
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        public void ReturnObject(GameObject obj)
        {
            if (obj == null) return;

            if (objectToPoolIdMap.TryGetValue(obj, out int poolKey))
            {
                obj.SetActive(false);
                if (poolDictionary.ContainsKey(poolKey))
                {
                    poolDictionary[poolKey].Enqueue(obj);
                }
            }
            else
            {
                // 不属于池子的物体直接销毁
                Debug.LogWarning($"Object '{obj.name}' not in pool map. Destroying.");
                Destroy(obj); 
            }
        }
    }
}