using System.Collections.Generic;
using UnityEngine;

namespace ProjectArc.Core
{
    // 修改 1: 不再继承 Singleton<ObjectPoolManager>，而是继承 MonoBehaviour
    public class ObjectPoolManager : MonoBehaviour
    {
        // 修改 2: 手动实现单例属性
        public static ObjectPoolManager Instance { get; private set; }

        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int size;
            public bool shouldExpand = true; // 是否允许自动扩容
        }

        public List<Pool> pools;
        public Dictionary<string, Queue<GameObject>> poolDictionary;
        
        // 字典：记录每个物体属于哪个Tag，方便回收时查找
        private Dictionary<GameObject, string> objectToTagMap;

        private void Awake()
        {
            // 修改 3: 标准单例初始化逻辑
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // 如果你希望它切换场景不销毁，可以取消下面这行的注释
            // DontDestroyOnLoad(gameObject);

            InitializePools();
        }

        void InitializePools()
        {
            poolDictionary = new Dictionary<string, Queue<GameObject>>();
            objectToTagMap = new Dictionary<GameObject, string>();

            foreach (Pool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.size; i++)
                {
                    GameObject obj = CreateNewObject(pool.prefab, pool.tag);
                    objectPool.Enqueue(obj);
                }

                poolDictionary.Add(pool.tag, objectPool);
            }
        }

        // 辅助方法：实例化并注册
        private GameObject CreateNewObject(GameObject prefab, string tag)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(this.transform); // 将生成的对象放在Manager下，保持层级整洁
            objectToTagMap[obj] = tag; // 记录它的“身份”
            return obj;
        }

        /// <summary>
        /// 从池中生成对象（替代旧的 SpawnFromPool）
        /// </summary>
        public GameObject SpawnObject(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            Queue<GameObject> poolQueue = poolDictionary[tag];
            GameObject objectToSpawn;

            if (poolQueue.Count == 0)
            {
                // 池子空了
                Pool poolSettings = pools.Find(p => p.tag == tag);
                if (poolSettings != null && poolSettings.shouldExpand)
                {
                    // 允许扩容：创建一个新的
                    objectToSpawn = CreateNewObject(poolSettings.prefab, tag);
                }
                else
                {
                    // 不允许扩容：只能从正在使用的对象中抢一个最老的（不推荐用于子弹，推荐用于特效）
                    // 或者直接返回null
                    Debug.LogWarning($"Pool {tag} is empty and expansion is disabled.");
                    return null;
                }
            }
            else
            {
                objectToSpawn = poolQueue.Dequeue();
            }

            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;

            // 注意：这里不再立即 Enqueue 回去！必须等待 ReturnObject 被调用。

            return objectToSpawn;
        }

        /// <summary>
        /// 将对象归还给池子
        /// </summary>
        public void ReturnObject(GameObject obj)
        {
            if (obj == null) return;

            if (objectToTagMap.TryGetValue(obj, out string tag))
            {
                if (poolDictionary.ContainsKey(tag))
                {
                    obj.SetActive(false);
                    poolDictionary[tag].Enqueue(obj);
                }
            }
            else
            {
                Debug.LogWarning($"Object {obj.name} was not created by ObjectPoolManager. Destroying it.");
                Destroy(obj);
            }
        }
        
        // 兼容性保留：如果你其他地方还没改，可以暂时留着这个，或者直接让它调用 SpawnObject
        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            return SpawnObject(tag, position, rotation);
        }
    }
}