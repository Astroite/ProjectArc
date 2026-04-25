using UnityEngine;
using ProjectArc.Gameplay.Player;

namespace ProjectArc.Gameplay.Player
{
    /// <summary>
    /// 驱动操作盘 Shader 的动态参数（通过 MaterialPropertyBlock）
    /// 挂载在 ControlPad 所在的 GameObject 上，引用 TurretController 和 WorldSpaceController
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ControlPadShaderDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurretController turret;
        [SerializeField] private WorldSpaceController inputController;

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propBlock;

        // Shader 属性 ID（缓存避免每帧字符串查找）
        private static readonly int PropAimAngle = Shader.PropertyToID("_AimAngle");
        private static readonly int PropSectorMin = Shader.PropertyToID("_SectorMin");
        private static readonly int PropSectorMax = Shader.PropertyToID("_SectorMax");
        private static readonly int PropTouchActive = Shader.PropertyToID("_TouchActive");
        private static readonly int PropTouchPos = Shader.PropertyToID("_TouchPos");

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (turret == null || _renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);

            // 瞄准角度 & 扇形范围
            _propBlock.SetFloat(PropAimAngle, turret.CurrentAngle);
            _propBlock.SetFloat(PropSectorMin, turret.MinAngle);
            _propBlock.SetFloat(PropSectorMax, turret.MaxAngle);

            // 触摸状态
            bool touching = inputController != null && inputController.IsDragging;
            _propBlock.SetFloat(PropTouchActive, touching ? 1f : 0f);

            // 触摸位置（世界坐标转 ControlPad 本地 UV）
            if (touching)
            {
                Vector3 localPos = transform.InverseTransformPoint(inputController.LastHitPoint);
                // Quad 本地坐标范围约 [-0.5, 0.5]，映射到 UV [0, 1]
                Vector2 uv = new Vector2(localPos.x + 0.5f, localPos.z + 0.5f);
                _propBlock.SetVector(PropTouchPos, new Vector4(uv.x, uv.y, 0, 0));
            }

            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
