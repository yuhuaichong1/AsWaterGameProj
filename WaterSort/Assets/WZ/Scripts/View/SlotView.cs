using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class SlotView : BaseView
    {
        private const int DefaultPanelIndex = 1;
        private const float DefaultClosedDisplayValue = 0f;

        public LotteryPanel LotteryPanel1;
        public Button DrawButton;
        public Button CloseButton;

        [Header("Panel 1 Reward")]
        [SerializeField] private ERewardType lotteryPanel1RewardType = ERewardType.Money;
        [SerializeField] private float lotteryPanel1RewardAmount = 100f;
        [SerializeField] private float rewardPopupDelay = 1f;



        private int currentPanelIndex = 1;
        private LotteryPanel currentPanel;
        private bool isRolling;
        private bool hasRewardClaimed;
        private bool buttonsBound;
        private Coroutine rewardDelayCoroutine;
        private float currentLotteryPanel1RewardAmount;
        private bool hasPendingOpenConfig;
        private System.Action rewardEffectFinishAction;

        public override void InitView()
        {
            PrepareOpenConfig();
            ResetState();
            BindButtons();
            BindPanelCallbacks();
            ApplyPanelState();
            RefreshButtons();
        }

        public override void Start()
        {
        }

        public override void Update()
        {
        }

        private void OnDestroy()
        {
            StopRewardDelay();
            UnbindButtons();
            UnbindPanelCallbacks();
        }

        protected override void HandleViewArgs(object[] args)
        {
            ResetOpenConfigToDefaults();
            hasPendingOpenConfig = true;
            rewardEffectFinishAction = null;

            if (args == null || args.Length == 0)
            {
                return;
            }

            int rewardArgIndex = 0;
            if (args.Length > 1 && TryGetIntValue(args[0], out int panelIndex))
            {
                currentPanelIndex = Mathf.Clamp(panelIndex, 1, 3);
                rewardArgIndex = 1;
            }

            for (int i = rewardArgIndex; i < args.Length; i++)
            {
                if (args[i] is System.Action finishAction)
                {
                    rewardEffectFinishAction = finishAction;
                    continue;
                }

                if (TryGetNumericValue(args[i], out float rewardValue))
                {
                    SetCurrentPanelRewardValue(rewardValue);
                }
            }
        }

        public void SetCurrentPanel(int panelIndex, int? randomValue = null)
        {
            currentPanelIndex = Mathf.Clamp(panelIndex, 1, 3);

            if (randomValue.HasValue)
            {
                SetCurrentPanelRewardValue(randomValue.Value);
            }

            ResetState();
            ApplyPanelState();
            RefreshButtons();
        }

        public void SetPanelRewardValue(int panelIndex, float rewardValue)
        {
            int safePanelIndex = Mathf.Clamp(panelIndex, 1, 3);
            float safeRewardValue = Mathf.Max(0f, rewardValue);
            int resultValue = Mathf.Max(0, Mathf.RoundToInt(safeRewardValue));

            switch (safePanelIndex)
            {
                case 1:
                    currentLotteryPanel1RewardAmount = safeRewardValue;
                    LotteryPanel1?.SetRandomInt(resultValue);
                    break;
            }
        }

        public void SetCurrentPanelRewardValue(float rewardValue)
        {
            SetPanelRewardValue(currentPanelIndex, rewardValue);
        }

        public void SetPanelRandomInt(int panelIndex, int randomValue)
        {
            SetPanelRewardValue(panelIndex, randomValue);
        }

        public void SetCurrentPanelRandomInt(int randomValue)
        {
            SetCurrentPanelRewardValue(randomValue);
        }

        private void ResetState()
        {
            StopRewardDelay();
            isRolling = false;
            hasRewardClaimed = false;
        }

        private void PrepareOpenConfig()
        {
            if (!hasPendingOpenConfig)
            {
                ResetOpenConfigToDefaults();
            }

            hasPendingOpenConfig = false;
        }

        private void ResetOpenConfigToDefaults()
        {
            currentPanelIndex = DefaultPanelIndex;
            rewardEffectFinishAction = null;
            SetPanelRewardValue(DefaultPanelIndex, DefaultClosedDisplayValue);
        }

        private void ApplyPanelState()
        {
            currentPanel = null;
            UpdatePanelState(LotteryPanel1, currentPanelIndex == 1);

        }

        private void UpdatePanelState(LotteryPanel panel, bool shouldShow)
        {
            if (panel == null)
            {
                return;
            }

            if (shouldShow)
            {
                currentPanel = panel;
            }

            if (panel.gameObject.activeSelf != shouldShow)
            {
                panel.gameObject.SetActive(shouldShow);
            }

            if (shouldShow)
            {
                panel.ShowStaticValue(Mathf.RoundToInt(DefaultClosedDisplayValue));
            }
        }

        private void BindButtons()
        {
            if (buttonsBound)
            {
                return;
            }

            if (DrawButton != null)
            {
                DrawButton.onClick.AddListener(OnDrawButtonClick);
            }

            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(OnCloseButtonClick);
            }

            buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (!buttonsBound)
            {
                return;
            }

            if (DrawButton != null)
            {
                DrawButton.onClick.RemoveListener(OnDrawButtonClick);
            }

            if (CloseButton != null)
            {
                CloseButton.onClick.RemoveListener(OnCloseButtonClick);
            }

            buttonsBound = false;
        }

        private void OnDrawButtonClick()
        {
            if (isRolling || hasRewardClaimed)
            {
                return;
            }

            if (currentPanel == null)
            {
                return;
            }

            SoundManager.Instance?.PlayUIClickSFX();
            currentPanel.PlayResultRoll();
        }

        private void OnCloseButtonClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            CloseSlotView();
        }

        private void CloseSlotView()
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

        private LotteryPanel GetCurrentPanel()
        {
            return currentPanel;
        }

        private LotteryPanel GetPanelByIndex(int panelIndex)
        {
            switch (Mathf.Clamp(panelIndex, 1, 3))
            {
                case 1: return LotteryPanel1;
                default: return null;
            }
        }

        private void BindPanelCallbacks()
        {
            BindPanelCallback(LotteryPanel1);

        }

        private void UnbindPanelCallbacks()
        {
            UnbindPanelCallback(LotteryPanel1);

        }

        private void BindPanelCallback(LotteryPanel panel)
        {
            if (panel == null)
            {
                return;
            }

            panel.RollStarted -= OnPanelRollStarted;
            panel.RollStarted += OnPanelRollStarted;
            panel.RollCompleted -= OnPanelRollCompleted;
            panel.RollCompleted += OnPanelRollCompleted;
        }

        private void UnbindPanelCallback(LotteryPanel panel)
        {
            if (panel == null)
            {
                return;
            }

            panel.RollStarted -= OnPanelRollStarted;
            panel.RollCompleted -= OnPanelRollCompleted;
        }

        private void OnPanelRollStarted(LotteryPanel panel, bool isRewardRoll)
        {
            if (panel != GetCurrentPanel())
            {
                return;
            }

            isRolling = true;
            RefreshButtons();
        }

        private void OnPanelRollCompleted(LotteryPanel panel, bool isRewardRoll, int finalValue)
        {
            if (panel != GetCurrentPanel())
            {
                return;
            }

            if (isRewardRoll && !hasRewardClaimed)
            {
                rewardDelayCoroutine = StartCoroutine(DelaySendReward(panel));
                RefreshButtons();
                return;
            }

            isRolling = false;
            RefreshButtons();
        }

        private IEnumerator DelaySendReward(LotteryPanel panel)
        {
            yield return new WaitForSeconds(rewardPopupDelay);
            rewardDelayCoroutine = null;

            if (panel != GetCurrentPanel() || hasRewardClaimed)
            {
                yield break;
            }

            hasRewardClaimed = true;
            isRolling = false;
            SendReward(panel);
            RefreshButtons();
        }

        private void StopRewardDelay()
        {
            if (rewardDelayCoroutine == null)
            {
                return;
            }

            StopCoroutine(rewardDelayCoroutine);
            rewardDelayCoroutine = null;
        }

        private void SendReward(LotteryPanel panel)
        {
            ERewardItemStruct rewardItem = new ERewardItemStruct
            {
                Type = GetRewardType(panel),
                Count = GetRewardAmount(panel)
            };

            FacadeEffectExtend.PlayGetRewardEffectHandle?.Invoke(new[] { rewardItem }, rewardEffectFinishAction);
            CloseSlotView();
        }

        private ERewardType GetRewardType(LotteryPanel panel)
        {
            if (panel == LotteryPanel1)
            {
                return lotteryPanel1RewardType;
            }

      

            return ERewardType.Money;
        }

        private float GetRewardAmount(LotteryPanel panel)
        {
            if (panel == LotteryPanel1)
            {
                return currentLotteryPanel1RewardAmount;
            }

            return 0f;
        }

        private bool TryGetIntValue(object value, out int result)
        {
            switch (value)
            {
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue when longValue >= int.MinValue && longValue <= int.MaxValue:
                    result = (int)longValue;
                    return true;
                case string stringValue when int.TryParse(stringValue, out int parsedValue):
                    result = parsedValue;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private bool TryGetNumericValue(object value, out float result)
        {
            switch (value)
            {
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case float floatValue:
                    result = floatValue;
                    return true;
                case double doubleValue:
                    result = (float)doubleValue;
                    return true;
                case decimal decimalValue:
                    result = (float)decimalValue;
                    return true;
                case string stringValue when float.TryParse(stringValue, out float parsedValue):
                    result = parsedValue;
                    return true;
                default:
                    result = 0f;
                    return false;
            }
        }

        private void RefreshButtons()
        {
            if (DrawButton != null)
            {
                bool showDrawButton = !isRolling && !hasRewardClaimed;
                DrawButton.gameObject.SetActive(showDrawButton);
                DrawButton.interactable = showDrawButton;
            }

            if (CloseButton != null)
            {
                bool showCloseButton = !isRolling && hasRewardClaimed;
                //CloseButton.gameObject.SetActive(showCloseButton);
                CloseButton.interactable = showCloseButton;
            }
        }
    }
}
