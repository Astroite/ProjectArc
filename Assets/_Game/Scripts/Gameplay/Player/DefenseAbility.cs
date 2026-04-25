using System.Collections;
using UnityEngine;
using ProjectArc.Gameplay.Combat;

namespace ProjectArc.Gameplay.Player
{
    /// <summary>
    /// 主防御技能：短时间无敌 + 反弹所有敌方弹幕
    /// 激活期间，碰到玩家的 EnemyProjectile 会被反弹而非销毁
    /// </summary>
    public class DefenseAbility : MonoBehaviour
    {
        [Header("Ability Settings")]
        [SerializeField] private float duration = 2f;
        [SerializeField] private float cooldown = 8f;
        [SerializeField] private float reflectSpeedMultiplier = 1.2f;

        [Header("Input")]
        [Tooltip("防御技能触发按键")]
        [SerializeField] private KeyCode activationKey = KeyCode.Space;

        private bool isOnCooldown;
        private float cooldownTimer;

        /// <summary>当前是否处于无敌状态</summary>
        public bool IsInvincible { get; private set; }

        private void Update()
        {
            // 冷却计时
            if (isOnCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f) isOnCooldown = false;
            }

            // 触发防御
            if (Input.GetKeyDown(activationKey) && !isOnCooldown && !IsInvincible)
            {
                StartCoroutine(ActivateDefense());
            }
        }

        private IEnumerator ActivateDefense()
        {
            IsInvincible = true;
            Debug.Log("<color=cyan>Defense Active!</color>");

            yield return new WaitForSeconds(duration);

            IsInvincible = false;
            isOnCooldown = true;
            cooldownTimer = cooldown;
            Debug.Log($"<color=yellow>Defense on cooldown ({cooldown}s)</color>");
        }

        /// <summary>
        /// 由 Projectile 在 HandleUnitHit 前调用
        /// 如果防御激活中，反弹敌方子弹而非造成伤害
        /// 返回 true 表示已处理（子弹被反弹），调用方应跳过后续伤害逻辑
        /// </summary>
        public bool TryReflect(Projectile bullet)
        {
            if (!IsInvincible) return false;
            if (bullet == null) return false;

            // 只反弹敌方子弹
            // 通过检查子弹的 faction 来判断
            // 这里需要 Projectile 暴露 Faction，但当前是 private
            // 所以我们通过碰撞层级判断：EnemyProjectile layer = 15
            if (bullet.gameObject.layer != LayerMask.NameToLayer("EnemyProjectile")) return false;

            // 反弹：反转方向，切换阵营
            bullet.ReflectBack(reflectSpeedMultiplier);

            return true;
        }
    }
}
