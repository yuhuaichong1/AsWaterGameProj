using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class WithdrawFeedbackView : BaseView
    {
        private const int Stage3Id = 3;

        private Text titleText;
        private Button generateMoreBtn;
        private Button closeBtn;
        private Button continueBtn;

        private bool isReferencesResolved;
        private bool isEventsBound;

        private void Awake()
        {
            ResolveReferencesIfNeeded();
        }

        public override void InitView()
        {
            ResolveReferencesIfNeeded();
            BindEvents();
            ApplyLocalizedTexts();
            RefreshGameplayWithdrawPrompt();
        }

        private void OnDestroy()
        {
            RemoveEvents();
        }

        private void ResolveReferencesIfNeeded()
        {
            if (isReferencesResolved)
                return;

            titleText = FindByPath<Text>("RewardMessage/Panel/Top/Title");
            generateMoreBtn = FindByPath<Button>("RewardMessage/Panel/Bottom/GenerateMoreBtn");
            closeBtn = FindByPath<Button>("RewardMessage/Panel/bg/close");
            continueBtn = FindByPath<Button>("RewardMessage/Panel/continuebtn");

            isReferencesResolved = titleText != null
                || generateMoreBtn != null
                || closeBtn != null
                || continueBtn != null;
        }

        private T FindByPath<T>(string path) where T : Component
        {
            GameObject target = FindChildUtility.FindChildByPath(gameObject, path);
            return target != null ? target.GetComponent<T>() : null;
        }

        private void BindEvents()
        {
            if (isEventsBound)
                return;

            generateMoreBtn?.onClick.AddListener(OnGenerateMoreClick);
            closeBtn?.onClick.AddListener(OnCloseClick);
            continueBtn?.onClick.AddListener(OnCloseClick);
            isEventsBound = true;
        }

        private void RemoveEvents()
        {
            if (!isEventsBound)
                return;

            generateMoreBtn?.onClick.RemoveListener(OnGenerateMoreClick);
            closeBtn?.onClick.RemoveListener(OnCloseClick);
            continueBtn?.onClick.RemoveListener(OnCloseClick);
            isEventsBound = false;
        }

        private void ApplyLocalizedTexts()
        {
            if (titleText != null)
            {
                LocalizedText localizedText = titleText.GetComponent<LocalizedText>();
                if (localizedText != null)
                {
                    localizedText.SetTextId("3046");
                }
                else
                {
                    titleText.text = GetText("3046", titleText.text);
                }
            }
        }

        private void OnGenerateMoreClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            CloseAndAdvanceToStage4Slot();
        }

        private void OnCloseClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            CloseAndAdvanceToStage4Slot();
        }

        private void CloseAndAdvanceToStage4Slot()
        {
            // 必须走 UIManager.CloseView，避免 HideView 动画期间仍被判定为可见而拦 Slot。
            UIManager.instance?.CloseView(EUIType.WithdrawFeedbackView);
            GameManagerWZ.instance?.CompleteStage3FeedbackAndShowSlot();
        }

        private void RefreshGameplayWithdrawPrompt()
        {
            if (GameDefines.ifIAA)
                return;

            // 宿主 UIGamePlay Stage3Root。
            XrCode.FacadeGamePlay.RefreshWzStageHud?.Invoke();

            if (!WaterSortWZBridge.UsesWzGameplayHud || UIManager.instance == null)
                return;

            UIManager.instance.GetView<GamePlayerView>(EUIType.GamePlayerView)?.UpdateWithdrawPrompt();
        }

        private string GetText(string key, string fallback)
        {
            if (LocalizationManager.Instance == null)
                return fallback;

            string value = LocalizationManager.Instance.GetText(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        public override void Start()
        {
            
        }

        public override void Update()
        {
         
        }
    }
}
