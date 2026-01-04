using UnityEngine;
using ProjectArc.Core.Data;

namespace ProjectArc.Core
{
    /// <summary>
    /// 挂载在每个关卡场景中，用于配置该关卡的主题数据
    /// </summary>
    public class LevelConfigurator : MonoBehaviour
    {
        [Tooltip("拖入当前关卡的主题配置单 (Theme SO)")]
        [SerializeField] private LevelTheme levelTheme;

        private void Start()
        {
            if (levelTheme != null && ObjectPoolManager.Instance != null)
            {
                // 1. 初始化对象池
                ObjectPoolManager.Instance.InitializeTheme(levelTheme);

                // 2. 设置环境（天空盒、雾等）
                // ApplyEnvironmentSettings();
            }
            else
            {
                Debug.LogWarning("LevelConfigurator: Missing Theme or ObjectPoolManager!");
            }
        }

        private void ApplyEnvironmentSettings()
        {
            if (levelTheme.skyboxMaterial != null)
            {
                RenderSettings.skybox = levelTheme.skyboxMaterial;
            }
            
            RenderSettings.fog = true;
            RenderSettings.fogColor = levelTheme.fogColor;
            RenderSettings.fogDensity = levelTheme.fogDensity;
            
            // 更新光照
            // DynamicGI.UpdateEnvironment();
        }
    }
}