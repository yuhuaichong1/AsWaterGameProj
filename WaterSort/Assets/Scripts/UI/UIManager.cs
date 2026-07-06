/**
 * UI视图管理器
 */

using UnityEngine;
using System.Collections.Generic;
using System;
using cfg;
using UnityEngine.UI;
using System.IO;
using System.Collections;

namespace XrCode
{
    public class UIManager : Singleton<UIManager>, ILoad, IDispose
    {
        // UI根节点
        private Transform canvas;
        private BaseUI curUI;
        private GraphicRaycaster graphicRaycaster;
        // UI字典
        private Dictionary<byte, BaseUI> uiDic;

        private Dictionary<int, Transform> uiLevelNodeDic;
        public Camera UICamera { get; private set; }

        private UINotice uiNotice;

        private Stack<BaseUI> childUIStack;//二级UI栈

        /// <summary>
        /// 初始化Canvas
        /// </summary>
        public void Load()
        {
            uiDic = new Dictionary<byte, BaseUI>();
            childUIStack = new Stack<BaseUI>();
            canvas = GameObject.Find("Canvas").transform;
            UICamera = canvas.Find("UICamera").GetComponent<Camera>();
            graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
            uiLevelNodeDic = new Dictionary<int, Transform>()
            {
                {  1, canvas.Find("Level1")},
                {  2, canvas.Find("Level2")},
                {  3, canvas.Find("Level3")},
                {  4, canvas.Find("Level4")},
                {  5, canvas.Find("Level5")},
                {  6, canvas.Find("Level6")},
                {  7, canvas.Find("Level7")},
                {  8, canvas.Find("Level8")},
            };
            ViewConfig.Init();
        }
        public void Dispose() { }

        #region 异步打开UIPanel

        // 异步打开UI界面
        public void OpenAsync<T>(EUIType uType, UIOpenType uOpenType = UIOpenType.None, System.Action<BaseUI> callBack = null, params object[] data) where T : BaseUI, new()
        {
            // 同一UI不重复打开
            if (curUI != null && curUI.UIType == uType) return;

            //从缓存中取ui
            if (!uiDic.TryGetValue((byte)uType, out BaseUI ui))
            {
                AsyncLoadUI<T>(uType, uOpenType, callBack, data);
                return;
            }

            if (ui == null) { return; }
            OpenUI(ui);
            ui.SetParam(data);
            ui.Show();
            ui.Enable();
            callBack?.Invoke(ui);

            ChildOpen(ui, uOpenType);
        }

        private void ChildOpen(BaseUI ui, UIOpenType uOpenType)
        {
            switch (uOpenType)
            {
                case UIOpenType.None:
                    break;
                case UIOpenType.Cover:
                    // 覆盖：直接入栈，不关闭当前UI
                    childUIStack.Push(ui);
                    break;
                case UIOpenType.Replace:
                    // 替换：关闭栈顶UI，新UI入栈
                    if (childUIStack.Count > 0)
                    {
                        BaseUI topUI = childUIStack.Peek();
                        if (topUI != null)
                        {
                            CloseUI(topUI.UIType, UICloseType.None); // 只关闭，不触发Pop逻辑
                        }
                        childUIStack.Pop();
                    }
                    childUIStack.Push(ui);
                    break;
                case UIOpenType.Back:
                    // 回退：找到目标UI，关闭中间的UI
                    GoTargetChildUI(ui.UIType);
                    // 找到目标后，将新UI入栈
                    childUIStack.Push(ui);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 递归查找目标UI，关闭中间的UI
        /// 优化：改为迭代方式，避免栈溢出
        /// </summary>
        private void GoTargetChildUI(EUIType targetType)
        {
            if (childUIStack.Count == 0)
            {
                return;
            }

            // 改为迭代方式，避免递归栈溢出
            List<BaseUI> tempList = new List<BaseUI>();
            bool found = false;

            // 从栈顶开始查找目标UI
            while (childUIStack.Count > 0)
            {
                BaseUI topUI = childUIStack.Pop();
                if (topUI != null && topUI.UIType == targetType)
                {
                    // 找到目标，重新入栈
                    childUIStack.Push(topUI);
                    // 将目标UI设置为最前，解决同层级遮挡问题
                    topUI.SetAsLastSibling();
                    found = true;
                    break;
                }
                // 没找到，保存到临时列表，后续关闭
                tempList.Add(topUI);
            }

            // 关闭中间的UI
            foreach (var ui in tempList)
            {
                if (ui != null)
                {
                    CloseUI(ui.UIType, UICloseType.None); // 只关闭，不触发Pop逻辑
                }
            }

            // 如果没找到目标，记录日志
            if (!found && tempList.Count > 0)
            {
                D.Log($"[UIManager] 未找到目标UI: {targetType}，已清空中间UI");
            }
        }

        //异步加载UI
        private void AsyncLoadUI<T>(EUIType uType, UIOpenType uOpenType, Action<BaseUI> cb, params object[] data) where T : BaseUI, new()
        {
            // 第一次加载新的ui
            ConfUIRes conf = ConfigModule.Instance.Tables.TbUIRes.GetOrDefault((int)uType);
            if (conf != null)
            {
                uiDic.Add((byte)uType, null);
                Debug.Log($"[ConfUI]: 加载ui {conf.Sn} ___ {conf.UiPath}");

                ResourceMod.Instance.AsyncLoad<UnityEngine.Object>(conf.UiPath,
                    (obj) =>
                    {
                        GameObject uiObj = InitUITransform(obj, conf.UiLevel);
                        if (uiObj == null) return; // 空值检查
                        T t = LoadUI<T>(uType, uiObj, conf, data);
                        cb?.Invoke(t);
                        ChildOpen(t, uOpenType);
                    });
            }
        }

        #endregion

        #region 同步打开UIPanel

        // 同步打开UI界面（添加UIOpenType支持）
        public void OpenSync<T>(EUIType uType, UIOpenType uOpenType = UIOpenType.None, System.Action<BaseUI> callBack = null, params object[] data) where T : BaseUI, new()
        {
            // 同一UI不重复打开
            if (curUI != null && curUI.UIType == uType) return;

            //从缓存中取ui
            if (!uiDic.TryGetValue((byte)uType, out BaseUI ui))
            {
                SyncLoadUI<T>(uType, uOpenType, callBack, data);
                return;
            }

            if (ui == null) { return; }
            OpenUI(ui);
            ui.SetParam(data);
            ui.Show();
            ui.Enable();
            callBack?.Invoke(ui);

            ChildOpen(ui, uOpenType);
        }

        //同步加载UI
        private void SyncLoadUI<T>(EUIType uType, UIOpenType uOpenType, Action<BaseUI> cb, params object[] data) where T : BaseUI, new()
        {
            // 第一次加载新的ui
            ConfUIRes conf = ConfigModule.Instance.Tables.TbUIRes.GetOrDefault((int)uType);
            if (conf != null)
            {
                Debug.Log($"[ConfUI]: 加载ui {conf.Sn}");
#if !UNITY_WEBGL
                var obj = ResourceMod.Instance.SyncLoad<GameObject>(conf.UiPath);
                GameObject go = InitUITransform(obj, conf.UiLevel);
                if (go == null) return; // 空值检查
                T t = LoadUI<T>(uType, go, conf, data);
                cb?.Invoke(t);
                ChildOpen(t, uOpenType);
#else
            //webgl平台同步加载也为异步加载
            ResourceMod.Instance.AsyncLoad<UnityEngine.Object>(conf.UiPath, (obj)=>
            {
                GameObject uiObj = InitUITransform(obj, conf.UiLevel);
                if (uiObj == null) return; // 空值检查
                T t = LoadUI<T>(uType, uiObj, conf, data);
                cb?.Invoke(t);
                ChildOpen(t, uOpenType);
            });
#endif
            }
        }

        #endregion

        //打开ui
        private void OpenUI(BaseUI ui)
        {
            if(ui == null) return;
            if (curUI != null)
            {
                if (ui.IsFullScreen) curUI.Hide();
                curUI.Disable();
            }
            curUI = ui;
            // 将UI设置为父级最前，解决同层级遮挡问题
            ui.SetAsLastSibling();
        }

        // 初始化ui gameObject
        private GameObject InitUITransform(UnityEngine.Object obj, int UiLevel)
        {
            if (obj == null)
            {
                D.Error("[UIManager] InitUITransform: obj is null");
                return null;
            }

            GameObject go = GameObject.Instantiate(obj) as GameObject;
            if (go == null)
            {
                D.Error("[UIManager] InitUITransform: Instantiate failed");
                return null;
            }

            // 空值检查和降级处理
            if (!uiLevelNodeDic.TryGetValue(UiLevel, out Transform parent) || parent == null)
            {
                D.Error($"[UIManager] UI层级 {UiLevel} 不存在，降级到Canvas");
                parent = canvas; // 降级到canvas
            }

            go.transform.parent = parent;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                D.Error("[UIManager] InitUITransform: RectTransform is null");
                return go;
            }

            rt.anchorMax = Vector2.one;
            rt.anchorMin = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;
            return go;
        }

        // 加载ui对象
        private T LoadUI<T>(EUIType uType, GameObject go, ConfUIRes conf, params object[] data) where T : BaseUI, new()
        {
            if (go == null)
            {
                D.Error("[UIManager] LoadUI: GameObject is null");
                return null;
            }

            T ui = new T();
            ui.Create(go);
            ui.SetFullScreen(conf.IsFullScreen);

            OpenUI(ui);

            ui.SetUIType(uType);
            ui.SetParam(data);
            ui.Awake();
            ui.Show();
            ui.Enable();
            if (!uiDic.ContainsKey((byte)uType))
                uiDic.Add((byte)uType, ui);
            else
            {
                if (uiDic[(byte)uType] == null)
                    uiDic[(byte)uType] = ui;
            }
            curUI = ui;
            return (T)ui;
        }
        public void CloseUI(EUIType uiType, UICloseType uCloseType = UICloseType.None)
        {
            if (!uiDic.TryGetValue((byte)uiType, out BaseUI ui) || ui == null)
            {
                return;
            }

            ui.Hide();
            bool isCurrentUI = curUI != null && curUI.UIType == uiType;
            if (isCurrentUI)
            {
                curUI.Disable();
                curUI = null;
            }

            switch(uCloseType) 
            { 
                case UICloseType.None:
                    break;
                case UICloseType.Pop:
                    HandlePopClose(uiType);
                    break;
                case UICloseType.Clear:
                    HandleClearClose();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 处理Pop关闭：恢复上一个UI（不执行SetParam）
        /// </summary>
        private void HandlePopClose(EUIType closedUIType)
        {
            if (childUIStack.Count == 0)
            {
                // 栈为空，curUI已经在CloseUI中设置为null
                return;
            }

            // 移除当前关闭的UI（Pop关闭时，栈顶应该是当前关闭的UI）
            if (childUIStack.Count > 0)
            {
                BaseUI topUI = childUIStack.Peek();
                if (topUI != null && topUI.UIType == closedUIType)
                {
                    childUIStack.Pop();
                }
            }

            // 恢复上一个UI（不执行SetParam）
            if (childUIStack.Count > 0)
            {
                BaseUI preUI = childUIStack.Peek();
                if (preUI != null)
                {
                    OpenUI(preUI);
                    preUI.Show();
                    preUI.Enable();
                    curUI = preUI;
                    // 将恢复的UI设置为最前，解决同层级遮挡问题
                    preUI.SetAsLastSibling();
                }
            }
        }

        /// <summary>
        /// 处理Clear关闭：清空所有二级UI
        /// </summary>
        private void HandleClearClose()
        {
            // 关闭栈中所有UI
            while (childUIStack.Count > 0)
            {
                BaseUI ui = childUIStack.Pop();
                if (ui != null)
                {
                    ui.Hide();
                    ui.Disable();
                }
            }
            childUIStack.Clear();
        }

        public void Show(EUIType uiType)
        {
            if (uiDic.TryGetValue((byte)uiType, out BaseUI ui))
            {
                ui.Show();
            }
        }

        public GameObject OpenNotice(string msg)
        {
            GameObject obj = null;
            if(uiNotice != null)
            {
                obj = uiNotice.ShowInfo(msg);
            }
            else
            {
                ConfUIRes conf = ConfigModule.Instance.Tables.TbUIRes.GetOrDefault((int)EUIType.EUINotice);
                if (conf != null)
                {
                    Debug.Log($"[ConfUI]: 加载ui {conf.Sn} ___ {conf.UiPath}");
                    ResourceMod.Instance.AsyncLoad<UnityEngine.Object>(conf.UiPath,
                        (obj) =>
                        {
                            GameObject uiObj = InitUITransform(obj, conf.UiLevel);
                            uiNotice = LoadUI<UINotice>(EUIType.EUINotice, uiObj, conf, null);
                            obj = uiNotice.ShowInfo(msg);
                        });
                }
                
            }

            return obj;
        }

        public GameObject OpenNotice2(string msg)
        {
            GameObject obj = null;
            if (uiNotice != null)
            {
                obj = uiNotice.ShowInfo2(msg);
            }
            else
            {
                ConfUIRes conf = ConfigModule.Instance.Tables.TbUIRes.GetOrDefault((int)EUIType.EUINotice);
                if (conf != null)
                {
                    Debug.Log($"[ConfUI]: 加载ui {conf.Sn} ___ {conf.UiPath}");
                    ResourceMod.Instance.AsyncLoad<UnityEngine.Object>(conf.UiPath,
                        (obj) =>
                        {
                            GameObject uiObj = InitUITransform(obj, conf.UiLevel);
                            uiNotice = LoadUI<UINotice>(EUIType.EUINotice, uiObj, conf, null);
                            obj = uiNotice.ShowInfo2(msg);
                        });
                }

            }

            return obj;
        }

        public void HideNotice(GameObject notice)
        {
            uiNotice?.HideInfo(notice);
        }

        public string GetUIPath(string[] strArray)
        {
            ConfUIRes confData = ConfigModule.Instance.Tables.TbUIRes.Get(int.Parse(strArray[0]));
            int uiLevel = confData.UiLevel;
            string uiName = Path.GetFileNameWithoutExtension(confData.UiPath);
            string objPath = strArray[1];

            return $"{canvas.name}/{uiLevelNodeDic[uiLevel].name}/{uiName}(Clone)/{objPath}";
        }

        public void Update()
        {
            curUI?.Update();
        }

        public void SetGraphicRaycaster(bool b)
        {
            if (graphicRaycaster != null)
                graphicRaycaster.enabled = b;
        }
    }

}