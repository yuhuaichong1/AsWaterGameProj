using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class ProgressView : BaseView
    {
        private const int Stage4Id = 4;
        private const string DescriptionTextId = "3095";
        private const float ProgressAnimationSpeed = 0.4f;


        private Image progress;
        private Text countText;
        private Text decText;
        private RectTransform progressRoot;
        private Button closeBtn;
        private Button continueBtn;

        private bool isReferencesResolved;
        private bool isEventsBound;
        private bool isClosing;
        private float openRewardAmount;
        private float targetCoinValue;
        private float displayedProgressValue = -1f;
        private float targetProgressValue;

        private void Awake()
        {
            ResolveReferencesIfNeeded();
        }

        public override void InitView()
        {
            ResolveReferencesIfNeeded();
            BindEvents();
            InitializeProgressState();
        }

        public override void Start()
        {
        }

        public override void Update()
        {
            UpdateProgressAnimation();
        }

        private void OnDestroy()
        {
            RemoveEvents();
            StopAllCoroutines();
        }

        protected override void HandleViewArgs(object[] args)
        {
            openRewardAmount = 0f;

            if (args == null)
                return;

            foreach (object arg in args)
            {
                if (arg is float floatValue)
                    openRewardAmount = Mathf.Max(floatValue, 0f);
                else if (arg is int intValue)
                    openRewardAmount = Mathf.Max(intValue, 0);
                else if (arg is double doubleValue)
                    openRewardAmount = Mathf.Max((float)doubleValue, 0f);
            }
        }

        private void ResolveReferencesIfNeeded()
        {
            if (isReferencesResolved)
                return;

            progress = FindComponent<Image>("Progress");
            countText = FindComponent<Text>("Count");
            decText = FindComponent<Text>("dec");
            progressRoot = FindComponent<RectTransform>("ProgressRoot");
            closeBtn = FindComponent<Button>("close");
            continueBtn = FindComponent<Button>("continuebtn");

            isReferencesResolved = true;
        }

        private T FindComponent<T>(string childName) where T : Component
        {
            GameObject target = FindChildUtility.FindChild(gameObject, childName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private void BindEvents()
        {
            if (isEventsBound)
                return;

            closeBtn?.onClick.AddListener(OnCloseClick);
            continueBtn?.onClick.AddListener(OnCloseClick);
            isEventsBound = true;
        }

        private void RemoveEvents()
        {
            if (!isEventsBound)
                return;

            closeBtn?.onClick.RemoveListener(OnCloseClick);
            continueBtn?.onClick.RemoveListener(OnCloseClick);
            isEventsBound = false;
        }

        private void InitializeProgressState()
        {
            if (progressRoot != null && !progressRoot.gameObject.activeSelf)
                progressRoot.gameObject.SetActive(true);

            isClosing = false;

            GameManagerWZ gm = GameManagerWZ.instance;
            targetCoinValue = GetTargetCoin(gm);

            float currentCoin = gm != null ? Mathf.Max(gm.currentCoin, 0f) : 0f;
            float startCoin = Mathf.Max(currentCoin - openRewardAmount, 0f);
            float safeTargetCoin = Mathf.Max(targetCoinValue, 1f);

            displayedProgressValue = Mathf.Clamp01((openRewardAmount > 0f ? startCoin : currentCoin) / safeTargetCoin);
            targetProgressValue = Mathf.Clamp01(currentCoin / safeTargetCoin);

            ApplyProgressVisualState(displayedProgressValue);
            ApplyStaticTexts(currentCoin, targetCoinValue);
            openRewardAmount = 0f;
        }

        private void UpdateProgressAnimation()
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            float currentCoin = gm != null ? Mathf.Max(gm.currentCoin, 0f) : 0f;
            targetCoinValue = GetTargetCoin(gm);
            float safeTargetCoin = Mathf.Max(targetCoinValue, 1f);
            targetProgressValue = Mathf.Clamp01(currentCoin / safeTargetCoin);

            if (displayedProgressValue < 0f)
            {
                displayedProgressValue = targetProgressValue;
                ApplyProgressVisualState(displayedProgressValue);
                ApplyStaticTexts(currentCoin, targetCoinValue);
                return;
            }

            displayedProgressValue = Mathf.MoveTowards(
                displayedProgressValue,
                targetProgressValue,
                ProgressAnimationSpeed * Time.unscaledDeltaTime);

            ApplyProgressVisualState(displayedProgressValue);
            ApplyStaticTexts(currentCoin, targetCoinValue);
        }

        private void ApplyProgressVisualState(float progressValue)
        {
            if (progress != null)
                progress.fillAmount = Mathf.Clamp01(progressValue);
        }

        private void ApplyStaticTexts(float currentCoin, float targetCoin)
        {
            float safeCurrentCoin = Mathf.Max(currentCoin, 0f);
            float safeTargetCoin = Mathf.Max(targetCoin, 1f);

            if (countText != null)
                countText.text = $"{FormatCoin(safeCurrentCoin)}/{FormatCoin(safeTargetCoin)}";

            if (decText != null)
            {
                string template = GetText(DescriptionTextId, "");
                decText.text = string.Format(
                    template,
                    FormatPlainCoin(safeCurrentCoin),
                    FormatPlainCoin(safeTargetCoin));
            }
        }

        private float GetTargetCoin(GameManagerWZ gm)
        {
            if (gm != null
                && gm.TryGetStageRequirement(Stage4Id, out int stageType, out int stageTarget)
                && stageType == 2
                && stageTarget > 0)
            {
                return stageTarget;
            }

            return 1f;
        }

        private string FormatCoin(float coin)
        {
            if (FacadePayTypeExtend.RegionalChangeHandle != null)
                return FacadePayTypeExtend.RegionalChangeHandle(coin);

            return $"{GetCurrencyMark()}{FormatPlainCoin(coin)}";
        }

        private string FormatPlainCoin(float coin)
        {
            GameManagerWZ gm = GameManagerWZ.instance;
            int decimals = gm != null ? Mathf.Max(gm.decimals, 0) : 2;
            string formatted = Mathf.Max(coin, 0f).ToString($"F{decimals}");
            return formatted.TrimEnd('0').TrimEnd('.');
        }

        private string GetCurrencyMark()
        {
            return GameManagerWZ.instance != null ? GameManagerWZ.instance.mark : "$";
        }

        private void OnCloseClick()
        {
            if (isClosing)
                return;

            isClosing = true;
            SoundManager.Instance?.PlayUIClickSFX();
            CloseCurrentView();
            StartCoroutine(CompleteStage4SlotFlowAfterClose());
        }

        private void CloseCurrentView()
        {
            if (canvasGroup != null)
            {
                HideView();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator CompleteStage4SlotFlowAfterClose()
        {
            yield return new WaitForSecondsRealtime(GameDefines.ShowAnimTime);
            GameManagerWZ.instance?.CompleteStage4SlotFlow();
        }

        private string GetText(string key, string fallback)
        {
            if (LocalizationManager.Instance == null)
                return fallback;

            string value = LocalizationManager.Instance.GetText(key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
