using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class TargetWithdrawView : BaseView
    {
        private const int Stage4Id = 4;
        private const float RefreshInterval = 0.2f;

        private Text curCoinText;
        private Text titleText;
        private Text progressText;
        private Image progressImage;
        private Button generateMoreBtn;
        private Button close;

        private bool isInitialized;
        private bool isEventsBound;
        private int targetStageId;
        private float currentTargetCoin;
        private float currentRemainingCoin;
        private float nextRefreshTime;

        private void Awake()
        {
            ResolveReferencesIfNeeded();
        }

        public override void InitView()
        {
            ResolveReferencesIfNeeded();
            BindEvents();
            RefreshView();
        }

        public override void Start()
        {
            ResolveReferencesIfNeeded();
            RefreshView();
        }

        public override void Update()
        {
            if (!isInitialized || Time.unscaledTime < nextRefreshTime)
                return;

            RefreshView();
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
        }

        private void OnDestroy()
        {
            RemoveEvents();
        }

        protected override void HandleViewArgs(object[] args)
        {
            targetStageId = GetStageId(args);
        }

        private void ResolveReferencesIfNeeded()
        {
            if (isInitialized)
                return;

            curCoinText = FindByPath<Text>("RewardMessage/Panel/Top/CurCoin", "CurCoin");
            titleText = FindByPath<Text>("RewardMessage/Panel/Top/Title", "Title");
            progressText = FindByPath<Text>("RewardMessage/Panel/Top/Progress/ProgressTxt", "ProgressTxt");
            progressImage = FindByPath<Image>("RewardMessage/Panel/Top/Progress", "Progress");
            generateMoreBtn = FindByPath<Button>("RewardMessage/Panel/Bottom/GenerateMoreBtn", "GenerateMoreBtn");
            close = FindByPath<Button>("RewardMessage/Panel/close", "close");

            isInitialized = curCoinText != null || titleText != null || progressText != null || progressImage != null || generateMoreBtn != null;
        }

        private T FindByPath<T>(string path, string fallbackName) where T : Component
        {
            GameObject target = FindChildUtility.FindChildByPath(gameObject, path);
            if (target == null)
                target = FindChildUtility.FindChild(gameObject, fallbackName);

            return target != null ? target.GetComponent<T>() : null;
        }

        private void BindEvents()
        {
            if (isEventsBound)
                return;

            generateMoreBtn?.onClick.AddListener(OnGenerateMoreClick);
            close?.onClick.AddListener(OnGenerateMoreClick);
            isEventsBound = true;
        }

        private void RemoveEvents()
        {
            if (!isEventsBound)
                return;

            generateMoreBtn?.onClick.RemoveListener(OnGenerateMoreClick);
            isEventsBound = false;
        }

        private void RefreshView()
        {
            var gm = GameManagerWZ.instance;
            if (gm == null)
                return;

            int stageId = targetStageId > 0 ? targetStageId : Mathf.Max(gm.currentStage, 1);
            targetStageId = stageId;

            if (!TryGetCoinStageTarget(gm, stageId, out float stageTargetCoin))
            {
                RefreshFallbackState(gm);
                return;
            }

            float currentCoin = Mathf.Max(gm.currentCoin, 0f);
            currentTargetCoin = stageTargetCoin;
            currentRemainingCoin = Mathf.Max(currentTargetCoin - currentCoin, 0f);

            if (titleText != null)
            {
                titleText.text = string.Format(
                    GetText("2100", "Earn ${0} more withdraw ${1}"),
                    FormatPlainCoin(currentRemainingCoin),
                    FormatPlainCoin(currentTargetCoin));
            }

            if (curCoinText != null)
                curCoinText.text = $"{FormatCoin(currentCoin)}";

            if (progressText != null)
                progressText.text = $"{FormatCoin(currentCoin)}/{FormatCoin(currentTargetCoin)}";

            if (progressImage != null)
                progressImage.fillAmount = Mathf.Clamp01(currentCoin / Mathf.Max(currentTargetCoin, 1f));

            SetGenerateMoreState(stageId == Stage4Id && currentRemainingCoin > 0f);
        }

        private void RefreshFallbackState(GameManagerWZ gm)
        {
            currentTargetCoin = 0f;
            currentRemainingCoin = 0f;

            if (titleText != null)
                titleText.text = GetText("2149", "Task completed");

            if (curCoinText != null)
                curCoinText.text = $"{FormatCoin(gm.currentCoin)}";

            if (progressText != null)
                progressText.text = "0/0";

            if (progressImage != null)
                progressImage.fillAmount = 0f;

            SetGenerateMoreState(false);
        }

        private void SetGenerateMoreState(bool isVisible)
        {
            if (generateMoreBtn == null)
                return;

            generateMoreBtn.gameObject.SetActive(isVisible);
            generateMoreBtn.interactable = isVisible;
        }

        private void OnGenerateMoreClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();

            if (targetStageId == Stage4Id)
            {
                //float slotRewardValue = Mathf.Max(currentRemainingCoin, 0f);
                CloseCurrentView();
                UIManager.instance.ShowView(EUIType.SlotView, 50);
                return;
            }

            CloseCurrentView();
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

        private bool TryGetCoinStageTarget(GameManagerWZ gm, int stageId, out float stageTargetCoin)
        {
            stageTargetCoin = 0f;

            if (gm == null
                || !gm.TryGetStageRequirement(stageId, out int stageType, out int stageTarget)
                || stageType != 2
                || stageTarget <= 0)
            {
                return false;
            }

            stageTargetCoin = stageTarget;
            return true;
        }

        private int GetStageId(object[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (args[0] is int intStageId && intStageId > 0)
                    return intStageId;

                if (args[0] is string stringStageId && int.TryParse(stringStageId, out int parsedStageId) && parsedStageId > 0)
                    return parsedStageId;
            }

            return GameManagerWZ.instance != null ? Mathf.Max(GameManagerWZ.instance.currentStage, 1) : 1;
        }

        private string FormatCoin(float coin)
        {
            if (FacadePayTypeExtend.RegionalChangeHandle != null)
                return FacadePayTypeExtend.RegionalChangeHandle(coin);

            return $"{GetCurrencyMark()}{FormatPlainCoin(coin)}";
        }

        private string FormatPlainCoin(float coin)
        {
            int decimals = GameManagerWZ.instance != null ? Mathf.Max(GameManagerWZ.instance.decimals, 0) : 2;
            string formatted = coin.ToString($"F{decimals}");
            return formatted.TrimEnd('0').TrimEnd('.');
        }

        private string GetCurrencyMark()
        {
            return GameManagerWZ.instance != null ? GameManagerWZ.instance.mark : "$";
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
