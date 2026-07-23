
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static WZSDK.LevelRewardStatic;

namespace WZSDK
{
    public class ChallangeSuccessView : BaseView
    {
        private const int LuckySpinAdTarget = 6;
        private const string LuckySpinAdCountKey = "ChallengeSuccessLuckySpinAdCount_Global";
        private const string LuckySpinReadyKey = "ChallengeSuccessLuckySpinReady_Global";
        private const int GemRewardType = 1;

        private Button AdBtn;
        private Button OnlyBtn;
        private RectTransform RewardRoot;
        private Text OnlyText;
        private Text Dec;
        private RectTransform ProgressRoot;

        private int currentLevel;
        private List<LevelRewardStatic.RewardItem> rewards = new List<LevelRewardStatic.RewardItem>();
        private bool _isEventsBound;
        private bool _isReferencesResolved;
        private Image progress;
        private RectTransform progressStateRoot;
        private readonly WithdrawMissionStateItem[] progressStates = new WithdrawMissionStateItem[LuckySpinAdTarget];
        private bool IsOnly;
        private bool shouldShowLuckySpin;
        private Action externalContinueAction;

        private void Awake()
        {
            ResolveReferencesIfNeeded();
        }

        public override void InitView()
        {
            // 仅 HostDriven 第2关及以后清引导；第1关 Goal1 通关页被跳过，不应误伤 WithDraw 引导。
            // 若仍打开了通关页，再清残留手指。
            if (WaterSortWZBridge.HostDriven && currentLevel >= 2)
                GameManagerWZ.instance?.CloseActiveTutorial();

            ResolveReferencesIfNeeded();

            if (OnlyBtn != null)
                OnlyBtn.gameObject.SetActive(false);

            GameManagerWZ.instance.StartCoroutine(DelayedAction());
            IsOnly = false;
            shouldShowLuckySpin = HasPendingLuckySpin();
            rewards = BuildRewardsForCurrentLevel();
            RefreshModeUI();
            RefreshRewardUI();
            RefreshLuckySpinProgressUI();
            BindEvents();
        }
        IEnumerator DelayedAction()
        {
            yield return new WaitForSeconds(1.0f);
            if (OnlyBtn != null)
                OnlyBtn.gameObject.SetActive(true);
        }
        private void OnDisable()
        {

        }

        public List<RewardItem> GetRewards()
        {
            List<RewardItem> rewardList = new List<RewardItem>();

            float coinAmount = GameManagerWZ.instance != null
                ? GameManagerWZ.instance.GetCoinRewardAmount(1, currentLevel)
                : 0f;
            rewardList.Add(new RewardItem
            {
                type = 0,
                amount = Mathf.Max(coinAmount, 0f)
            });

            return rewardList;
        }

        private List<RewardItem> BuildRewardsForCurrentLevel()
        {
            return FilterChallengeRewards(GetRewards());
        }

        private List<RewardItem> FilterChallengeRewards(List<RewardItem> sourceRewards)
        {
            List<RewardItem> filteredRewards = new List<RewardItem>();
            if (sourceRewards == null)
                return filteredRewards;

            foreach (RewardItem reward in sourceRewards)
            {
                if (reward == null || reward.type == GemRewardType)
                    continue;

                if (reward.type != 0 && reward.amount <= 0f)
                    continue;

                filteredRewards.Add(new RewardItem
                {
                    type = reward.type,
                    amount = reward.type == 0 ? Mathf.Max(reward.amount, 0f) : reward.amount
                });
            }

            return filteredRewards;
        }

        private void RefreshRewardUI()
        {
            if (RewardRoot == null) return;

            bool hasReward = rewards != null && rewards.Count > 0;
            RewardRoot.gameObject.SetActive(hasReward);

            for (int i = 0; i < RewardRoot.childCount; i++)
            {
                GameObject rewardObj = RewardRoot.GetChild(i).gameObject;

                if (i < rewards.Count)
                {
                    rewardObj.SetActive(true);

                    Text countText = rewardObj.GetComponentInChildren<Text>();
                    if (countText != null)
                    {
                        if (rewards[i].type == 1)
                        {
                            countText.text = ((int)rewards[i].amount).ToString();
                        }
                        else
                        {
                            countText.text = FacadePayTypeExtend.RegionalChangeHandle(rewards[i].amount);
                        }
                    }

                    Image iconImage = rewardObj.GetComponentInChildren<Image>();
                    if (iconImage != null)
                    {
                        ELuckySpinRewardType rewardType = ConvertToLuckySpinType(rewards[i].type);
                        Sprite iconSprite = GetRewardIcon(rewardType);
                        if (iconSprite != null)
                        {
                            iconImage.sprite = iconSprite;
                        }
                    }
                }
                else
                {
                    rewardObj.SetActive(false);
                }
            }

            if (hasReward)
                LayoutRebuilder.ForceRebuildLayoutImmediate(RewardRoot);

            if (OnlyText != null)
            {
                if (OnlyText.transform.parent != null)
                    OnlyText.transform.parent.gameObject.SetActive(hasReward);

                if (hasReward)
                {
                    float oneTenthAmount = rewards[0].amount / 10f;

                    if (rewards[0].type == 1)
                    {
                        OnlyText.text = LocalizationManager.Instance.GetText("1021") + oneTenthAmount.ToString("F1");
                    }
                    else
                    {
                        OnlyText.text = LocalizationManager.Instance.GetText("1021") + FacadePayTypeExtend.RegionalChangeHandle(oneTenthAmount);
                    }
                }
            }
        }

        private void BindEvents()
        {
            ResolveReferencesIfNeeded();
            if (_isEventsBound) return;
            if (AdBtn == null && OnlyBtn == null) return;

            AdBtn?.onClick.AddListener(OnAdClick);
            OnlyBtn?.onClick.AddListener(OnOnlyClick);
            _isEventsBound = true;
        }

        private void RemoveEvents()
        {
            AdBtn?.onClick.RemoveAllListeners();
            OnlyBtn?.onClick.RemoveAllListeners();
            _isEventsBound = false;
        }

        private void ResolveReferencesIfNeeded()
        {
            if (_isReferencesResolved)
                return;

            AdBtn = FindByPath<Button>("Plane/AdBtn", "AdBtn");
            OnlyBtn = FindByPath<Button>("Plane/OnlyBtn", "OnlyBtn");
            RewardRoot = FindByPath<RectTransform>("Plane/RewardRoot", "RewardRoot");
            OnlyText = FindByPath<Text>("Plane/OnlyBtn/OnlyText", "OnlyText");
            ProgressRoot = FindByPath<RectTransform>("ProgressRoot", "ProgressRoot");
            Dec = FindByPath<Text>("ProgressRoot/Dec", "Dec");
            progress = FindByPath<Image>("ProgressRoot/BG/Progress", "Progress");
            progressStateRoot = FindByPath<RectTransform>("ProgressRoot/ProgressStateRoot", "ProgressStateRoot");

            if (progressStateRoot != null)
            {
                int count = Mathf.Min(progressStateRoot.childCount, progressStates.Length);
                for (int i = 0; i < count; i++)
                    progressStates[i] = GetOrAddStateItem(progressStateRoot.GetChild(i).gameObject);
            }

            _isReferencesResolved = AdBtn != null && OnlyBtn != null && RewardRoot != null && OnlyText != null;
        }

        private void RefreshModeUI()
        {
            bool shouldShowProgressRoot = !GameDefines.ifIAA;

            if (ProgressRoot != null)
            {
                ProgressRoot.gameObject.SetActive(shouldShowProgressRoot);
                SetProgressRootRaycastEnabled(false);
            }

            if (!shouldShowProgressRoot)
                ResetLuckySpinProgressState();
        }

        private void SetProgressRootRaycastEnabled(bool enabled)
        {
            if (ProgressRoot == null)
                return;

            Graphic[] graphics = ProgressRoot.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                    graphic.raycastTarget = enabled;
            }
        }

        private T FindByPath<T>(string path, string fallbackName) where T : Component
        {
            GameObject target = FindChildUtility.FindChildByPath(gameObject, path);
            if (target == null)
                target = FindChildUtility.FindChild(gameObject, fallbackName);

            return target != null ? target.GetComponent<T>() : null;
        }

        private void OnAdClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();

            AdsControl.Instance.ShowRewardedAd("challenge_success_reward", success =>
            {
                if (success)
                {
                    IsOnly = false;
                    shouldShowLuckySpin = HasPendingLuckySpin();
                    Debug.Log($"广告观看成功，发放{rewards.Count}个奖励");
                }
                else
                {
                    Debug.Log("广告观看失败");
                }
                CloseAndNextLevel();
            }, false);
        }

        private void OnOnlyClick()
        {
            SoundManager.Instance?.PlayUIClickSFX();

            AdsControl.Instance.HandleRefuseAd("force_reward",
                () =>
                {
                    IsOnly = false;
                    shouldShowLuckySpin = HasPendingLuckySpin();
                    CloseAndNextLevel();
                },
                (failMsg) =>
                {
                    IsOnly = true;
                    CloseAndNextLevel();
                },
                false
            );
        }

        /// <summary>
        /// 播放多个奖励特效
        /// </summary>
        private void PlayRewardEffects(List<LevelRewardStatic.RewardItem> rewardsToPlay)
        {
            if (rewardsToPlay == null || rewardsToPlay.Count == 0) return;

            List<ERewardItemStruct> rewardStructs = new List<ERewardItemStruct>();

            foreach (var reward in rewardsToPlay)
            {
                ERewardType rewardType = ConvertToERewardType(ConvertToLuckySpinType(reward.type));

                rewardStructs.Add(new ERewardItemStruct()
                {
                    Type = rewardType,
                    Count = reward.amount,
                });
            }

            FacadeEffectExtend.PlayGetRewardEffectHandle(rewardStructs.ToArray(), null);
        }

        /// <summary>
        /// 将ELuckySpinRewardType转换为ERewardType
        /// </summary>
        private ERewardType ConvertToERewardType(ELuckySpinRewardType type)
        {
            switch (type)
            {
                case ELuckySpinRewardType.Money: return ERewardType.Money;
                case ELuckySpinRewardType.Gem: return ERewardType.Gem;
                case ELuckySpinRewardType.AddBottle: return ERewardType.AddBottle;
                case ELuckySpinRewardType.Clear: return ERewardType.Clear;
                case ELuckySpinRewardType.Undo: return ERewardType.Undo;
                default: return ERewardType.Money;
            }
        }

        /// <summary>
        /// 关闭面板并进入下一关
        /// </summary>
        private void CloseAndNextLevel()
        {
            Time.timeScale = 1f;
            GameManagerWZ.instance.StartCoroutine(DelayedNextLevel());
        }

        private System.Collections.IEnumerator DelayedNextLevel()
        {
            UIManager.instance.CloseView(EUIType.ChallangeSuccessView);

            if (!IsOnly)
                PlayRewardEffects(rewards);

            yield return new WaitForSeconds(1f);

            if (externalContinueAction != null)
            {
                Action callback = externalContinueAction;
                externalContinueAction = null;
                callback.Invoke();
            }
            else if (GameManagerWZ.instance != null
                     && GameManagerWZ.instance.ShouldDeferNextLevelForStage3CompletionFlow())
            {
                // Stage3 达标链路进行中：先弹 LuckyWallet→…→Slot，结束后再 NextLevel。
            }
            else
            {
                GameManagerWZ.instance.NextLevel();
            }

            if (IsOnly && rewards.Count > 0 && rewards[0].type == 0 && rewards[0].amount > 0f)
            {
                GameManagerWZ.instance.AddCoin(rewards[0].amount / 10f, currentLevel);
                FacadeEffectExtend.PlayFlyMoneyHandle(transform, GameDefines.Elimination_FlyMoneyCount, rewards[0].amount / 10f, null, ERewardType.Money);
            }

            if (shouldShowLuckySpin && !WaterSortWZBridge.HostDriven
                && (GameManagerWZ.instance == null
                    || !GameManagerWZ.instance.ShouldDeferNextLevelForStage3CompletionFlow()))
            {
                shouldShowLuckySpin = false;
                ConsumePendingLuckySpin();
                yield return null;
                UIManager.instance.ShowView(EUIType.LuckySpinView);
            }
        }
        /// <summary>
        /// 将奖励类型转换
        /// </summary>
        private ELuckySpinRewardType ConvertToLuckySpinType(int type)
        {
            switch (type)
            {
                case 0: return ELuckySpinRewardType.Money;
                case 1: return ELuckySpinRewardType.Gem;
                case 2: return ELuckySpinRewardType.AddBottle;
                case 3: return ELuckySpinRewardType.Clear;
                case 4: return ELuckySpinRewardType.Undo;
                default: return ELuckySpinRewardType.Money;
            }
        }

        /// <summary>
        /// 获取奖励ICON
        /// </summary>
        private Sprite GetRewardIcon(ELuckySpinRewardType type)
        {
            switch (type)
            {
                case ELuckySpinRewardType.Money:
                    return GameDefines.ifIAA
                        ? Resources.Load<Sprite>(GameDefines.LSIAAMoneyIconPath)
                        : (FacadePayTypeExtend.GetMultCoinCoinAct?.Invoke()
                           ?? Resources.Load<Sprite>(GameDefines.ERMultCoinIconPath));
                case ELuckySpinRewardType.Gem:
                    return FacadePayTypeExtend.GetMulteGem();
                case ELuckySpinRewardType.AddBottle:
                    return Resources.Load<Sprite>(GameDefines.ERAddSpaceIconPath);
                case ELuckySpinRewardType.Clear:
                    return Resources.Load<Sprite>(GameDefines.ERClearIconPath);
                case ELuckySpinRewardType.Undo:
                    return Resources.Load<Sprite>(GameDefines.ERHammerIconPath);
                default:
                    return null;
            }
        }

        protected override void HandleViewArgs(object[] args)
        {
            externalContinueAction = null;
            if (args == null || args.Length == 0)
                return;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is int level)
                    currentLevel = level;
                else if (args[i] is Action continueAction)
                    externalContinueAction = continueAction;
            }
        }

        private void OnDestroy()
        {
            if (_isEventsBound) RemoveEvents();
        }

        private void RefreshLuckySpinProgressUI()
        {
            if (GameDefines.ifIAA)
            {
                if (ProgressRoot != null)
                    ProgressRoot.gameObject.SetActive(false);
                return;
            }

            int adCount = Mathf.Clamp(GetLuckySpinAdCount(), 0, LuckySpinAdTarget);
            int remainingCount = Mathf.Max(LuckySpinAdTarget - adCount, 0);

            for (int i = 0; i < progressStates.Length; i++)
                progressStates[i]?.SetState(i + 1, i < adCount);

            if (progress != null)
                progress.fillAmount = (float)adCount / LuckySpinAdTarget;

            if (Dec != null)
                Dec.text = GetLuckySpinDecText(remainingCount);
        }

        private string GetLuckySpinDecText(int remainingCount)
        {
            return string.Format(LocalizationManager.Instance.GetText("3070"), remainingCount);
        }

        private WithdrawMissionStateItem GetOrAddStateItem(GameObject target)
        {
            if (target == null)
                return null;

            WithdrawMissionStateItem component = target.GetComponent<WithdrawMissionStateItem>();
            if (component == null)
                component = target.AddComponent<WithdrawMissionStateItem>();

            return component;
        }

        private int GetLuckySpinAdCount()
        {
            return PlayerPrefs.GetInt(LuckySpinAdCountKey, 0);
        }

        private void SaveLuckySpinAdCount(int count)
        {
            PlayerPrefs.SetInt(LuckySpinAdCountKey, Mathf.Clamp(count, 0, LuckySpinAdTarget));
            PlayerPrefs.Save();
        }

        private static void ResetLuckySpinProgressState()
        {
            bool hadAdCount = PlayerPrefs.HasKey(LuckySpinAdCountKey);
            bool hadReadyState = PlayerPrefs.HasKey(LuckySpinReadyKey);

            if (!hadAdCount && !hadReadyState)
                return;

            PlayerPrefs.DeleteKey(LuckySpinReadyKey);
            PlayerPrefs.DeleteKey(LuckySpinAdCountKey);
            PlayerPrefs.Save();
        }

        public static bool HasPendingLuckySpinReward()
        {
            if (GameDefines.ifIAA)
            {
                ResetLuckySpinProgressState();
                return false;
            }

            return PlayerPrefs.GetInt(LuckySpinReadyKey, 0) == 1;
        }

        public static void ConsumePendingLuckySpinReward()
        {
            ResetLuckySpinProgressState();
        }

        private bool HasPendingLuckySpin()
        {
            return HasPendingLuckySpinReward();
        }

        private void ConsumePendingLuckySpin()
        {
            ConsumePendingLuckySpinReward();
        }

        public static void RecordAdSuccessProgress()
        {
            if (GameDefines.ifIAA)
            {
                ResetLuckySpinProgressState();
                return;
            }

            int currentCount = PlayerPrefs.GetInt(LuckySpinAdCountKey, 0);
            bool isReady = PlayerPrefs.GetInt(LuckySpinReadyKey, 0) == 1;

            if (isReady || currentCount >= LuckySpinAdTarget)
                return;

            currentCount++;
            if (currentCount >= LuckySpinAdTarget)
            {
                currentCount = LuckySpinAdTarget;
                PlayerPrefs.SetInt(LuckySpinReadyKey, 1);
            }

            PlayerPrefs.SetInt(LuckySpinAdCountKey, currentCount);
            PlayerPrefs.Save();
        }

        public override void Start() { }
        public override void Update() { }
    }
}
