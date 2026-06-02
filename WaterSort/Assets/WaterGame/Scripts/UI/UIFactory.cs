using UnityEngine;
using UnityEngine.UI;

namespace AsGame.UI
{
    public static class UIFactory
    {
        public static RectTransform CreatePanel(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            go.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.98f);
            return rt;
        }

        public static Text CreateLabel(Transform parent, string text, int size, Vector2 pos, Vector2? panelSize = null)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = panelSize ?? new Vector2(500, 60);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.color = Color.white;
            t.font = BasePopupView.DefaultFont;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 pos, Vector2 size,
            string objectName = "Btn")
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.95f);
            var txt = CreateLabel(go.transform, label, 26, Vector2.zero, size);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = txt.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        public static Toggle CreateToggle(Transform parent, string label, Vector2 pos, bool on)
        {
            var go = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 50);
            go.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.32f);
            var t = go.GetComponent<Toggle>();
            t.isOn = on;
            CreateLabel(go.transform, label + (on ? " On" : " Off"), 24, Vector2.zero, rt.sizeDelta);
            return t;
        }
    }
}
