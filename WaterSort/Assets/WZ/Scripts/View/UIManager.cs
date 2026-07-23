using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WZSDK
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager instance;

        public enum UILayer
        {
            Layer1 = 1,
            Layer2 = 2,
            Layer3 = 3,
            Layer4 = 4,
            Layer5 = 5,
            Layer6 = 6,
            Layer7 = 7,
            Layer8 = 8
        }

        public static HashSet<string> noAnimationViewNames = new HashSet<string>()
    {
        "LoadingView",
        "GameView",
        "GamePlayerView",
        "Tutorial",
        "UIEffectView",
    };

        [SerializeField] private Transform uiContainer;
        [SerializeField] private int maxInstanceCount = 2;

        private Dictionary<UILayer, Transform> layerContainers = new Dictionary<UILayer, Transform>();
        private Dictionary<EUIType, UIConfig> uiConfigs = new Dictionary<EUIType, UIConfig>();
        private Dictionary<EUIType, GameObject> prefabCache = new Dictionary<EUIType, GameObject>();
        private Dictionary<EUIType, Queue<BaseView>> viewPool = new Dictionary<EUIType, Queue<BaseView>>();
        private Dictionary<EUIType, List<BaseView>> activeViews = new Dictionary<EUIType, List<BaseView>>();
        public Dictionary<UILayer, List<BaseView>> layerViews = new Dictionary<UILayer, List<BaseView>>();
        private Dictionary<int, LegacyUIConfig> legacyConfigs = new Dictionary<int, LegacyUIConfig>();
        private Dictionary<int, GameObject> legacyPrefabCache = new Dictionary<int, GameObject>();
        private Dictionary<int, IBaseView> legacyActiveViews = new Dictionary<int, IBaseView>();
        private static readonly HashSet<EUIType> gameplayPersistentViews = new HashSet<EUIType>
        {
            EUIType.GameView,
            EUIType.GamePlayerView,
            EUIType.UIEffectView
        };

        [System.Serializable]
        private class UIConfig
        {
            public int sn;
            public UILayer layer;
            public string prefabPath;
            public EUIType uiType;
        }

        private class LegacyUIConfig
        {
            public int legacyId;
            public UILayer layer;
            public string prefabPath;
            public string prefabName;
            public Transform parentTf;
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                InitializeUIManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeUIManager()
        {
            InitializeLayerContainers();
            LoadUIConfigFromCSV();
            InitializeViewPools();
            InitializeLayerManagement();
        }

        private void InitializeLayerContainers()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                string layerName = $"Level{(int)layer}";
                Transform layerContainer = uiContainer.Find(layerName);
                if (layerContainer == null)
                {
                    GameObject layerObj = new GameObject(layerName);
                    layerObj.transform.SetParent(uiContainer);
                    layerObj.transform.localPosition = Vector3.zero;
                    layerObj.transform.localScale = Vector3.one;
                    layerContainer = layerObj.transform;
                }
                layerContainers[layer] = layerContainer;
            }
        }

        private void LoadUIConfigFromCSV()
        {
            uiConfigs.Clear();

            try
            {
                List<string[]> rows = UserDataManager.GetCsvRows("UIRes");
                if (rows.Count == 0)
                {
                    return;
                }

                for (int i = 1; i < rows.Count; i++)
                {
                    string[] values = rows[i];

                    if (values.Length >= 4)
                    {
                        if (!int.TryParse(values[0].Trim(), out int sn) ||
                            !int.TryParse(values[1].Trim(), out int uiLevel) ||
                            !Enum.TryParse<EUIType>(values[3].Trim(), out EUIType uiType))
                        {
                            continue;
                        }

                        string prefabPath = values[2].Trim();
                        if (string.IsNullOrEmpty(prefabPath)) continue;

                        if (prefabPath.EndsWith(".prefab"))
                        {
                            prefabPath = prefabPath.Replace(".prefab", "");
                        }

                        uiConfigs[uiType] = new UIConfig
                        {
                            sn = sn,
                            layer = (UILayer)Mathf.Clamp(uiLevel, 1, 7),
                            prefabPath = prefabPath,
                            uiType = uiType
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.LogError($"读取UI配置失败: {ex.Message}");
            }
        }

        private void InitializeViewPools()
        {
            foreach (EUIType viewType in Enum.GetValues(typeof(EUIType)))
            {
                viewPool[viewType] = new Queue<BaseView>();
                activeViews[viewType] = new List<BaseView>();
            }
        }

        private void InitializeLayerManagement()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                layerViews[layer] = new List<BaseView>();
            }
        }

        private GameObject LoadPrefab(EUIType viewType)
        {
            if (prefabCache.TryGetValue(viewType, out GameObject cachedPrefab))
            {
                return cachedPrefab;
            }

            if (!uiConfigs.TryGetValue(viewType, out UIConfig config))
            {
                Debug.LogWarning($"未找到 {viewType} 的配置");
                return null;
            }

            GameObject prefab = Resources.Load<GameObject>(config.prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"无法加载预制体: {config.prefabPath}");
                return null;
            }

            if (prefab.GetComponent<BaseView>() == null)
            {
                Debug.LogError($"预制体缺少BaseView组件: {config.prefabPath}");
                return null;
            }

            prefabCache[viewType] = prefab;
            return prefab;
        }

        private UILayer GetUILayer(EUIType viewType)
        {
            return uiConfigs.TryGetValue(viewType, out UIConfig config) ? config.layer : UILayer.Layer7;
        }

        private Transform GetLayerContainer(UILayer layer)
        {
            if (layerContainers.TryGetValue(layer, out Transform container))
            {
                return container;
            }

            GameObject layerObj = new GameObject($"Level{(int)layer}");
            layerObj.transform.SetParent(uiContainer);
            layerObj.transform.localPosition = Vector3.zero;
            layerObj.transform.localScale = Vector3.one;
            layerContainers[layer] = layerObj.transform;
            return layerObj.transform;
        }

        public Transform GetLayerRoot(UILayer layer)
        {
            return GetLayerContainer(layer);
        }

        private BaseView GetViewFromPool(EUIType viewType)
        {
            if (viewPool[viewType].Count > 0)
            {
                BaseView view = viewPool[viewType].Dequeue();
                view.gameObject.SetActive(true);
                return view;
            }

            GameObject prefab = LoadPrefab(viewType);
            if (prefab == null) return null;

            UILayer layer = GetUILayer(viewType);
            Transform parent = GetLayerContainer(layer);
            GameObject viewObject = Instantiate(prefab, parent);
            BaseView viewComponent = viewObject.GetComponent<BaseView>();

            return viewComponent;
        }

        public BaseView ShowView(EUIType viewType)
        {
            return ShowView(viewType, string.Empty);
        }

        public BaseView ShowView(EUIType viewType, params object[] args)
        {
            if (activeViews[viewType].Count > 0)
            {
                var view1 = activeViews[viewType][activeViews[viewType].Count - 1];
                view1.transform.SetAsLastSibling();

                if (args != null && args.Length > 0)
                {
                    view1.Open(args);
                }
                else
                {
                    view1.Open();
                }

                return view1;
            }

            if (activeViews[viewType].Count >= maxInstanceCount)
            {
                CloseOldestView(viewType);
            }

            BaseView view = GetViewFromPool(viewType);
            if (view == null) return null;

            UILayer layer = GetUILayer(viewType);
            Transform parent = GetLayerContainer(layer);
            view.transform.SetParent(parent);
            view.transform.SetAsLastSibling();

            layerViews[layer].Add(view);
            activeViews[viewType].Add(view);

            Canvas canvas = view.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = (int)layer * 100 + layerViews[layer].Count - 1;
            }

            if (args != null && args.Length > 0)
            {
                // 检查第一个参数是否是回调
                if (args[0] is Action<BaseView> callback)
                {
                    view.Open();
                    callback?.Invoke(view);
                }
                else
                {
                    view.Open(args);
                }
            }
            else
            {
                view.Open();
            }

            return view;
        }

        // 新增：带回调的ShowView方法
        public BaseView ShowView(EUIType viewType, Action<BaseView> onShown)
        {
            return ShowView(viewType, new object[] { onShown });
        }

        public void CloseView(EUIType viewType, BaseView specificView = null)
        {
            if (specificView != null)
            {
                if (activeViews[viewType].Contains(specificView))
                {
                    ReturnViewToPool(viewType, specificView);
                }
            }
            else if (activeViews[viewType].Count > 0)
            {
                BaseView lastView = activeViews[viewType][activeViews[viewType].Count - 1];
                ReturnViewToPool(viewType, lastView);
            }
        }

        private void ReturnViewToPool(EUIType viewType, BaseView view)
        {
            if (view == null) return;

            view.Close();

            UILayer layer = GetUILayer(viewType);
            layerViews[layer].Remove(view);
            activeViews[viewType].Remove(view);

            viewPool[viewType].Enqueue(view);
        }

        private void CloseOldestView(EUIType viewType)
        {
            if (activeViews[viewType].Count > 0)
            {
                BaseView oldestView = activeViews[viewType][0];
                ReturnViewToPool(viewType, oldestView);
            }
        }

        public T GetView<T>(EUIType viewType) where T : BaseView
        {
            if (activeViews[viewType].Count > 0)
            {
                return activeViews[viewType][activeViews[viewType].Count - 1] as T;
            }
            return null;
        }

        public bool HasActiveView(EUIType viewType)
        {
            if (!activeViews.TryGetValue(viewType, out List<BaseView> views))
                return false;

            foreach (var view in views)
            {
                if (IsViewVisible(view))
                    return true;
            }

            return false;
        }

        public void CloseAllViews()
        {
            foreach (EUIType viewType in Enum.GetValues(typeof(EUIType)))
            {
                while (activeViews[viewType].Count > 0)
                {
                    ReturnViewToPool(viewType, activeViews[viewType][0]);
                }
            }

            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                layerViews[layer].Clear();
            }

            List<int> legacyKeys = new List<int>(legacyActiveViews.Keys);
            foreach (int legacyKey in legacyKeys)
            {
                CloseLegacyView(legacyKey);
            }
        }

        // Legacy WithDraw MVC panels still call GameApp.viewManager; route them here
        // so both old and new entry points share the same UI manager.
        public void RegisterLegacyView(int key, ViewInfo viewInfo, UILayer defaultLayer = UILayer.Layer5)
        {
            if (viewInfo == null || string.IsNullOrEmpty(viewInfo.prefabName))
            {
                Debug.LogWarning($"Legacy UI {key} 注册失败，配置为空");
                return;
            }

            legacyConfigs[key] = new LegacyUIConfig
            {
                legacyId = key,
                layer = defaultLayer,
                prefabName = viewInfo.prefabName,
                prefabPath = NormalizeLegacyPrefabPath(viewInfo.prefabName),
                parentTf = viewInfo.parentTf != null ? viewInfo.parentTf : GetLayerContainer(defaultLayer)
            };
        }

        public void UnregisterLegacyView(int key)
        {
            legacyConfigs.Remove(key);
            legacyPrefabCache.Remove(key);
        }

        public bool HasLegacyRegistration(int key)
        {
            return legacyConfigs.ContainsKey(key);
        }

        public bool HasActiveLegacyView(int key)
        {
            if (!legacyActiveViews.TryGetValue(key, out IBaseView view))
            {
                return false;
            }

            if (!IsLegacyViewAlive(view))
            {
                legacyActiveViews.Remove(key);
                return false;
            }

            return true;
        }

        public IBaseView GetLegacyView(int key)
        {
            if (!legacyActiveViews.TryGetValue(key, out IBaseView view))
            {
                return null;
            }

            if (!IsLegacyViewAlive(view))
            {
                legacyActiveViews.Remove(key);
                return null;
            }

            return view;
        }

        public T GetLegacyView<T>(int key) where T : class, IBaseView
        {
            return GetLegacyView(key) as T;
        }

        public IBaseView OpenLegacyView(int key, params object[] args)
        {
            IBaseView openedView = GetLegacyView(key);
            if (openedView != null)
            {
                return openedView;
            }

            if (!legacyConfigs.TryGetValue(key, out LegacyUIConfig config))
            {
                Debug.LogError($"未找到 Legacy UI 配置: {key}");
                return null;
            }

            GameObject prefab = LoadLegacyPrefab(key, config);
            if (prefab == null)
            {
                return null;
            }

            Transform parent = config.parentTf != null ? config.parentTf : GetLayerContainer(config.layer);
            GameObject uiObject = Instantiate(prefab, parent);
            IBaseView view = ResolveLegacyView(uiObject);
            if (view == null)
            {
                Debug.LogError($"Legacy UI 预制体缺少 IBaseView 组件: {config.prefabPath}");
                Destroy(uiObject);
                return null;
            }

            view.ViewId = key;
            legacyActiveViews[key] = view;
            view.InitUi();
            view.InitData();
            view.Open(args);
            return view;
        }

        public void CloseLegacyView(int key, params object[] args)
        {
            IBaseView view = GetLegacyView(key);
            if (view == null)
            {
                legacyActiveViews.Remove(key);
                return;
            }

            legacyActiveViews.Remove(key);
            view.Close(args);
        }

        private GameObject LoadLegacyPrefab(int key, LegacyUIConfig config)
        {
            if (legacyPrefabCache.TryGetValue(key, out GameObject cachedPrefab) && cachedPrefab != null)
            {
                return cachedPrefab;
            }

            GameObject prefab = Resources.Load<GameObject>(config.prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"无法加载 Legacy UI 预制体: {config.prefabPath}");
                return null;
            }

            legacyPrefabCache[key] = prefab;
            return prefab;
        }

        private IBaseView ResolveLegacyView(GameObject uiObject)
        {
            MonoBehaviour[] behaviours = uiObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IBaseView view)
                {
                    return view;
                }
            }

            return null;
        }

        private bool IsLegacyViewAlive(IBaseView view)
        {
            if (view is MonoBehaviour behaviour)
            {
                return behaviour != null;
            }

            return view != null;
        }

        private string NormalizeLegacyPrefabPath(string prefabName)
        {
            string prefabPath = prefabName.Replace("\\", "/").Trim();
            if (prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                prefabPath = prefabPath.Substring(0, prefabPath.Length - ".prefab".Length);
            }

            if (!prefabPath.Contains("/"))
            {
                prefabPath = $"Prefab/{prefabPath}";
            }

            return prefabPath;
        }

        public void CleanupPool(EUIType viewType)
        {
            int destroyedCount = 0;
            while (viewPool[viewType].Count > maxInstanceCount)
            {
                BaseView view = viewPool[viewType].Dequeue();
                if (view != null)
                {
                    Destroy(view.gameObject);
                    destroyedCount++;
                }
            }
        }

        public void PreloadViews(EUIType viewType, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = LoadPrefab(viewType);
                if (prefab != null)
                {
                    UILayer layer = GetUILayer(viewType);
                    Transform parent = GetLayerContainer(layer);
                    GameObject viewObject = Instantiate(prefab, parent);
                    BaseView view = viewObject.GetComponent<BaseView>();
                    viewObject.SetActive(false);
                    viewPool[viewType].Enqueue(view);
                }
            }
        }


        public bool IsGameplayOnlyUIActive()
        {
            if (HasBlockingNonGameplayOverlay())
                return false;

            // 宿主 UIGamePlay 作为主 HUD 时，没有 WZ GameView/GamePlayerView 也算局内。
            if (WaterSortWZBridge.UseHostGameplayHud || WaterSortWZBridge.HostDriven)
                return true;

            return HasVisibleView(EUIType.GameView) || HasVisibleView(EUIType.GamePlayerView);
        }

        /// <summary>
        /// 是否存在非局内常驻的可见弹层。
        /// Game2 宿主局内没有 GamePlayerView 时，用此判断是否应延迟进度条刷新。
        /// </summary>
        public bool HasBlockingNonGameplayOverlay()
        {
            foreach (var kvp in activeViews)
            {
                EUIType viewType = kvp.Key;
                List<BaseView> views = kvp.Value;

                foreach (var view in views)
                {
                    if (!IsViewVisible(view))
                        continue;

                    if (!gameplayPersistentViews.Contains(viewType))
                        return true;
                }
            }

            return HasActiveLegacyView();
        }

        public bool PrintActiveViews()
        {
            StringBuilder sb = new StringBuilder();
            List<string> activePanelNames = new List<string>();

            foreach (var kvp in activeViews)
            {
                foreach (var view in kvp.Value)
                {
                    if (!IsViewVisible(view))
                        continue;

                    activePanelNames.Add($"{kvp.Key}");
                    break;
                }
            }

            if (activePanelNames.Count > 0)
                sb.Append(string.Join(", ", activePanelNames));

            return IsGameplayOnlyUIActive();
        }

        private bool HasVisibleView(EUIType viewType)
        {
            if (!activeViews.TryGetValue(viewType, out List<BaseView> views))
                return false;

            foreach (var view in views)
            {
                if (IsViewVisible(view))
                    return true;
            }

            return false;
        }

        private bool IsViewVisible(BaseView view)
        {
            if (view == null || !view.gameObject.activeInHierarchy)
                return false;

            if (view.IsShow())
                return true;

            if (view.canvasGroup == null)
                return false;

            return view.canvasGroup.alpha > 0.001f
                || view.canvasGroup.blocksRaycasts
                || view.canvasGroup.interactable;
        }

        private bool HasActiveLegacyView()
        {
            if (legacyActiveViews.Count == 0)
                return false;

            List<int> staleKeys = null;

            foreach (var kvp in legacyActiveViews)
            {
                if (IsLegacyViewAlive(kvp.Value))
                    return true;

                if (staleKeys == null)
                    staleKeys = new List<int>();

                staleKeys.Add(kvp.Key);
            }

            if (staleKeys != null)
            {
                foreach (int key in staleKeys)
                {
                    legacyActiveViews.Remove(key);
                }
            }

            return false;
        }

        private void Update()
        {

        }
        private void OnDestroy()
        {
            CloseAllViews();
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
