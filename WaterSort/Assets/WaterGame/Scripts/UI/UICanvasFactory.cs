using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;

namespace AsGame.UI
{
    public static class UICanvasFactory
    {
        public static GameObject CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(750, 1334);
            s.matchWidthOrHeight = 0.5f;
            return go;
        }

        public static Image CreateFullScreenImage(Transform parent, string objectName, Color fallback)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = fallback;
            return img;
        }

        public static Text CreateText(Transform parent, string objectName, string txt, int size, Vector2 pos)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(600, 50);
            var t = go.GetComponent<Text>();
            t.text = txt;
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return t;
        }

        public static void TrySetSprite(Image img, string path)
        {
            var sp = Core.GameResourceLoader.LoadSprite(path);
            if (sp == null) return;
            img.sprite = sp;
            img.color = Color.white;
            img.preserveAspect = true;
            img.SetNativeSize();
        }

        public static void TrySetFullScreenSprite(Image img, string path, bool preserveAspect = true)
        {
            var sp = Core.GameResourceLoader.LoadSprite(path);
            if (sp == null) return;
            img.sprite = sp;
            img.color = Color.white;
            img.type = Image.Type.Simple;
            img.preserveAspect = preserveAspect;
        }
    }
}
