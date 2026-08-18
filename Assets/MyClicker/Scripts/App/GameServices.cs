using MyClicker.Data;
using UnityEngine;

namespace MyClicker.App
{
    public class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }

        public SaveSystem Save { get; private set; }
        public GameConfig Config { get; private set; }

        public static GameServices Ensure()
        {
            if (Instance != null)
                return Instance;

            var existing = FindFirstObjectByType<GameServices>();
            if (existing != null)
            {
                Instance = existing;
                existing.Initialize();
                return existing;
            }

            var go = new GameObject("GameServices");
            DontDestroyOnLoad(go);
            var services = go.AddComponent<GameServices>();
            services.Initialize();
            return services;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        void Initialize()
        {
            if (Save != null)
                return;

            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;
            Save = new SaveSystem();
            Config = Resources.Load<GameConfig>("GameConfig");
            if (Config == null)
                Config = ScriptableObject.CreateInstance<GameConfig>();
        }
    }
}
