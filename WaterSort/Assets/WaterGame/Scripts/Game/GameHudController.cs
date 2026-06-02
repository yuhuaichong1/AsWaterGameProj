using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Data;
using AsGame.Events;
using AsGame.UI;

namespace AsGame.Water
{
    /// <summary>Gameplay HUD on GameplayCanvas prefab. Sprites/text bound at runtime from Resources.</summary>
    public class GameHudController : MonoBehaviour
    {
        [SerializeField] Text levelLabel;
        [SerializeField] PropButtonView shuffle;
        [SerializeField] PropButtonView undo;
        [SerializeField] PropButtonView addBottle;
        [SerializeField] GameObject shuffleTip;
        [SerializeField] Button btnSetting;
        [SerializeField] Button btnShuffleTipDone;

        System.Action<object> _onUpdateProp;
        System.Action<object> _onUpdateHeart;

        void Awake() => BindTopHud();

        void Start()
        {
            BindTopHud();

            btnSetting?.onClick.AddListener(() =>
                PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.Setting }));
            btnShuffleTipDone?.onClick.AddListener(HideShuffleTip);

            _onUpdateProp = _ => RefreshProps();
            _onUpdateHeart = _ => RefreshHeart();
            EventBus.Subscribe(GameEvents.UpdateProp, _onUpdateProp);
            EventBus.Subscribe(GameEvents.UpdateHeart, _onUpdateHeart);

            RefreshLevel();
            RefreshProps();
        }

        void BindTopHud()
        {
            ResolveReferences();
            BindLevelBanner();
            BindSettingsButton();
        }

        void ResolveReferences()
        {
            if (levelLabel == null)
                levelLabel = transform.Find("LevelBanner/LevelLabel")?.GetComponent<Text>();

            if (btnSetting == null)
                btnSetting = transform.Find("Btn_Settings")?.GetComponent<Button>();
        }

        void BindLevelBanner()
        {
            var banner = transform.Find("LevelBanner")?.GetComponent<Image>();
            if (banner != null)
            {
                var sp = GameResourceLoader.LoadSprite("Sprites/UI/img_01")
                       ?? GameResourceLoader.LoadSprite("Sprites/UI/frame_prop")
                       ?? GameResourceLoader.LoadSprite("Sprites/UI/panel_bg");
                if (sp != null)
                {
                    banner.sprite = sp;
                    banner.type = sp.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                    banner.color = Color.white;
                    banner.enabled = true;
                }
                else
                {
                    banner.sprite = null;
                    banner.color = new Color(0.72f, 0.55f, 0.38f, 0.95f);
                }
            }

            if (levelLabel != null)
                ApplyLevelLabelStyle(levelLabel);
        }

        public static void ApplyLevelLabelStyle(Text label)
        {
            label.color = new Color(1f, 0.98f, 0.92f, 1f);
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            var outline = label.GetComponent<Outline>() ?? label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.05f, 0.05f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        void BindSettingsButton()
        {
            var btnTf = transform.Find("Btn_Settings");
            if (btnTf == null) return;

            var img = btnTf.GetComponent<Image>();
            if (img != null)
            {
                var iconSp = GameResourceLoader.LoadSprite("Sprites/UI/icon_2")
                             ?? GameResourceLoader.LoadSprite("Sprites/UI/btn_icon");
                if (iconSp != null)
                {
                    img.sprite = iconSp;
                    img.color = Color.white;
                    img.preserveAspect = true;
                    img.SetNativeSize();
                }
                else
                {
                    img.sprite = null;
                    img.color = new Color(0.35f, 0.55f, 0.85f, 1f);
                }
            }

            var wordTf = btnTf.Find("Word");
            if (wordTf == null)
            {
                var wordGo = new GameObject("Word", typeof(RectTransform), typeof(Image));
                wordGo.transform.SetParent(btnTf, false);
                wordTf = wordGo.transform;
                var wordRt = wordGo.GetComponent<RectTransform>();
                wordRt.anchorMin = wordRt.anchorMax = new Vector2(0.5f, 0.5f);
                wordRt.anchoredPosition = new Vector2(0f, -46f);
                wordRt.sizeDelta = new Vector2(63f, 37f);
            }

            var wordImg = wordTf.GetComponent<Image>() ?? wordTf.gameObject.AddComponent<Image>();
            wordImg.raycastTarget = false;
            var wordSp = GameResourceLoader.LoadSprite("Sprites/UI/word_3");
            if (wordSp != null)
            {
                wordImg.sprite = wordSp;
                wordImg.color = Color.white;
                wordImg.SetNativeSize();
            }
            else
                wordImg.enabled = false;
        }

        void OnDestroy()
        {
            if (_onUpdateProp != null) EventBus.Unsubscribe(GameEvents.UpdateProp, _onUpdateProp);
            if (_onUpdateHeart != null) EventBus.Unsubscribe(GameEvents.UpdateHeart, _onUpdateHeart);
        }

        public void RefreshLevel()
        {
            if (levelLabel != null)
                levelLabel.text = "第 " + GameSaveData.CurrentLevel + " 关";
        }

        public void RefreshHeart() { }

        public void RefreshProps()
        {
            shuffle?.Refresh();
            undo?.Refresh();
            addBottle?.Refresh();
        }

        public void SetUndoGray(bool gray) => undo?.SetGray(gray);
        public void SetAddBottleGray(bool gray) => addBottle?.SetGray(gray);
        public void SetShuffleGray(bool gray) => shuffle?.SetGray(gray);
        public void ShowShuffleTip() => shuffleTip?.SetActive(true);
        public void HideShuffleTip() => shuffleTip?.SetActive(false);
    }
}
