using System;
using ProjectArc.Gameplay.Combat;
using UnityEngine;

namespace ProjectArc.Gameplay.Player
{
    /// <summary>
    /// 世界空间控制器 (3D UI)
    /// 负责：
    /// 1. 发射射线检测玩家点击的 3D 区域
    /// 2. 如果点击在"操作盘"上，计算世界坐标系下的角度
    /// 3. 直接控制炮台
    /// </summary>
    public class WorldSpaceController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurretController _turret;
        [SerializeField] private WeaponSystem _weapon;
        
        [Header("Settings")]
        [Tooltip("操作盘所在的层级，用于射线检测过滤")]
        [SerializeField] private LayerMask _controlLayer;
        
        [Tooltip("炮台的中心点位置（用于计算角度的参考点）")]
        [SerializeField] private Transform _centerPoint;

        private Camera _mainCamera;
        private bool _isDragging = false;

        private Vector3 _debugHitPoint =  Vector3.zero;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            // 1. 获取屏幕输入点
            Vector3 inputScreenPos = Vector3.zero;
            bool isPressed = false;
            bool isUp = false;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButton(0))
            {
                inputScreenPos = Input.mousePosition;
                isPressed = true;
            }
            if (Input.GetMouseButtonUp(0)) isUp = true;
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    inputScreenPos = touch.position;
                    isPressed = true;
                }
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) isUp = true;
            }
#endif

            // 2. 处理抬起事件
            if (isUp)
            {
                _isDragging = false;
                // _weapon?.StopFiring();
                return;
            }

            // 3. 处理按下/拖拽事件
            if (isPressed)
            {
                Ray ray = _mainCamera.ScreenPointToRay(inputScreenPos);
                RaycastHit hit;

                // 射线检测：只检测 Control Layer
                if (Physics.Raycast(ray, out hit, 1000f, _controlLayer))
                {
                    _isDragging = true;
                    UpdateTurretAim(hit.point);
                    // _weapon?.StartFiring();
                    
                    // Debug
                    _debugHitPoint = hit.point;
                }
                else if (_isDragging) 
                {
                    // 即使手指滑出了操作盘，只要还在拖拽状态，我们依然想控制炮台
                    // 这是一个优化手感的技巧：使用无限平面与射线的交点
                    // 假设操作盘是一个水平面 (Plane)，法线向上 Vector3.up
                    Plane controlPlane = new Plane(Vector3.up, _centerPoint.position);
                    float enter;
                    if (controlPlane.Raycast(ray, out enter))
                    {
                        Vector3 hitPoint = ray.GetPoint(enter);
                        UpdateTurretAim(hitPoint);
                        
                        // Debug
                        _debugHitPoint = hitPoint;
                    }
                }
            }
        }

        private void UpdateTurretAim(Vector3 hitPoint)
        {
            if (_turret == null || _centerPoint == null) return;

            // 计算世界空间下的方向向量
            // 忽略 Y 轴高度差，只在水平面计算
            Vector3 direction = hitPoint - _centerPoint.position;
            direction.y = 0; 

            // 将 3D 方向转换为 2D 方向 (X, Z) -> (x, y) 传给 TurretController
            // 注意：TurretController 期望的是 UI 坐标系的 (x, y) 用于 Atan2 计算
            // 在 3D 世界中：X轴是右(x)，Z轴是前(y)
            Vector2 aimDir = new Vector2(direction.x, direction.z).normalized;

            _turret.UpdateAimDirection(aimDir);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_debugHitPoint, 0.2f);
        }
    }
}