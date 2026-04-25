using UnityEngine;

namespace ProjectArc.Gameplay.Combat
{
    public enum EmitterType
    {
        Fan,        // 扇形发射
        Circle,     // 圆形全向发射
        Spiral,     // 螺旋发射（持续旋转）
        Aimed       // 瞄准玩家发射
    }

    [CreateAssetMenu(fileName = "NewEmitterPattern", menuName = "Project Arc/Emitter Pattern")]
    public class EnemyEmitterPattern : ScriptableObject
    {
        [Header("Emitter Type")]
        public EmitterType emitterType = EmitterType.Fan;

        [Header("Projectile")]
        [Tooltip("子弹预制体")]
        public GameObject projectilePrefab;

        [Header("Timing")]
        [Tooltip("每次发射间隔（秒）")]
        public float fireInterval = 1f;

        [Tooltip("每次发射的子弹数量")]
        public int bulletsPerBurst = 1;

        [Header("Fan Settings")]
        [Tooltip("扇形总角度范围")]
        [Range(5f, 360f)]
        public float spreadAngle = 30f;

        [Header("Spiral Settings")]
        [Tooltip("螺旋旋转速度（度/秒）")]
        public float spiralSpeed = 90f;

        [Tooltip("螺旋发射间隔（子弹间的间隔秒数）")]
        public float spiralBulletInterval = 0.1f;

        [Header("Stats")]
        [Tooltip("子弹速度倍率")]
        public float speedMultiplier = 1f;

        [Tooltip("子弹伤害倍率")]
        public float damageMultiplier = 1f;
    }
}
