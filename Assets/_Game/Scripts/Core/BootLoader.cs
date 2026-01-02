using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace ProjectArc.Core
{
    public class BootLoader : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("要跳转的目标场景名称（必须在 Build Settings 中添加）")]
        [SerializeField] private string targetSceneName = "L_Test_01";
        
        [Tooltip("启动停留时间（用于展示 Logo 或等待初始化完成）")]
        [SerializeField] private float waitTime = 1.0f;

        private IEnumerator Start()
        {
            // 1. 在这里可以进行一些全局初始化
            // 比如：Application.targetFrameRate = 60;
            // 或者初始化你的单例：ObjectPoolManager.Instance.Init(); 
            // (虽然单例通常在Awake自启动，但有时需要手动控制顺序)
            
            Debug.Log("BootLoader: Initializing systems...");

            // 2. 等待一段时间（可选，模拟加载或展示Logo）
            yield return new WaitForSeconds(waitTime);

            // 3. 加载下一个场景
            Debug.Log($"BootLoader: Loading scene {targetSceneName}...");
            SceneManager.LoadScene(targetSceneName);
        }
    }
}