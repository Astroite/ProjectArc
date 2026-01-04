using System.Collections;
using UnityEngine;
using ProjectArc.Core;
using ProjectArc.Core.Interfaces;

namespace ProjectArc.Gameplay.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float damagePower = 1f;
        [SerializeField] private float maxDurability = 1f;
        
        [Header("VFX References")]
        [Tooltip("拖入销毁/重击特效 Prefab")]
        [SerializeField] private GameObject hitVfxPrefab;
        
        [Tooltip("拖入反弹/轻微碰撞特效 Prefab")]
        [SerializeField] private GameObject bounceVfxPrefab;

        [Header("Ricochet (Bounce)")]
        [SerializeField] private int maxBounces = 0;
        [Range(0f, 1f)] [SerializeField] private float bounceSpeedMultiplier = 0.9f;
        [Range(0f, 1f)] [SerializeField] private float bounceDamageMultiplier = 0.8f;

        // 运行时状态
        private float currentDurability;
        private int currentBounces;
        private Vector3 moveDirection;
        private Coroutine deactivateRoutine;
        private TrailRenderer trail;

        public float CurrentHealth => currentDurability;

        private void Awake()
        {
            trail = GetComponentInChildren<TrailRenderer>();
        }

        public void Initialize(Vector3 direction, float speedMultiplier = 1f, float damageMultiplier = 1f)
        {
            moveDirection = direction.normalized;
            currentDurability = maxDurability;
            currentBounces = 0;
            
            this.speed = 20f * speedMultiplier; 
            this.damagePower = 1f * damageMultiplier; 

            RotateToFaceDirection();

            if (trail != null) trail.Clear();

            if (deactivateRoutine != null) StopCoroutine(deactivateRoutine);
            deactivateRoutine = StartCoroutine(DeactivateAfterTime(lifetime));
        }

        private void Update()
        {
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
            RotateToFaceDirection();
        }
        
        private void RotateToFaceDirection()
        {
            if (moveDirection != Vector3.zero) transform.forward = moveDirection;
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable target = other.GetComponent<IDamageable>();

            if (target != null)
            {
                if (target is Projectile otherBullet) HandleBulletClash(otherBullet);
                else HandleUnitHit(target);
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
            {
                HandleEnvironmentCollision(other);
            }
        }

        private void HandleEnvironmentCollision(Collider wall)
        {
            if (currentBounces < maxBounces) ReflectProjectile(wall);
            else { SpawnVFX(hitVfxPrefab, transform.position); Die(); }
        }

        private void ReflectProjectile(Collider wall)
        {
            float backTrackDist = speed * Time.deltaTime * 2f + 1f; 
            Ray backRay = new Ray(transform.position - moveDirection * backTrackDist, moveDirection);
            
            if (wall.Raycast(backRay, out RaycastHit hitInfo, backTrackDist * 2f))
            {
                moveDirection = Vector3.Reflect(moveDirection, hitInfo.normal).normalized;
                transform.position = hitInfo.point + moveDirection * 0.1f;

                speed *= bounceSpeedMultiplier;
                damagePower *= bounceDamageMultiplier;
                currentBounces++;
                
                SpawnVFX(bounceVfxPrefab, hitInfo.point);
            }
            else Die();
        }

        private void HandleBulletClash(Projectile otherBullet)
        {
            float myDamage = this.damagePower;
            float theirHardness = otherBullet.currentDurability; 

            otherBullet.TakeDamage(myDamage, this.gameObject);
            this.TakeDamage(theirHardness, otherBullet.gameObject);

            if (currentDurability <= 0) SpawnVFX(hitVfxPrefab, transform.position);
            else SpawnVFX(bounceVfxPrefab, transform.position);
        }

        private void HandleUnitHit(IDamageable unit)
        {
            unit.TakeDamage(damagePower, this.gameObject);
            TakeDamage(currentDurability, unit.gameObject); 

            if (currentDurability <= 0) SpawnVFX(hitVfxPrefab, transform.position);
            else SpawnVFX(bounceVfxPrefab, transform.position);
        }

        public void TakeDamage(float amount, GameObject attacker = null)
        {
            currentDurability -= amount;
            if (currentDurability <= 0) Die();
        }

        private void Die()
        {
            if (deactivateRoutine != null) StopCoroutine(deactivateRoutine);
            ObjectPoolManager.Instance.ReturnObject(this.gameObject);
        }

        private IEnumerator DeactivateAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            Die();
        }
        
        private void SpawnVFX(GameObject prefab, Vector3 pos)
        {
            if (prefab != null && ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Spawn(prefab, pos, Quaternion.identity);
            }
        }
    }
}