using UnityEngine;

namespace OrostrideToolkit.Core
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        private static T _instance;

        protected SingletonType singletonType = SingletonType.SingleScene;
        
        public static bool HasInstance => _instance != null;
    
        public static T Instance
        {
            get
            {
                if (_instance) return _instance;
                _instance = FindFirstObjectByType<T>();
                if (_instance) return _instance;
                GameObject obj = new GameObject(typeof(T).ToString());
                _instance = obj.AddComponent<T>();
                return _instance;
            }
        }
    
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (singletonType == SingletonType.Global)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

    public enum SingletonType : byte
    {
        SingleScene,
        Global
    }
}