using UnityEngine;
using ProjectArc.Core.Interfaces;
using ProjectArc.Gameplay.Managers;

namespace ProjectArc.Gameplay.Player
{
    /// <summary>
    /// 玩家血量管理，实现 IDamageable 接口
    /// 优先将伤害传递给护盾，护盾耗尽后再扣本体血量
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float maxHealth = 100f;

        private float currentHealth;
        private ShieldController shield;
        private DefenseAbility defense;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            shield = GetComponent<ShieldController>();
            defense = GetComponent<DefenseAbility>();
        }

        private void OnEnable()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float amount, GameObject attacker = null)
        {
            // 无敌状态下免疫伤害
            if (defense != null && defense.IsInvincible) return;

            // 护盾优先承受伤害
            if (shield != null && shield.IsActive)
            {
                float remaining = shield.AbsorbDamage(amount, attacker);
                if (remaining <= 0f) return;
                amount = remaining;
            }

            currentHealth -= amount;
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
        }

        private void Die()
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.TriggerPlayerDeath();
        }
    }
}
