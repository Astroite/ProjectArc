using System.Collections;
using UnityEngine;
using ProjectArc.Core;
using ProjectArc.Gameplay.Combat;

namespace ProjectArc.Gameplay.Enemies
{
    public class EnemyEmitterController : MonoBehaviour
    {
        [SerializeField] private EnemyEmitterPattern pattern;
        [SerializeField] private Transform firePoint;

        private Coroutine fireRoutine;
        private float spiralAngle;

        private void OnEnable()
        {
            if (pattern != null) StartFiring();
        }

        private void OnDisable()
        {
            StopFiring();
        }

        public void StartFiring()
        {
            StopFiring();
            spiralAngle = 0f;
            fireRoutine = StartCoroutine(FireLoop());
        }

        public void StopFiring()
        {
            if (fireRoutine != null)
            {
                StopCoroutine(fireRoutine);
                fireRoutine = null;
            }
        }

        private IEnumerator FireLoop()
        {
            while (true)
            {
                FireBurst();
                yield return new WaitForSeconds(pattern.fireInterval);
            }
        }

        private void FireBurst()
        {
            if (pattern.projectilePrefab == null || ObjectPoolManager.Instance == null) return;

            switch (pattern.emitterType)
            {
                case EmitterType.Fan:
                    FireFan();
                    break;
                case EmitterType.Circle:
                    FireCircle();
                    break;
                case EmitterType.Spiral:
                    FireSpiral();
                    break;
                case EmitterType.Aimed:
                    FireAimed();
                    break;
            }
        }

        private void FireFan()
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            Vector3 baseForward = firePoint != null ? firePoint.forward : transform.forward;

            float startAngle = -pattern.spreadAngle * 0.5f;
            float step = pattern.bulletsPerBurst > 1
                ? pattern.spreadAngle / (pattern.bulletsPerBurst - 1)
                : 0f;

            for (int i = 0; i < pattern.bulletsPerBurst; i++)
            {
                float angle = startAngle + step * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * baseForward;
                SpawnBullet(origin, dir);
            }
        }

        private void FireCircle()
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            float step = 360f / pattern.bulletsPerBurst;

            for (int i = 0; i < pattern.bulletsPerBurst; i++)
            {
                float angle = step * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                SpawnBullet(origin, dir);
            }
        }

        private void FireSpiral()
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            Vector3 dir = Quaternion.Euler(0, spiralAngle, 0) * Vector3.forward;
            SpawnBullet(origin, dir);
            spiralAngle += pattern.spiralSpeed * pattern.spiralBulletInterval;
        }

        private void FireAimed()
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;

            // 尝试找到玩家位置
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                // 找不到玩家则向前发射
                Vector3 fwd = firePoint != null ? firePoint.forward : transform.forward;
                SpawnBullet(origin, fwd);
                return;
            }

            Vector3 dir = (player.transform.position - origin).normalized;
            dir.y = 0; // 保持水平
            dir.Normalize();

            for (int i = 0; i < pattern.bulletsPerBurst; i++)
            {
                if (pattern.bulletsPerBurst == 1)
                {
                    SpawnBullet(origin, dir);
                }
                else
                {
                    float spreadStart = -pattern.spreadAngle * 0.5f;
                    float spreadStep = pattern.spreadAngle / (pattern.bulletsPerBurst - 1);
                    float angle = spreadStart + spreadStep * i;
                    Vector3 spreadDir = Quaternion.Euler(0, angle, 0) * dir;
                    SpawnBullet(origin, spreadDir);
                }
            }
        }

        private void SpawnBullet(Vector3 position, Vector3 direction)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            GameObject bullet = ObjectPoolManager.Instance.Spawn(
                pattern.projectilePrefab, position, rotation);

            if (bullet != null)
            {
                Projectile proj = bullet.GetComponent<Projectile>();
                if (proj != null)
                {
                    proj.Initialize(direction, pattern.speedMultiplier, pattern.damageMultiplier, Faction.Enemy);
                }
            }
        }
    }
}
