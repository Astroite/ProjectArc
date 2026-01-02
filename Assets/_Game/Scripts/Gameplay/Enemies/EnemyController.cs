using UnityEngine;
using ProjectArc.Core;
using ProjectArc.Core.Interfaces;
using ProjectArc.Gameplay.Combat;

namespace ProjectArc.Gameplay.Enemies
{
    // 修改 1: 依赖 3D Rigidbody
    [RequireComponent(typeof(Rigidbody))] 
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 10f;
        [SerializeField] private int scoreValue = 100;
        
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private Vector3 moveDirection = Vector3.back; // 3D 中通常 Z轴(back) 是朝向屏幕/玩家
        [Tooltip("超出这个位置时自动回收（根据你的摄像机视角调整轴向）")]
        [SerializeField] private float despawnLimit = -20f; 

        [Header("Visuals")]
        [SerializeField] private string deathVfxTag = "VFX_Explosion_Enemy";
        [SerializeField] private string hitVfxTag = "VFX_Hit_Enemy";

        private float currentHealth;
        private WeaponSystem weaponSystem;

        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            weaponSystem = GetComponent<WeaponSystem>();
            // 确保刚体不受重力影响，完全由代码控制
            Rigidbody rb = GetComponent<Rigidbody>();
            if(rb != null) {
                rb.useGravity = false;
                rb.isKinematic = true; // 推荐开启 Kinematic，避免物理推挤
            }
        }

        private void OnEnable()
        {
            currentHealth = maxHealth;
            if (weaponSystem != null) weaponSystem.SetAutoFire(true);
        }

        private void Update()
        {
            HandleMovement();
            CheckBounds();
        }

        private void HandleMovement()
        {
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
        }

        private void CheckBounds()
        {
            // 假设是纵向卷轴射击，Z轴向负方向移动
            // 如果你的游戏是向下飞(Y轴)，这里改回 transform.position.y
            if (transform.position.z < despawnLimit) 
            {
                ObjectPoolManager.Instance.ReturnObject(this.gameObject);
            }
        }

        public void TakeDamage(float amount, GameObject attacker = null)
        {
            currentHealth -= amount;
            if (currentHealth <= 0) Die();
        }

        private void Die()
        {
            if (!string.IsNullOrEmpty(deathVfxTag) && ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.SpawnObject(deathVfxTag, transform.position, Quaternion.identity);
            }
            ObjectPoolManager.Instance.ReturnObject(this.gameObject);
        }

        // 修改 2: 3D 碰撞回调
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                IDamageable player = other.GetComponent<IDamageable>();
                if (player != null)
                {
                    player.TakeDamage(10f, this.gameObject); 
                    TakeDamage(maxHealth, other.gameObject); 
                }
            }
        }
    }
}