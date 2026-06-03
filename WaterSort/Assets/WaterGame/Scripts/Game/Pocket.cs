using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Spine;
using AsGame.UI;
using XrCode;
using Spine.Unity;

namespace AsGame.Water
{
    public class Pocket : MonoBehaviour
    {
        [SerializeField] private Image pocketImage;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Button _unlockButton;
        [SerializeField] private SkeletonGraphic bao_xingEffect;
        [SerializeField] private SkeletonGraphic dai_ziEffect;

        int _colorId;
        bool _locked;
        Action<Pocket> _onUnlock;
        Vector3 _orgLocalPos;

        public int PackColorId => _colorId;
        public bool IsLocked => _locked;

        public void Init(bool locked, int colorId, Action<Pocket> onUnlock = null)
        {
            _orgLocalPos = transform.localPosition;
            _locked = locked;
            _colorId = colorId;
            _onUnlock = onUnlock;
            if (lockOverlay != null) lockOverlay.SetActive(locked);
            if (_unlockButton != null)
            {
                _unlockButton.gameObject.SetActive(locked);
                _unlockButton.onClick.RemoveAllListeners();
                _unlockButton.onClick.AddListener(() => { _onUnlock?.Invoke(this); });
            }
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
            RestorePocketView();
            if (pocketImage == null) return;

            _unlockButton.gameObject.SetActive(_locked);

            if (_locked)
            {
                ApplyPocketSprite("UI/Pocket/kong.png");
                return;
            }

            if (colorId <= 0)
            {
                pocketImage.sprite = null;
                pocketImage.color = new Color(1f, 1f, 1f, 0.25f);
                return;
            }

            Sprite sprite = ResourceMod.Instance.SyncLoad<Sprite>($"UI/Pocket/{colorId}_1.png");
            ApplyPocketSprite($"UI/Pocket/{colorId}_1.png");

            //var sprite = GameResourceLoader.LoadSprite("Sprites/Pocket/" + colorId + "_1");
            //if (sprite != null)
            //    ApplyPocketSprite($"UI/Pocket/{colorId}_1.png");
            //else if (GameConstants.GameColorData.TryGetValue(colorId, out var pair))
            //{
            //    pocketImage.sprite = null;
            //    pocketImage.color = pair.Base;
            //}
        }

        void ApplyPocketSprite(string path)
        {
            //var sprite = GameResourceLoader.LoadSprite(path);
            Sprite sprite = ResourceMod.Instance.SyncLoad<Sprite>(path);
            if (sprite == null) return;
            pocketImage.sprite = sprite;
            pocketImage.color = Color.white;
            pocketImage.SetNativeSize();
        }

        /// <summary>对齐 Cocos PocketComp.onPocketAction：星星爆开 → 装袋 → 口袋上飞。</summary>
        public IEnumerator OnPocketAction(int packColorId = 0)
        {
            Debug.LogError("/");

            if (packColorId <= 0)
                yield break;

            AudioManager.Instance?.PlaySfx("PackUp");
            SpineService.ClearEffects(transform);

            var fxPos = GetPocketFxLocalPos(packColorId);
            var baoDone = false;
            //SpineService.PlayEffect(transform, fxPos, "bao_xing", "bao", loop: false, onComplete: () => baoDone = true);
            PlayFinishEffect1();

            var wait = 0f;
            while (!baoDone && wait < 0.5f)
            {
                wait += Time.deltaTime;
                yield return null;
            }

            //yield return new WaitForSeconds(0.1f);

            if (pocketImage != null)
            {
                var c = pocketImage.color;
                c.a = 0f;
                pocketImage.color = c;
            }

            GameConstants.GamePocketSpineSkin.TryGetValue(packColorId, out var skinName);
            var daiDone = false;
            //SpineService.PlayEffect(transform, fxPos, "dai_zi", "zhuang", loop: false, onComplete: () => daiDone = true, skinName: skinName);
            PlayFinishEffect2();

            wait = 0f;
            while (!daiDone && wait < 2.5f)
            {
                wait += Time.deltaTime;
                yield return null;
            }

            SpineService.ClearEffects(transform);

            var rt = transform as RectTransform;
            if (rt != null)
            {
                var startY = rt.localPosition.y;
                yield return TweenHelper.ToFloat(0f, 1f, 0.3f, t =>
                {
                    var eased = t * t;
                    var pos = rt.localPosition;
                    pos.y = Mathf.Lerp(startY, startY + 1000f, eased);
                    rt.localPosition = pos;
                });
            }
        }

        void RestorePocketView()
        {
            transform.localPosition = _orgLocalPos;
            if (pocketImage == null) return;
            var c = pocketImage.color;
            c.a = 1f;
            pocketImage.color = c;
        }

        static Vector3 GetPocketFxLocalPos(int colorId)
        {
            if (GameConstants.GamePocketSpinePos.TryGetValue(colorId, out var offset))
                return new Vector3(offset.x, offset.y, 0f);
            return Vector3.zero;
        }

        public void ConfigureLockedState(bool locked, Action<Pocket> onUnlock)
        {
            _locked = locked;
            _onUnlock = onUnlock;
            if (lockOverlay != null) lockOverlay.SetActive(locked);
            if (_unlockButton != null) _unlockButton.gameObject.SetActive(locked);
        }

        public static Pocket Create(Transform parent, bool locked, Action<Pocket> onUnlock = null)
        {
            //var prefab = Resources.Load<GameObject>(PrefabPaths.Pocket);
            GameObject prefab = ResourceMod.Instance.SyncLoad<GameObject>(GameDefines.PocketPath);

            if (prefab != null)
            {
                var go = UnityEngine.Object.Instantiate(prefab, parent);
                go.name = "Pocket";
                var ctrl = go.GetComponent<Pocket>();
                ctrl.ConfigureLockedState(locked, onUnlock);
                //ctrl.Init(locked, 0, onUnlock);
                return ctrl;
            }

            return CreateLegacy(parent, locked, onUnlock);
        }

        public static Pocket CreateLegacy(Transform parent, bool locked, Action<Pocket> onUnlock = null)
        {
            var go = new GameObject("Pocket", typeof(RectTransform), typeof(Pocket));
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
            var ctrl = go.GetComponent<Pocket>();
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
                ctrl._unlockButton = unlockBtn;
            }

            ctrl.Init(locked, 0, onUnlock);
            return ctrl;
        }

        private void PlayFinishEffect1()
        {
            bao_xingEffect.gameObject.SetActive(true);
            bao_xingEffect.AnimationState.SetAnimation(0, "bao", false).Complete += (Entry) => { bao_xingEffect.gameObject.SetActive(false); };
            
        }

        private void PlayFinishEffect2()
        {
            dai_ziEffect.gameObject.SetActive(true);
            dai_ziEffect.Skeleton.SetSkin(GameDefines.dai_ziName[_colorId]);
            dai_ziEffect.Skeleton.SetSlotsToSetupPose();
            dai_ziEffect.AnimationState.SetAnimation(0, "zhuang", false).Complete += (Entry) => { dai_ziEffect.gameObject.SetActive(false); };
        }

        public void RequestUnlock() => _onUnlock?.Invoke(this);
    }
}
