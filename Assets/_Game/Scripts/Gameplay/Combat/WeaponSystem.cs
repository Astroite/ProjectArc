using System;
using ProjectArc.Core;
using UnityEngine;

namespace ProjectArc.Gameplay.Combat
{
    public class WeaponSystem : MonoBehaviour
    {
        [System.Serializable]
        public class WeaponSlot
        {
            public Transform firePoint;
            public float fireRate = 0.5f;
            public string projectileTag = "Bullet_Basic";

            [Header("Stats Modifiers")] [Tooltip("伤害倍率：传给子弹用于计算最终伤害")]
            public float damageMultiplier = 1f;

            [Tooltip("速度倍率：传给子弹用于计算飞行速度")] public float speedMultiplier = 1f;

            [HideInInspector] public float nextFireTime;
        }

        [Header("Control Settings")] [Tooltip("是否自动连续射击（适用于敌人或自动炮台）")] [SerializeField]
        private bool isAutoFire = false;

        [Tooltip("是否响应玩家鼠标输入（调试或玩家控制器用）")] [SerializeField]
        private bool usePlayerInput = false;

        public WeaponSlot[] weaponSlots;

        void Update()
        {
            // 逻辑分支：自动开火 OR 玩家输入
            if (isAutoFire)
            {
                FireWeapons();
            }
            else if (usePlayerInput && Input.GetMouseButton(0))
            {
                FireWeapons();
            }
        }

        /// <summary>
        /// 触发所有武器槽位的发射逻辑（会检查冷却时间）
        /// </summary>
        public void FireWeapons()
        {
            // 遍历所有武器槽
            foreach (var slot in weaponSlots)
            {
                if (slot.firePoint == null) continue;

                // 检查冷却
                if (Time.time >= slot.nextFireTime)
                {
                    Fire(slot);
                    slot.nextFireTime = Time.time + 1 / slot.fireRate;
                }
            }
        }

        /// <summary>
        /// 执行单次发射
        /// </summary>
        void Fire(WeaponSlot slot)
        {
            if (ObjectPoolManager.Instance == null)
            {
                Debug.LogWarning("ObjectPoolManager instance not found!");
                return;
            }

            // 修改 1: 使用新的 SpawnObject API
            GameObject bullet = ObjectPoolManager.Instance.SpawnObject(
                slot.projectileTag,
                slot.firePoint.position,
                slot.firePoint.rotation
            );

            if (bullet != null)
            {
                // 修改 2: 适配新的 Projectile.Initialize 签名，传递倍率参数
                Projectile projScript = bullet.GetComponent<Projectile>();
                if (projScript != null)
                {
                    projScript.Initialize(slot.firePoint.forward, slot.speedMultiplier, slot.damageMultiplier);
                }
            }
        }

        // --- 公共方法，供外部脚本（如 EnemyController）控制 ---

        public void SetAutoFire(bool active)
        {
            isAutoFire = active;
        }

        private void OnDrawGizmos()
        {
            foreach (var slot in weaponSlots)
            {
                if (slot.firePoint == null) continue;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(slot.firePoint.position, slot.firePoint.position + slot.firePoint.forward * 1.5f);

            }
        }
    }
}