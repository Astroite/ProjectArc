using System;
using UnityEngine;
using ProjectArc.Core;

namespace ProjectArc.Gameplay.Combat
{
    public class WeaponSystem : MonoBehaviour
    {
        [System.Serializable]
        public class WeaponSlot
        {
            public Transform firePoint;
            public float fireRate = 0.5f;
            
            [Tooltip("直接拖入子弹预制体 (Prefab)")]
            public GameObject projectilePrefab; // 修改点：不再使用 string Tag
            
            [Header("Stats Modifiers")]
            public float damageMultiplier = 1f;
            public float speedMultiplier = 1f;

            [HideInInspector] public float nextFireTime;
        }

        [Header("Control Settings")]
        [SerializeField] private bool isAutoFire = false;
        [SerializeField] private bool usePlayerInput = false;

        public WeaponSlot[] weaponSlots;

        void Update()
        {
            if (isAutoFire)
            {
                FireWeapons();
            }
            else if (usePlayerInput && Input.GetMouseButton(0))
            {
                FireWeapons();
            }
        }

        public void FireWeapons()
        {
            foreach (var slot in weaponSlots)
            {
                if (slot.firePoint == null) continue;

                if (Time.time >= slot.nextFireTime)
                {
                    Fire(slot);
                    slot.nextFireTime = Time.time + 1 / slot.fireRate;
                }
            }
        }

        void Fire(WeaponSlot slot)
        {
            if (ObjectPoolManager.Instance == null || slot.projectilePrefab == null) return;

            // 使用新的 Spawn 接口，传入 Prefab 引用
            GameObject bullet = ObjectPoolManager.Instance.Spawn(
                slot.projectilePrefab, 
                slot.firePoint.position, 
                slot.firePoint.rotation
            );
            
            if (bullet != null)
            {
                Projectile projScript = bullet.GetComponent<Projectile>();
                if (projScript != null)
                {
                    // 假设 Forward 是 Z 轴 (3D)
                    projScript.Initialize(slot.firePoint.forward, slot.speedMultiplier, slot.damageMultiplier);
                }
            }
        }

        public void SetAutoFire(bool active)
        {
            isAutoFire = active;
        }

        private void OnDrawGizmos()
        {

            foreach (var slot in weaponSlots)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(slot.firePoint.position, slot.firePoint.position + slot.firePoint.forward);
            }
        }
    }
}