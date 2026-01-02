using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectArc.Gameplay.Player
{
    /// <summary>
    /// 炮台控制器
    /// 核心功能：接收目标方向并旋转炮台模型
    /// </summary>
    public class TurretController : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private float rotationSmoothness = 20f;

        // 角度限制 (相对于正上方 90度)
        [Range(0, 360)] [SerializeField] private float minAngle = 90f; // 正前方
        [Range(0, 360)] [SerializeField] private float maxAngle = 180f; // 正左方

        [Header("References")] [SerializeField]
        private Transform turretPivot; // 炮台旋转轴心

        private Quaternion _targetRotation;

        private void Start()
        {
            // 初始角度
            _targetRotation = turretPivot.rotation;
        }

        private void Update()
        {
            // 平滑旋转
            turretPivot.rotation = Quaternion.Slerp(turretPivot.rotation, _targetRotation, Time.deltaTime * rotationSmoothness);
        }

        /// <summary>
        /// 供 UI 调用的瞄准方法
        /// </summary>
        /// <param name="direction">从圆心指向触摸点的归一化向量 (UI坐标系)</param>
        public void UpdateAimDirection(Vector2 direction)
        {
            // 1. 计算原始角度 [0, 360]
            // Atan2(x, y) 对应 Unity 中的 (0, angle, 0) 旋转：0=前, 90=右, 180=后, 270=左
            float angle = (Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg + 360) % 360;

            // 2. 智能钳制 (Smart Clamp)
            // 计算扇形区域的中心点和半径
            float midAngle = (minAngle + maxAngle) * 0.5f;
            float range = (maxAngle - minAngle) * 0.5f;

            // 使用 Mathf.DeltaAngle 计算“最短偏差值”
            // DeltaAngle 会自动处理 360/0 的边界问题 (例如 10度 和 350度 的差是 20度)
            float delta = Mathf.DeltaAngle(midAngle, angle);

            // 限制偏差值在半径范围内
            float clampedDelta = Mathf.Clamp(delta, -range, range);

            // 还原最终角度
            float finalAngle = midAngle + clampedDelta;

            _targetRotation = Quaternion.Euler(0, finalAngle, 0);
        }
        
        // 辅助可视化：在 Scene 视图画出可旋转范围
        private void OnDrawGizmos()
        {
            if (turretPivot == null) return;

            Vector3 startDir = Quaternion.Euler(0, minAngle, 0) * Vector3.forward;
            Vector3 endDir = Quaternion.Euler(0, maxAngle, 0) * Vector3.forward;
            Vector3 targetDir = _targetRotation * Vector3.forward;
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(turretPivot.position, turretPivot.position + startDir * 5f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(turretPivot.position, turretPivot.position + endDir * 5f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(turretPivot.position, turretPivot.position + targetDir * 5f);
        }
    }
}