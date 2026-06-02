using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Spine;
using AsGame.UI;

namespace AsGame.Water
{
    public class PocketController : MonoBehaviour
    {
        [SerializeField] Image pocketImage;
        [SerializeField] GameObject lockOverlay;
        GameObject _unlockButton;

        int _colorId;
        bool _locked;
        Action<PocketController> _onUnlock;

        public int PackColorId => _colorId;
        public bool IsLocked => _locked;

        public void Init(bool locked, int colorId, Action<PocketController> onUnlock = null)
        {
            _locked = locked;
            _colorId = colorId;
            _onUnlock = onUnlock;
            if (lockOverlay != null) lockOverlay.SetActive(locked);
            if (_unlockButton != null) _unlockButton.SetActive(locked);
            SetColor(colorId);
            if (pocketImage != null)
                StartCoroutine(FadeInPocket());
        }

        IEnumerator FadeInPocket()
        {
            var c = pocketImage.color;
            c.a = 0f;
            pocketImage.color = c;
            yield return TweenHelper.ToFloat(0f, 1f, 0.3f, a =>
            {
                if (pocketImage == null) return;
                var col = pocketImage.color;
                col.a = a;
                pocketImage.color = col;
            });
        }

        public void SetColor(int colorId)
        {
            _colorId = colorId;
            if (pocketImage == null) return;

            if (_locked)
            {
                ApplyPocketSprite("Sprites/Pocket/kong");
                return;
            }

            if (colorId <= 0)
            {
                pocketImage.sprite = null;
                pocketImage.color = new Color(1f, 1f, 1f, 0.25f);
                return;
            }

            var sprite = GameResourceLoader.LoadSprite("Sprites/Pocket/" + colorId + "_1");
            if (sprite != null)
                ApplyPocketSprite("Sprites/Pocket/" + colorId + "_1");
            else if (GameConstants.GameColorData.TryGetValue(colorId, out var pair))
            {
                pocketImage.sprite = null;
                pocketImage.color = pair.Base;
            }
        }

        void ApplyPocketSprite(string path)
        {
            var sprite = GameResourceLoader.LoadSprite(path);
            if (sprite == null) return;
            pocketImage.sprite = sprite;
            pocketImage.color = Color.white;
            pocketImage.SetNativeSize();
        }

        public IEnumerator OnPocketAction()
        {
            SpineService.PlayEffect(transform, Vector3.zero, "dai_zi", "zhuang");
            var rt = transform as RectTransform;
            if (rt == null) yield break;
            var start = rt.localScale;
            yield return TweenHelper.ToFloat(0f, 1f, 0.25f, t =>
            {
                var scale = Mathf.Lerp(1f, 1.15f, Mathf.Sin(t * Mathf.PI));
                rt.localScale = start * scale;
            });
            rt.localScale = start;
        }

        public void ConfigureLockedState(bool locked, Action<PocketController> onUnlock)
        {
            _locked = locked;
            _onUnlock = onUnlock;
            if (lockOverlay != null) lockOverlay.SetActive(locked);
            if (_unlockButton != null) _unlockButton.SetActive(locked);
        }

        public static PocketController Create(Transform parent, bool locked, Action<PocketController> onUnlock = null)
        {
            var prefab = Resources.Load<GameObject>(PrefabPaths.Pocket);
            if (prefab != null)
            {
                var go = UnityEngine.Object.Instantiate(prefab, parent);
                go.name = "Pocket";
                var ctrl = go.GetComponent<PocketController>();
                ctrl.ConfigureLockedState(locked, onUnlock);
                ctrl.Init(locked, 0, onUnlock);
                return ctrl;
            }

            return CreateLegacy(parent, locked, onUnlock);
        }

        public static PocketController CreateLegacy(Transform parent, bool locked, Action<PocketController> onUnlock = null)
        {
            var go = new GameObject("Pocket", typeof(RectTransform), typeof(PocketController));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            // 对齐 Cocos Pocket.prefab：锚点底部居中，105×188。
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(105f, 188f);

            var imgGo = new GameObject("img", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(go.transform, false);
            var imgRt = imgGo.GetComponent<RectTransform>();
            imgRt.anchorMin = imgRt.anchorMax = new Vector2(0.5f, 0f);
            imgRt.pivot = new Vector2(0.5f, 0f);
            imgRt.anchoredPosition = Vector2.zero;
            var img = imgGo.GetComponent<Image>();
            var ctrl = go.GetComponent<PocketController>();
            ctrl.pocketImage = img;

            if (locked)
            {
                var lockGo = new GameObject("lock", typeof(RectTransform), typeof(Image));
                lockGo.transform.SetParent(go.transform, false);
                var lockRt = lockGo.GetComponent<RectTransform>();
                lockRt.anchorMin = lockRt.anchorMax = new Vector2(0.5f, 0f);
                lockRt.pivot = new Vector2(0.5f, 0f);
                lockRt.anchoredPosition = new Vector2(0f, 20f);
                var lockImg = lockGo.GetComponent<Image>();
                var frame = GameResourceLoader.LoadSprite("Sprites/Bottle/frame_jd");
                if (frame != null)
                {
                    lockImg.sprite = frame;
                    lockImg.SetNativeSize();
                }
                ctrl.lockOverlay = lockGo;
            }

            if (locked)
            {
                var unlockBtn = UIFactory.CreateButton(go.transform, "Unlock", new Vector2(0f, 30f), new Vector2(100f, 40f),
                    "Btn_Unlock");
                unlockBtn.onClick.AddListener(() => ctrl.RequestUnlock());
                ctrl._unlockButton = unlockBtn.gameObject;
            }

            ctrl.Init(locked, 0, onUnlock);
            return ctrl;
        }

        public void RequestUnlock() => _onUnlock?.Invoke(this);
    }
}
