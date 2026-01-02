using UnityEngine;

namespace ProjectArc.Core.Interfaces
{
    /// <summary>
    /// 所有可受伤害对象的通用接口（单位、子弹、可破坏场景物等）
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 承受伤害
        /// </summary>
        /// <param name="amount">伤害数值</param>
        /// <param name="attacker">攻击者（可选，用于判断阵营或击杀统计）</param>
        void TakeDamage(float amount, GameObject attacker = null);

        /// <summary>
        /// 获取当前生命值/耐久度
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// 获取该对象的阵营/Layer (用于逻辑判断)
        /// </summary>
        GameObject gameObject { get; }
    }
}