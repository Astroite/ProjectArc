using _Game.Scripts.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Game.Scripts.Gameplay.Combat
{
    /// <summary>
    /// 子弹基础行为
    /// 负责：直线飞行、生命周期管理、碰撞检测（预留）
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [FormerlySerializedAs("_speed")]
        [Header("Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifeTime = 3f;
        [SerializeField] private GameObject hitVFX; // 命中特效(暂留)

        private float _timer;
        private GameObject _originalPrefab; // 记录自己属于哪个池子

        // 初始化数据（由 WeaponSystem 调用）
        public void Initialize(GameObject originalPrefab)
        {
            _originalPrefab = originalPrefab;
        }

        private void OnEnable()
        {
            // 每次从池里取出来时重置计时器
            _timer = 0f;
        }

        private void Update()
        {
            // 简单的直线移动
            transform.Translate(Vector3.forward * (speed * Time.deltaTime));

            // 生命周期检查
            _timer += Time.deltaTime;
            if (_timer >= lifeTime)
            {
                Recycle();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // TODO: 这里后续添加伤害逻辑和碰撞特效
            // Debug.Log($"Hit: {other.name}");
            
            // 碰到物体后回收
            Recycle();
        }

        private void Recycle()
        {
            if (ObjectPoolManager.Instance && _originalPrefab)
            {
                ObjectPoolManager.Instance.Despawn(_originalPrefab, this.gameObject);
            }
            else
            {
                // 如果没有池子系统（比如单独测试），则直接销毁
                Destroy(gameObject);
            }
        }
    }
}