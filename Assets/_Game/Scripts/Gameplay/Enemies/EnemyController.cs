using UnityEngine;
using ProjectArc.Core;
using ProjectArc.Core.Interfaces;
using ProjectArc.Gameplay.Combat;

namespace ProjectArc.Gameplay.Enemies
{
    [RequireComponent(typeof(Rigidbody))] 
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 10f;
        [SerializeField] private int scoreValue = 100;
        
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private Vector3 moveDirection = Vector3.back; 
        [SerializeField] private float despawnLimit = -20f; 

        [Header("Visuals")]
        [Tooltip("死亡特效 Prefab")]
        [SerializeField] private GameObject deathVfxPrefab; // 修改：引用 Prefab
        
        // [SerializeField] private GameObject hitVfxPrefab; // 如果有受击特效也可以加

        private float currentHealth;
        private WeaponSystem weaponSystem;

        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            weaponSystem = GetComponent<WeaponSystem>();
            Rigidbody rb = GetComponent<Rigidbody>();
            if(rb != null) {
                rb.useGravity = false;
                rb.isKinematic = true;
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
            // 生成死亡特效
            if (deathVfxPrefab != null && ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Spawn(deathVfxPrefab, transform.position, Quaternion.identity);
            }
            
            ObjectPoolManager.Instance.ReturnObject(this.gameObject);
        }

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