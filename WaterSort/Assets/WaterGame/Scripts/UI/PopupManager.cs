using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AsGame.UI;
using AsGame.UI.Popups;

namespace AsGame.UI
{
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        static readonly Dictionary<PopupType, string> PrefabNames = new()
        {
            { PopupType.Setting, "SettingPopup" },
            { PopupType.Success, "SuccessPopup" },
            { PopupType.Rank, "RankPopup" },
            { PopupType.Collect, "CollectPopup" },
            { PopupType.GetCollect, "GetCollectPopup" },
            { PopupType.RecoverHeart, "RecoverHeartPopup" },
            { PopupType.GetHeart, "GetHeartPopup" },
            { PopupType.DailyHeart, "DailyHeartPopup" },
            { PopupType.NewPlay, "NewPlayPopup" },
            { PopupType.Feedback, "FeedbackPopup" },
        };

        Transform _root;
        readonly List<BasePopupView> _stack = new();

        public static void Ensure()
        {
            if (Instance != null) return;
            var go = new GameObject("PopupManager", typeof(PopupManager));
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

            var canvasGo = ResourcePrefabLoader.Instantiate(PrefabPaths.PopupCanvas, transform);
            if (canvasGo == null)
            {
                canvasGo = UICanvasFactory.CreateCanvas("PopupCanvas");
                canvasGo.transform.SetParent(transform);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.sortingOrder = 100;
            }

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            PopupLayoutHelper.StretchFullScreen(canvasRt);
            canvasRt.localScale = Vector3.one;
            _root = canvasGo.transform;
        }

        public void ShowAtOnce(PopupContext ctx)
        {
            if (TryShowFromPrefab(ctx, out var view))
            {
                _stack.Add(view);
                return;
            }

            Debug.LogError($"[PopupManager] Prefab missing for {ctx.Type}. Run AsGame → Generate All UI Prefabs.");
        }

        bool TryShowFromPrefab(PopupContext ctx, out BasePopupView view)
        {
            view = null;
            if (!PrefabNames.TryGetValue(ctx.Type, out var prefabName)) return false;

            var prefab = Resources.Load<GameObject>(PrefabPaths.PopupsRoot + prefabName);
            if (prefab == null) return false;

            var host = Instantiate(prefab, _root);
            host.name = ctx.Type + "Popup";
            PopupLayoutHelper.NormalizePopupHost(host.transform);

            var content = FindPopupContent(host.transform);
            if (content == null)
            {
                Destroy(host);
                return false;
            }

            CanvasGroup blocker = null;
            var blockTf = host.transform.Find("blockInput") ?? host.transform.Find("background");
            if (blockTf != null)
            {
                blocker = blockTf.GetComponent<CanvasGroup>();
                if (blocker == null)
                {
                    var img = blockTf.GetComponent<Image>() ?? blockTf.gameObject.AddComponent<Image>();
                    if (img.sprite == null)
                        img.color = new Color(0, 0, 0, 0.55f);
                    blocker = blockTf.gameObject.AddComponent<CanvasGroup>();
                }
            }

            view = ctx.Type switch
            {
                PopupType.Setting => host.AddComponent<SettingPopupView>(),
                PopupType.Success => host.AddComponent<SuccessPopupView>(),
                PopupType.Rank => host.AddComponent<RankPopupView>(),
                PopupType.Collect => host.AddComponent<CollectPopupView>(),
                PopupType.GetCollect => host.AddComponent<GetCollectPopupView>(),
                PopupType.RecoverHeart => host.AddComponent<RecoverHeartPopupView>(),
                PopupType.GetHeart => host.AddComponent<GetHeartPopupView>(),
                PopupType.DailyHeart => host.AddComponent<DailyHeartPopupView>(),
                PopupType.NewPlay => host.AddComponent<NewPlayPopupView>(),
                PopupType.Feedback => host.AddComponent<FeedbackPopupView>(),
                _ => host.AddComponent<SettingPopupView>()
            };

            view.Setup(ctx, content, blocker);
            return true;
        }

        public void ClearAll()
        {
            foreach (var v in _stack)
                if (v != null) Destroy(v.gameObject);
            _stack.Clear();
        }

        static RectTransform FindPopupContent(Transform host)
        {
            for (var i = 0; i < host.childCount; i++)
            {
                var child = host.GetChild(i);
                if (child.name != "content") continue;
                return child as RectTransform;
            }

            return host.Find("content") as RectTransform;
        }
    }
}
