using System.Collections;
using UnityEngine;

namespace ProjectArc.Gameplay.Player
{
    /// <summary>
    /// 护盾系统：吸收伤害，传递受击点给 Shader 驱动波纹效果
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ShieldController : MonoBehaviour
    {
        [Header("Shield Stats")]
        [SerializeField] private float maxShieldHp = 50f;
        [SerializeField] private float rechargeDelay = 5f;
        [SerializeField] private float rechargeRate = 10f; // HP/sec

        [Header("Visual")]
        [Tooltip("护盾 MeshRenderer（如未指定则用自身）")]
        [SerializeField] private MeshRenderer shieldRenderer;

        private float currentShieldHp;
        private Coroutine rechargeRoutine;
        private MaterialPropertyBlock propBlock;

        private static readonly int PropHitPoint = Shader.PropertyToID("_HitPoint");
        private static readonly int PropHitTime = Shader.PropertyToID("_HitTime");
        private static readonly int PropShieldHp = Shader.PropertyToID("_ShieldHp");
        private static readonly int PropShieldMax = Shader.PropertyToID("_ShieldMaxHp");

        /// <summary>护盾是否激活（HP > 0）</summary>
        public bool IsActive => currentShieldHp > 0f;

        private void Awake()
        {
            if (shieldRenderer == null) shieldRenderer = GetComponent<MeshRenderer>();
            propBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            currentShieldHp = maxShieldHp;
            UpdateShader();
        }

        /// <summary>
        /// 护盾吸收伤害。返回穿透护盾的剩余伤害量（0 表示完全吸收）
        /// </summary>
        public float AbsorbDamage(float amount, GameObject attacker = null)
        {
            if (!IsActive) return amount;

            currentShieldHp -= amount;

            // 传递受击点给 Shader
            if (attacker != null)
                SendHitPoint(attacker.transform.position);

            // 重置回充协程
            if (rechargeRoutine != null) StopCoroutine(rechargeRoutine);
            rechargeRoutine = StartCoroutine(RechargeAfterDelay());

            if (currentShieldHp <= 0f)
            {
                currentShieldHp = 0f;
                UpdateShader();
                OnShieldBreak();
                return -currentShieldHp; // 返回溢出伤害
            }

            UpdateShader();
            return 0f;
        }

        private void OnShieldBreak()
        {
            Debug.Log("<color=cyan>Shield Broken!</color>");
            // 可以在这里播放护盾破碎特效
        }

        private IEnumerator RechargeAfterDelay()
        {
            yield return new WaitForSeconds(rechargeDelay);

            while (currentShieldHp < maxShieldHp)
            {
                currentShieldHp = Mathf.Min(currentShieldHp + rechargeRate * Time.deltaTime, maxShieldHp);
                UpdateShader();
                yield return null;
            }
        }

        private void SendHitPoint(Vector3 worldPos)
        {
            if (shieldRenderer == null) return;
            shieldRenderer.GetPropertyBlock(propBlock);
            propBlock.SetVector(PropHitPoint, new Vector4(worldPos.x, worldPos.y, worldPos.z, 0));
            propBlock.SetFloat(PropHitTime, Time.time);
            shieldRenderer.SetPropertyBlock(propBlock);
        }

        private void UpdateShader()
        {
            if (shieldRenderer == null) return;
            shieldRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(PropShieldHp, currentShieldHp);
            propBlock.SetFloat(PropShieldMax, maxShieldHp);
            shieldRenderer.SetPropertyBlock(propBlock);
        }
    }
}
