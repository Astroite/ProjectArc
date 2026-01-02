using System.Collections;
using UnityEngine;
using ProjectArc.Core;
using ProjectArc.Core.Interfaces;

namespace ProjectArc.Gameplay.Combat
{
    [RequireComponent(typeof(Rigidbody))] // 确保有刚体
    public class Projectile : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifetime = 3f;
        
        [Tooltip("攻击力：打中别人时，扣除别人多少血")]
        [SerializeField] private float damagePower = 1f;
        
        [Tooltip("耐久度：子弹自身的血量。")]
        [SerializeField] private float maxDurability = 1f;
        
        [Header("VFX")]
        [Tooltip("销毁/重击特效的Tag（子弹死亡时触发）")]
        [SerializeField] private string hitVFXTag = "VFX_Hit_Spark";
        
        [Tooltip("反弹/轻微碰撞特效的Tag（撞墙反弹或拼刀存活时触发）")]
        [SerializeField] private string bounceVFXTag = "VFX_Hit_Spark_Weak"; 

        [Header("Ricochet (Bounce)")]
        [Tooltip("最大反弹次数 (0 = 不反弹)")]
        [SerializeField] private int maxBounces = 0;
        
        [Tooltip("每次反弹后的速度保留比例 (0.8 = 速度变为原来的80%)")]
        [Range(0f, 1f)]
        [SerializeField] private float bounceSpeedMultiplier = 0.9f;

        [Tooltip("每次反弹后的伤害保留比例")]
        [Range(0f, 1f)]
        [SerializeField] private float bounceDamageMultiplier = 0.8f;

        // 运行时状态
        private float currentDurability;
        private int currentBounces; // 当前已反弹次数
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
            currentBounces = 0; // 重置反弹次数
            
            // 应用倍率
            this.speed = 20f * speedMultiplier; 
            this.damagePower = 1f * damageMultiplier; 

            // 确保朝向移动方向 (如果是长条形子弹这很重要)
            RotateToFaceDirection();

            if (trail != null) trail.Clear();

            if (deactivateRoutine != null) StopCoroutine(deactivateRoutine);
            deactivateRoutine = StartCoroutine(DeactivateAfterTime(lifetime));
        }

        private void Update()
        {
            // 3D 空间位移
            transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
            
            // 额外的：每帧更新朝向（防止在反弹后朝向没变）
            RotateToFaceDirection();
        }
        
        private void RotateToFaceDirection()
        {
            if (moveDirection != Vector3.zero)
            {
                transform.forward = moveDirection;
            }
        }

        // --- 核心改动：碰撞与反弹逻辑 ---
        private void OnTriggerEnter(Collider other)
        {
            IDamageable target = other.GetComponent<IDamageable>();

            if (target != null)
            {
                // 撞到了可破坏物体（敌人、子弹等），依然执行原来的逻辑
                if (target is Projectile otherBullet)
                {
                    HandleBulletClash(otherBullet);
                }
                else
                {
                    HandleUnitHit(target);
                }
            }
            else
            {
                // 撞到了环境 (Wall / Environment)
                if (other.gameObject.layer == LayerMask.NameToLayer("Environment"))
                {
                    HandleEnvironmentCollision(other);
                }
            }
        }

        private void HandleEnvironmentCollision(Collider wall)
        {
            // 检查是否还有反弹次数
            if (currentBounces < maxBounces)
            {
                ReflectProjectile(wall);
            }
            else
            {
                // 没有反弹次数了，触发销毁特效并销毁
                SpawnHitEffect(transform.position, hitVFXTag);
                Die();
            }
        }

        private void ReflectProjectile(Collider wall)
        {
            // 核心算法：我们需要找到碰撞点的法线来计算反射角。
            float backTrackDist = speed * Time.deltaTime * 2f + 1f; 
            Ray backRay = new Ray(transform.position - moveDirection * backTrackDist, moveDirection);
            
            if (wall.Raycast(backRay, out RaycastHit hitInfo, backTrackDist * 2f))
            {
                // 1. 计算反射向量
                Vector3 incomingVec = moveDirection;
                Vector3 normal = hitInfo.normal;
                Vector3 reflectVec = Vector3.Reflect(incomingVec, normal);

                // 2. 更新方向
                moveDirection = reflectVec.normalized;
                
                // 3. 将子弹位置修正到碰撞点稍微外面一点
                transform.position = hitInfo.point + moveDirection * 0.1f;

                // 4. 应用衰减
                speed *= bounceSpeedMultiplier;
                damagePower *= bounceDamageMultiplier;

                // 5. 计数与特效
                currentBounces++;
                
                // 修改：这里只播放反弹特效（弱特效），而不是销毁特效
                SpawnHitEffect(hitInfo.point, bounceVFXTag);
            }
            else
            {
                Die();
            }
        }

        private void HandleBulletClash(Projectile otherBullet)
        {
            float myDamage = this.damagePower;
            float theirHardness = otherBullet.currentDurability; 

            // 互相造成伤害
            otherBullet.TakeDamage(myDamage, this.gameObject);
            this.TakeDamage(theirHardness, otherBullet.gameObject);

            // 逻辑修改：只有当自己被销毁时才播放大特效，否则播放小特效
            if (currentDurability <= 0)
            {
                SpawnHitEffect(transform.position, hitVFXTag);
            }
            else
            {
                SpawnHitEffect(transform.position, bounceVFXTag);
            }
        }

        private void HandleUnitHit(IDamageable unit)
        {
            unit.TakeDamage(damagePower, this.gameObject);
            
            // 对自己造成伤害（通常如果是普通子弹，碰到单位也就碎了）
            TakeDamage(currentDurability, unit.gameObject); 

            // 同样判断是否真的死了
            if (currentDurability <= 0)
            {
                SpawnHitEffect(transform.position, hitVFXTag);
            }
            else
            {
                // 如果是穿透弹，可能只播一个小特效
                SpawnHitEffect(transform.position, bounceVFXTag);
            }
        }

        public void TakeDamage(float amount, GameObject attacker = null)
        {
            currentDurability -= amount;
            if (currentDurability <= 0)
            {
                Die();
            }
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
        
        // 修改：增加 Tag 参数
        private void SpawnHitEffect(Vector3 pos, string tag)
        {
            if (!string.IsNullOrEmpty(tag) && ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.SpawnObject(tag, pos, Quaternion.identity);
            }
        }
    }
}