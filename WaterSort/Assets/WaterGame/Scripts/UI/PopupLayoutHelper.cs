using System;
using UnityEngine;
using UnityEngine.UI;

namespace AsGame.UI
{
    /// <summary>弹窗预制体（Cocos 导入）布局校正与节点绑定。</summary>
    public static class PopupLayoutHelper
    {
        public static bool HasPrefabLayout(RectTransform content) =>
            content != null && content.childCount > 0;

        public static void StretchFullScreen(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        public static void NormalizePopupHost(Transform host)
        {
            if (host == null) return;
            StretchFullScreen(host as RectTransform);
            StretchFullScreen(host.Find("background") as RectTransform);
            StretchFullScreen(host.Find("content") as RectTransform);
        }

        public static void SetRect(Transform root, string path, Vector2 size, Vector2 anchoredPos)
        {
            var rt = root.Find(path) as RectTransform;
            if (rt == null) return;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            var img = rt.GetComponent<Image>();
            if (img != null && img.sprite != null)
                img.type = Image.Type.Sliced;
        }

        public static void BindButton(Transform root, string path, Action onClick)
        {
            var btn = root.Find(path)?.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        public static void SetText(Transform root, string path, string text)
        {
            var label = root.Find(path)?.GetComponent<Text>();
            if (label != null)
                label.text = text;
        }

        public static void SetActive(Transform root, string path, bool active)
        {
            var node = root.Find(path);
            if (node != null)
                node.gameObject.SetActive(active);
        }
    }
}
