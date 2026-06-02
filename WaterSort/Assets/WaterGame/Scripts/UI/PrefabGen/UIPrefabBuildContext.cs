using UnityEngine;
using UnityEngine.UI;

namespace AsGame.UI.PrefabGen
{
    /// <summary>Editor 生成 Prefab 时的上下文（不依赖 Play 模式）。</summary>
    public sealed class UIPrefabBuildContext
    {
        public RectTransform Root { get; }
        public RectTransform Content { get; }
        public CanvasGroup Blocker { get; }
        public MonoBehaviour View { get; }

        public UIPrefabBuildContext(RectTransform root, RectTransform content, CanvasGroup blocker, MonoBehaviour view)
        {
            Root = root;
            Content = content;
            Blocker = blocker;
            View = view;
        }

        public Text CreateLabel(string name, string text, int fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(Content, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return t;
        }

        public Button CreateButton(string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(Content, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.95f);
            var txt = CreateLabel(name + "_Label", label, 26, Vector2.zero, size);
            txt.rectTransform.SetParent(go.transform, false);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = txt.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        public Toggle CreateToggle(string name, string label, Vector2 pos, bool isOn)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle), typeof(Image));
            go.transform.SetParent(Content, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 50);
            go.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.32f);
            var t = go.GetComponent<Toggle>();
            t.isOn = isOn;
            CreateLabel(name + "_Label", label + (isOn ? " On" : " Off"), 24, Vector2.zero, rt.sizeDelta)
                .transform.SetParent(go.transform, false);
            return t;
        }

        /// <summary>按路径查找或创建子节点，便于 [SerializeField] 绑定。</summary>
        public RectTransform GetOrCreatePath(string path)
        {
            var parts = path.Split('/');
            Transform current = Content;
            foreach (var part in parts)
            {
                var child = current.Find(part);
                if (child == null)
                {
                    var go = new GameObject(part, typeof(RectTransform));
                    go.transform.SetParent(current, false);
                    child = go.transform;
                }

                current = child;
            }

            return (RectTransform)current;
        }
    }
}
