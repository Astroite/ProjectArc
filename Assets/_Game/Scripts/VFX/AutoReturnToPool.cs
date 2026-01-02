using UnityEngine;
using ProjectArc.Core;
using System.Collections;

namespace ProjectArc.VFX
{
    /// <summary>
    /// 挂载在特效预制体上，用于在指定时间后自动回收到对象池
    /// </summary>
    public class AutoReturnToPool : MonoBehaviour
    {
        [Tooltip("特效播放时长，超过这个时间自动回收")]
        [SerializeField] private float lifetime = 2f;

        private void OnEnable()
        {
            StartCoroutine(ReturnRoutine());
        }

        private IEnumerator ReturnRoutine()
        {
            yield return new WaitForSeconds(lifetime);
            
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnObject(this.gameObject);
            }
            else
            {
                // 如果没有池子（比如测试场景），就直接销毁
                Destroy(gameObject);
            }
        }
    }
}