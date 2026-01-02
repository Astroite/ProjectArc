using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Core
{
    /// <summary>
    /// 简易事件系统
    /// 使用方法: 
    /// 1. 监听: EventManager.AddListener("PlayerDied", OnPlayerDied);
    /// 2. 触发: EventManager.TriggerEvent("PlayerDied");
    /// 3. 移除: EventManager.RemoveListener("PlayerDied", OnPlayerDied);
    /// </summary>
    public static class EventManager
    {
        // 存储无参数事件
        private static Dictionary<string, Action> _eventDictionary = new Dictionary<string, Action>();
        
        // 存储带参数事件 (使用 object 传递数据，需要拆箱)
        private static Dictionary<string, Action<object>> _paramEventDictionary = new Dictionary<string, Action<object>>();

        #region No Parameters
        public static void AddListener(string eventName, Action listener)
        {
            if (!_eventDictionary.ContainsKey(eventName))
            {
                _eventDictionary.Add(eventName, null);
            }
            _eventDictionary[eventName] += listener;
        }

        public static void RemoveListener(string eventName, Action listener)
        {
            if (_eventDictionary.ContainsKey(eventName))
            {
                _eventDictionary[eventName] -= listener;
            }
        }

        public static void TriggerEvent(string eventName)
        {
            if (_eventDictionary.TryGetValue(eventName, out Action action))
            {
                action?.Invoke();
            }
        }
        #endregion

        #region With Parameters
        // 泛型封装，避免外部直接处理 object
        public static void AddListener<T>(string eventName, Action<T> listener)
        {
            Action<object> wrapper = (obj) => 
            {
                if (obj is T tObj) listener(tObj);
                else Debug.LogError($"[EventManager] Event {eventName} parameter type mismatch. Expected {typeof(T)}, got {obj?.GetType()}");
            };

            // 注意：这里简单的 Action<object> 包装在 RemoveListener 时会比较麻烦
            // 这是一个简化版本。对于生产环境，通常建议定义强类型 Event Channels (ScriptableObject)
            // 但对于当前原型阶段，我们先用简单的 Action<object> 存储
            
            if (!_paramEventDictionary.ContainsKey(eventName))
            {
                _paramEventDictionary.Add(eventName, null);
            }
            // 为了简化 Demo，这里暂不处理 Wrapper 的移除映射问题，
            // 实际项目中建议使用 Dictionary<Delegate, Action<object>> 来映射
            _paramEventDictionary[eventName] += (o) => listener((T)o);
        }

        public static void TriggerEvent(string eventName, object data)
        {
            if (_paramEventDictionary.TryGetValue(eventName, out Action<object> action))
            {
                action?.Invoke(data);
            }
        }
        #endregion

        /// <summary>
        /// 场景切换时清理所有事件，防止内存泄漏
        /// </summary>
        public static void ClearAll()
        {
            _eventDictionary.Clear();
            _paramEventDictionary.Clear();
        }
    }
}