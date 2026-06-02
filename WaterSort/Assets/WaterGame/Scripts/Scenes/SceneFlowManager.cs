using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AsGame.Ads;
using AsGame.Core;
using AsGame.Events;
using AsGame.Water;
using AsGame.UI;

namespace AsGame.Scenes
{
    public class SceneFlowManager : MonoBehaviour
    {
        public static SceneFlowManager Instance { get; private set; }
        public static SceneId CurrentScene { get; private set; } = SceneId.Loading;

        GameObject _loadingCanvas;
        GameObject _homeCanvas;
        GameObject _gameplayCanvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("SceneFlowManager", typeof(SceneFlowManager));
            DontDestroyOnLoad(go);
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
            EnsureEventSystem();
            PopupManager.Ensure();
            AdsService.Initialize();
            if (AudioManager.Instance == null)
                new GameObject("AudioManager", typeof(AudioManager));
            LoadScene(SceneId.Loading);
        }

        static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }

        public static void LoadScene(SceneId scene)
        {
            if (Instance == null) return;
            Instance.Load(scene);
        }

        void Load(SceneId scene)
        {
            PopupManager.Instance?.ClearAll();
            DestroySceneCanvases();
            CurrentScene = scene;
            EventBus.Publish(GameEvents.SceneChange, scene);

            switch (scene)
            {
                case SceneId.Loading:
                    _loadingCanvas = ResourcePrefabLoader.Instantiate(PrefabPaths.LoadingCanvas);
                    if (_loadingCanvas != null) _loadingCanvas.name = "LoadingCanvas";
                    break;
                case SceneId.Home:
                    _homeCanvas = ResourcePrefabLoader.Instantiate(PrefabPaths.HomeCanvas);
                    if (_homeCanvas != null) _homeCanvas.name = "HomeCanvas";
                    break;
                case SceneId.Game:
                    GameSceneBuilder.Build();
                    _gameplayCanvas = GameObject.Find("GameplayCanvas");
                    break;
            }
        }

        void DestroySceneCanvases()
        {
            if (_gameplayCanvas != null) Destroy(_gameplayCanvas);
            if (_loadingCanvas != null) Destroy(_loadingCanvas);
            if (_homeCanvas != null) Destroy(_homeCanvas);
            _gameplayCanvas = _loadingCanvas = _homeCanvas = null;

            var legacy = GameObject.Find("GameplayCanvas");
            if (legacy != null) Destroy(legacy);
            legacy = GameObject.Find("LoadingCanvas");
            if (legacy != null) Destroy(legacy);
            legacy = GameObject.Find("HomeCanvas");
            if (legacy != null) Destroy(legacy);
        }
    }
}
