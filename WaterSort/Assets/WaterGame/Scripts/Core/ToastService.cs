using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using XrCode;

namespace AsGame.Core
{
    public class ToastService : MonoBehaviour
    {
        public static ToastService Instance { get; private set; }

        Text _label;
        CanvasGroup _cg;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            Ensure();
            if (Instance == null) return;
            Instance.StartCoroutine(Instance.ShowRoutine(message));
        }

        static void Ensure()
        {
            if (Instance != null) return;

            var go = new GameObject("ToastService", typeof(ToastService));
            var svc = go.GetComponent<ToastService>();
            // Awake 已设置 Instance

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(750, 1334);
            go.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(go.transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.3f);
            rt.sizeDelta = new Vector2(520, 72);
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            svc._cg = panel.GetComponent<CanvasGroup>();
            svc._cg.alpha = 0f;

            var txt = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txt.transform.SetParent(panel.transform, false);
            svc._label = txt.GetComponent<Text>();
            svc._label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            svc._label.fontSize = 26;
            svc._label.color = Color.white;
            svc._label.alignment = TextAnchor.MiddleCenter;
            svc._label.raycastTarget = false;
            var trt = txt.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        IEnumerator ShowRoutine(string msg)
        {
            if (_label == null || _cg == null) yield break;
            _label.text = msg;
            _cg.alpha = 1f;
            yield return new WaitForSeconds(1.8f);
            yield return TweenHelper.FadeCanvasGroup(_cg, 0f, 0.2f);
        }
    }
}
