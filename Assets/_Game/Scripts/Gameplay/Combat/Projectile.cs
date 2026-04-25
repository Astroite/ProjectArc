using System.Collections;
using UnityEngine;
using ProjectArc.Core;
using ProjectArc.Core.Interfaces;
using ProjectArc.Gameplay.Player;

namespace ProjectArc.Gameplay.Combat
{
    public enum Faction { Player, Enemy }

    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float damagePower = 1f;
        [SerializeField] private float maxDurability = 1f;

        [Header("Collision")]
        [SerializeField] private LayerMask targetLayers = ~0; // 默认全部碰撞
        
        [Header("VFX References")]
        [Tooltip("拖入销毁/重击特效 Prefab")]
        [SerializeField] private GameObject hitVfxPrefab;

        [Tooltip("拖入反弹/轻微碰撞特效 Prefab")]
        [SerializeField] private GameObject bounceVfxPrefab;

        [Tooltip("拖入子弹拖尾特效 Prefab（跟随子弹移动）")]
        [SerializeField] private GameObject trailVfxPrefab;

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
        private Faction faction;
        private GameObject activeTrailVfx;

        public float CurrentHealth => currentDurability;

        private void Awake()
        {
            trail = GetComponentInChildren<TrailRenderer>();
        }

        public void Initialize(Vector3 direction, float speedMultiplier = 1f, float damageMultiplier = 1f, Faction faction = Faction.Player)
        {
            moveDirection = direction.normalized;
            currentDurability = maxDurability;
            currentBounces = 0;
            this.faction = faction;

            this.speed = 20f * speedMultiplier;
            this.damagePower = 1f * damageMultiplier;

            RotateToFaceDirection();

            if (trail != null) trail.Clear();

            // 生成拖尾 VFX
            SpawnTrailVfx();

            if (deactivateRoutine != null) StopCoroutine(deactivateRoutine);
            deactivateRoutine = StartCoroutine(DeactivateAfterTime(lifetime));
        }

        private void Update()
        {
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
            RotateToFaceDirection();
            if (activeTrailVfx != null) activeTrailVfx.transform.position = transform.position;
        }
        
        private void RotateToFaceDirection()
        {
            if (moveDirection != Vector3.zero) transform.forward = moveDirection;
        }

        private void OnTriggerEnter(Collider other)
        {
            int otherLayer = other.gameObject.layer;

            // 环境碰撞单独处理（墙壁反弹/销毁）
            if (otherLayer == LayerMask.NameToLayer("Environment"))
            {
                HandleEnvironmentCollision(other);
                return;
            }

            // 层级过滤：不在 targetLayers 中的直接跳过
            if ((targetLayers.value & (1 << otherLayer)) == 0) return;

            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                if (target is Projectile otherBullet) HandleBulletClash(otherBullet);
                else HandleUnitHit(target);
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
            // 同阵营子弹不互相抵消
            if (this.faction == otherBullet.faction) return;

            float myDamage = this.damagePower;
            float theirHardness = otherBullet.currentDurability;

            otherBullet.TakeDamage(myDamage, this.gameObject);
            this.TakeDamage(theirHardness, otherBullet.gameObject);

            if (currentDurability <= 0) SpawnVFX(hitVfxPrefab, transform.position);
            else SpawnVFX(bounceVfxPrefab, transform.position);
        }

        private void HandleUnitHit(IDamageable unit)
        {
            // 检查目标是否有防御技能（无敌 + 反弹）
            DefenseAbility defense = unit.gameObject.GetComponent<DefenseAbility>();
            if (defense != null && defense.TryReflect(this))
            {
                // 子弹被反弹，跳过伤害
                SpawnVFX(bounceVfxPrefab, transform.position);
                return;
            }

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

        /// <summary>
        /// 反弹子弹：反转方向，切换阵营，提升速度
        /// </summary>
        public void ReflectBack(float speedMultiplier = 1f)
        {
            moveDirection = -moveDirection;
            speed *= speedMultiplier;
            faction = Faction.Player;
            currentBounces = 0;
            RotateToFaceDirection();
            if (trail != null) trail.Clear();
        }

        private void Die()
        {
            ReturnTrailVfx();
            if (deactivateRoutine != null) StopCoroutine(deactivateRoutine);
            ObjectPoolManager.Instance.ReturnObject(this.gameObject);
        }

        private IEnumerator DeactivateAfterTime(float time)
        {
            yield return new WaitForSeconds(time);
            Die();
        }
        
        private void SpawnTrailVfx()
        {
            ReturnTrailVfx();
            if (trailVfxPrefab != null && ObjectPoolManager.Instance != null)
            {
                activeTrailVfx = ObjectPoolManager.Instance.Spawn(
                    trailVfxPrefab, transform.position, Quaternion.identity);
            }
        }

        private void ReturnTrailVfx()
        {
            if (activeTrailVfx != null)
            {
                ObjectPoolManager.Instance.ReturnObject(activeTrailVfx);
                activeTrailVfx = null;
            }
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