using System;
using System.Collections.Generic;
using UnityEngine;

namespace WZSDK
{
    public class ViewInfo
    {
        public string prefabName;
        public Transform parentTf;
    }

    // Compatibility entry point for old code. Actual lifecycle is now owned by UIManager.
    public class ViewManager
    {
        public Transform canvasTf;
        public Dictionary<int, IBaseView> _opens;
        public Dictionary<int, IBaseView> _viewCache;
        public Dictionary<int, ViewInfo> _views;

        public ViewManager()
        {
            canvasTf = ResolveCanvasTransform();
            _opens = new Dictionary<int, IBaseView>();
            _views = new Dictionary<int, ViewInfo>();
            _viewCache = new Dictionary<int, IBaseView>();
            RegisterDefaultViews();
        }

        public void Register(int key, ViewInfo viewInfo)
        {
            if (viewInfo == null)
            {
                return;
            }

            if (viewInfo.parentTf == null)
            {
                viewInfo.parentTf = ResolveCanvasTransform();
            }

            _views[key] = viewInfo;
            SyncRegistration(key);
        }

        public void Register(ViewType viewType, ViewInfo viewInfo)
        {
            Register((int)viewType, viewInfo);
        }

        public void UnRegister(int key)
        {
            _views.Remove(key);
            _viewCache.Remove(key);
            _opens.Remove(key);
            UIManager.instance?.UnregisterLegacyView(key);
        }

        public void RemoveView(int key)
        {
            UnRegister(key);
        }

        public bool IsOpen(int key)
        {
            return UIManager.instance != null && UIManager.instance.HasActiveLegacyView(key);
        }

        public IBaseView GetView(int key)
        {
            SyncRegistration(key);

            IBaseView view = UIManager.instance?.GetLegacyView(key);
            if (view != null)
            {
                _opens[key] = view;
                _viewCache[key] = view;
            }

            return view;
        }

        public T GetView<T>(int key) where T : class, IBaseView
        {
            return GetView(key) as T;
        }

        public void Destroy(int key)
        {
            if (UIManager.instance == null)
            {
                Debug.LogError("UIManager.instance is null, cannot destroy unified UI view.");
                return;
            }

            UIManager.instance.CloseLegacyView(key);
            UIManager.instance.UnregisterLegacyView(key);
            _opens.Remove(key);
            _viewCache.Remove(key);
            _views.Remove(key);
        }

        public void Close(int key, params object[] args)
        {
            if (UIManager.instance == null)
            {
                Debug.LogError("UIManager.instance is null, cannot close unified UI view.");
                return;
            }

            UIManager.instance.CloseLegacyView(key, args);
            _opens.Remove(key);
            _viewCache.Remove(key);
        }

        public void Open(ViewType viewType, params object[] args)
        {
            Open((int)viewType, args);
        }

        public void Open(int key, params object[] args)
        {
            if (UIManager.instance == null)
            {
                Debug.LogError("UIManager.instance is null, cannot open unified UI view.");
                return;
            }

            SyncRegistration(key);
            IBaseView view = UIManager.instance.OpenLegacyView(key, args);
            if (view != null)
            {
                _opens[key] = view;
                _viewCache[key] = view;
            }
        }

        private void SyncRegistration(int key)
        {
            if (UIManager.instance == null)
            {
                return;
            }

            if (!_views.TryGetValue(key, out ViewInfo viewInfo))
            {
                return;
            }

            if (viewInfo.parentTf == null)
            {
                viewInfo.parentTf = ResolveCanvasTransform();
            }

            UIManager.instance.RegisterLegacyView(key, viewInfo);
        }

        private Transform ResolveCanvasTransform()
        {
            if (canvasTf != null)
            {
                return canvasTf;
            }

            Transform canvasLevel = GameObject.Find("Canvas/Level5")?.transform;
            if (canvasLevel != null)
            {
                canvasTf = canvasLevel;
                return canvasTf;
            }

            if (UIManager.instance != null)
            {
                canvasTf = UIManager.instance.GetLayerRoot(UIManager.UILayer.Layer5);
            }

            return canvasTf;
        }

        private void RegisterDefaultViews()
        {
            RegisterDefaultView(ViewType.kaiju_ui_1, "kaiju_ui_1");
            RegisterDefaultView(ViewType.kaiju_ui_1_1, "kaiju_ui_1_1");
            RegisterDefaultView(ViewType.kaiju_ui_2, "kaiju_ui_2");
            RegisterDefaultView(ViewType.kaiju_ui_2_1, "kaiju_ui_2_1");
            RegisterDefaultView(ViewType.kaiju_ui_3, "kaiju_ui_3");
            RegisterDefaultView(ViewType.kaiju_ui_4, "kaiju_ui_4");
            RegisterDefaultView(ViewType.kaiju_ui_5, "kaiju_ui_5");
            RegisterDefaultView(ViewType.SuccessMessage, "SuccessMessage");
            RegisterDefaultView(ViewType.WithDrawSucMessage, "WithDrawSucMessage");
            RegisterDefaultView(ViewType.WithDraw, "WithDraw");
            RegisterDefaultView(ViewType.WithDrawMethod, "WithDrawmethod");
            RegisterDefaultView(ViewType.WithDrawQues, "WithDrawQues");
            RegisterDefaultView(ViewType.WithDrawConfirm, "WithDrawConfirm");
            RegisterDefaultView(ViewType.WithDrawProcess, "WithDrawProcess");
            RegisterDefaultView(ViewType.WithDrawProcess1, "WithDrawProcess1");
            RegisterDefaultView(ViewType.WithDrawKeepEarn, "WithDrawKeepEarn");
            RegisterDefaultView(ViewType.WithDrawContinue, "WithDrawContinue");
            RegisterDefaultView(ViewType.WithDrawSuc, "WithDrawSuc");
            RegisterDefaultView(ViewType.WithDrawDateNow, "WithDrawDateNow");
            RegisterDefaultView(ViewType.WithDrawHistory, "WithDrawHistory");
            RegisterDefaultView(ViewType.RewardMessageWith, "RewardMessageWith");
            RegisterDefaultView(ViewType.RewardMessageMoney, "RewardMessageMoney");
            RegisterDefaultView(ViewType.RewardMessageDia, "RewardMessageDia");
        }

        private void RegisterDefaultView(ViewType viewType, string prefabName)
        {
            _views[(int)viewType] = new ViewInfo
            {
                prefabName = prefabName,
                parentTf = ResolveCanvasTransform()
            };
        }
    }
}
