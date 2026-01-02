using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Core
{
    /// <summary>
    /// 对象池管理器
    /// 自动管理不同 Prefab 的对象池，支持自动扩容。
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        // 字典：Prefab -> 对象队列
        private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
        
        // 字典：用于快速查找某个物体属于哪个父节点（便于层级管理）
        private Dictionary<GameObject, Transform> _poolParents = new Dictionary<GameObject, Transform>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 预加载对象池
        /// </summary>
        public void CreatePool(GameObject prefab, int size)
        {
            if (_pools.ContainsKey(prefab)) return;

            // 创建一个专属父节点，保持 Hierarchy 整洁
            GameObject poolGroup = new GameObject($"Pool_{prefab.name}");
            poolGroup.transform.SetParent(this.transform);
            _poolParents[prefab] = poolGroup.transform;

            Queue<GameObject> objectQueue = new Queue<GameObject>();

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, poolGroup.transform);
                obj.SetActive(false);
                objectQueue.Enqueue(obj);
            }

            _pools.Add(prefab, objectQueue);
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.ContainsKey(prefab))
            {
                // 如果池子不存在，默认创建一个小规模的池子
                Debug.LogWarning($"[ObjectPool] Auto creating pool for {prefab.name}");
                CreatePool(prefab, 10);
            }

            Queue<GameObject> pool = _pools[prefab];
            GameObject obj;

            // 如果池子空了，扩容
            if (pool.Count == 0)
            {
                Transform parent = _poolParents[prefab];
                obj = Instantiate(prefab, parent);
            }
            else
            {
                obj = pool.Dequeue();
            }

            // 初始化状态
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            return obj;
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        /// <param name="prefab">原始的Prefab引用（作为Key）</param>
        /// <param name="instance">场景中的实例</param>
        public void Despawn(GameObject prefab, GameObject instance)
        {
            if (!_pools.ContainsKey(prefab))
            {
                Debug.LogError($"[ObjectPool] Trying to despawn object {instance.name} but pool for {prefab.name} does not exist.");
                Destroy(instance); // 兜底销毁
                return;
            }

            instance.SetActive(false);
            _pools[prefab].Enqueue(instance);
        }
    }
}