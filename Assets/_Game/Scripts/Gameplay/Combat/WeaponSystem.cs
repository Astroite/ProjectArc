using _Game.Scripts.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Game.Scripts.Gameplay.Combat
{
    /// <summary>
    /// 武器系统
    /// 负责：射击频率控制、枪口位置管理、调用对象池发射
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject projectilePrefab; // 子弹预制体
        [SerializeField] private Transform[] firePoints;      // 枪口位置（支持多枪口）

        [Header("Stats")]
        [SerializeField] private float fireRate = 0.1f;       // 射击间隔
        [SerializeField] private bool autoFire = true;        // 是否自动射击

        private float _fireTimer;
        private bool _isFiring = false; // 内部开火状态

        private void Update()
        {
            // 只要处于开火状态 或者 开启了自动射击，就尝试开火
            if (_isFiring || autoFire)
            {
                ProcessFiring();
            }
        }

        // --- 新增供 UI 调用的接口 ---
        public void StartFiring() => _isFiring = true;
        public void StopFiring() => _isFiring = false;
        public void SetAutoFire(bool active) => autoFire = active;
        // ---------------------------

        private void ProcessFiring()
        {
            if (_fireTimer > 0)
            {
                _fireTimer -= Time.deltaTime;
                return;
            }

            Fire();
            _fireTimer = fireRate;
        }

        private void Fire()
        {
            if (!projectilePrefab || firePoints == null || firePoints.Length == 0) return;

            foreach (var point in firePoints)
            {
                // 从对象池获取子弹
                var bulletObj = ObjectPoolManager.Instance.Spawn(
                    projectilePrefab, 
                    point.position, 
                    point.rotation
                );

                // 初始化子弹
                Projectile projectile = bulletObj.GetComponent<Projectile>();
                if (projectile)
                {
                    projectile.Initialize(projectilePrefab);
                }
            }
        }
    }
}
