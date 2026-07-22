using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using static WZSDK.LevelRewardStatic;
using static WZSDK.Tutorial;

namespace WZSDK
{
    public class RewardView : BaseView
    {
        public Button AdBtn;
        public Button OnlyBtn;
        public RectTransform RewardRoot;
        public Text OnlyText;

        bool _isEventsBound;
        private List<LevelRewardStatic.RewardItem> rewards = new List<LevelRewardStatic.RewardItem>();

        private bool hasCustomRewards = false;
        private bool isRewardGiven = false;
        private bool isProcessing = false;

        private int guideType = -1;

        public override void InitView()
        {
            if (OnlyBtn != null)
                OnlyBtn.gameObject.SetActive(false);

            if (GameManagerWZ.instance != null)
                GameManagerWZ.instance.StartCoroutine(DelayedAction());
            else if (OnlyBtn != null)
                OnlyBtn.gameObject.SetActive(true);

            isRewardGiven = false;
            isProcessing = false;
            if (!hasCustomRewards)
            {
                rewards.Clear();
                rewards = BuildRewardsForCurrentLevel();
            }

            RefreshRewardUI();
            BindEvents();
        }
        IEnumerator DelayedAction()
        {

            yield return new WaitForSeconds(1.0f);
            if (OnlyBtn != null)
                OnlyBtn.gameObject.SetActive(true);
        }

        public List<RewardItem> GetRewards()
        {
            List<RewardItem> rewardList = new List<RewardItem>();

            float coinAmount = GameManagerWZ.instance != null
                ? GameManagerWZ.instance.GetCoinRewardAmount(1)
                : 0f;
            rewardList.Add(new RewardItem
            {
                type = 0,
                amount = Mathf.Max(coinAmount, 0f)
            });
            float gemAmount = 0;//GameManagerWZ.instance.getResourceNum(2, 1, 0);
            if (gemAmount > 0)
            {
                rewardList.Add(new RewardItem
                {
                    type = 1,
                    amount = gemAmount
                });
            }
            return rewardList;
        }

        private List<RewardItem> BuildRewardsForCurrentLevel()
        {
            return GetRewards();
        }

        protected override void HandleViewArgs(object[] args)
        {
            hasCustomRewards = false;
            rewards.Clear();
            guideType = -1;

            if (args == null || args.Length == 0)
            {
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is List<LevelRewardStatic.RewardItem> customRewards)
                {
                    if (customRewards != null && customRewards.Count > 0)
                    {
                        hasCustomRewards = true;
                        rewards = new List<LevelRewardStatic.RewardItem>();
                        foreach (var item in customRewards)
                        {
                            if (item == null)
                                continue;

                            rewards.Add(new LevelRewardStatic.RewardItem
                            {
                                type = item.type,
                                amount = GetSafeRewardAmount(item)
                            });
                        }

                        hasCustomRewards = rewards.Count > 0;
                    }
                }
                else if (args[i] is LevelRewardStatic.RewardItem singleReward)
                {
                    if (singleReward == null)
                        continue;

                    hasCustomRewards = true;
                    rewards = new List<LevelRewardStatic.RewardItem>
                {
                    new LevelRewardStatic.RewardItem
                    {
                        type = singleReward.type,
                        amount = GetSafeRewardAmount(singleReward)
                    }
                };
                }
                else if (args[i] is int type)
                {
                    guideType = type;
                }
            }
        }

        private void RefreshRewardUI()
        {
            if (RewardRoot == null) return;

            int slotCount = RewardRoot.childCount;

            for (int i = 0; i < slotCount; i++)
            {
                GameObject rewardObj = RewardRoot.GetChild(i).gameObject;

                if (i < rewards.Count)
                {
                    rewardObj.SetActive(true);

                    Text countText = rewardObj.GetComponentInChildren<Text>();
                    if (countText != null)
                    {
                        countText.text = FormatRewardAmount(rewards[i]);
                    }

                    Image iconImage = rewardObj.GetComponentInChildren<Image>();
                    if (iconImage != null)
                    {
                        ELuckySpinRewardType rewardType = ConvertToLuckySpinType(rewards[i].type);
                        Sprite iconSprite = GetRewardIcon(rewardType);
                        if (iconSprite != null)
                            iconImage.sprite = iconSprite;
                    }
                }
                else
                {
                    rewardObj.SetActive(false);
                }
            }

            if (OnlyText != null)
            {
                if (rewards.Count > 0)
                {
                    float oneTenthAmount = rewards[0].amount / 10f;
                    string onlyPrefix = GetOnlyRewardPrefix();

                    if (rewards[0].type == 1)
                    {
                        OnlyText.text = onlyPrefix + oneTenthAmount.ToString("F1");
                    }
                    else
                    {
                        OnlyText.text = onlyPrefix + FormatMoneyAmount(oneTenthAmount);
                    }

                    if (OnlyText.transform.parent != null)
                        OnlyText.transform.parent.gameObject.SetActive(true);
                }
                else
                {
                    if (OnlyText.transform.parent != null)
                        OnlyText.transform.parent.gameObject.SetActive(false);
                }
            }
        }

        private void BindEvents()
        {
            if (AdBtn != null)
                AdBtn.onClick.RemoveAllListeners();
            if (OnlyBtn != null)
                OnlyBtn.onClick.RemoveAllListeners();
            if (AdBtn != null)
                AdBtn.onClick.AddListener(OnAdClick);
            if (OnlyBtn != null)
                OnlyBtn.onClick.AddListener(OnOnlyClick);

            _isEventsBound = true;
        }

        private void RemoveEvents()
        {
            if (AdBtn != null)
                AdBtn.onClick.RemoveAllListeners();
            if (OnlyBtn != null)
                OnlyBtn.onClick.RemoveAllListeners();
            _isEventsBound = false;
        }

        private void OnAdClick()
        {
            if (isProcessing || isRewardGiven) return;
            isProcessing = true;

            SoundManager.Instance?.PlayUIClickSFX();

            if (AdsControl.Instance == null)
            {
                Debug.LogWarning("RewardView: AdsControl.Instance is null");
                CloseAndReset();
                return;
            }

            AdsControl.Instance.ShowRewardedAd("reward_success_reward", success =>
            {
                if (success && !isRewardGiven)
                {
                    isRewardGiven = true;
                    PlayRewardEffects(rewards);
                    Debug.Log($"广告观看成功，获得{rewards.Count}个奖励（10倍）");
                }
                else
                {
                    Debug.Log("广告观看失败或奖励已发放");
                }
                CloseAndReset();
            });
        }

        private void OnOnlyClick()
        {
            if (isProcessing || isRewardGiven) return;
            isProcessing = true;

            SoundManager.Instance?.PlayUIClickSFX();

            if (AdsControl.Instance == null)
            {
                Debug.LogWarning("RewardView: AdsControl.Instance is null");
                CloseAndReset();
                return;
            }

            AdsControl.Instance.HandleRefuseAd("force_reward",
                () =>
                {
                    if (!isRewardGiven && rewards.Count > 0)
                    {
                        isRewardGiven = true;
                        PlayRewardEffects(rewards);
                    }
                    CloseAndReset();
                },
                (failMsg) =>
                {
                    if (!isRewardGiven && rewards.Count > 0)
                    {
                        isRewardGiven = true;
                        /*    List<LevelRewardStatic.RewardItem> oneTenthRewards = new List<LevelRewardStatic.RewardItem>();
                            oneTenthRewards.Add(new LevelRewardStatic.RewardItem
                            {
                                type = rewards[0].type,
                                amount = rewards[0].amount / 10f
                            }) ;
                            PlayRewardEffects(oneTenthRewards);*/

                        RewardItem moneyReward = GetFirstMoneyReward();
                        if (moneyReward != null)
                        {
                            float rewardAmount = moneyReward.amount / 10f;
                            GameManagerWZ.instance?.AddCoin(rewardAmount);
                            FacadeEffectExtend.PlayFlyMoneyHandle?.Invoke(transform, GameDefines.Elimination_FlyMoneyCount, rewardAmount, null, ERewardType.Money);
                        }
                    }
                    CloseAndReset();
                });
        }

        /// <summary>
        /// 播放多个奖励特效（一次性传入所有奖励）
        /// </summary>
        private void PlayRewardEffects(List<LevelRewardStatic.RewardItem> rewardsToPlay)
        {
            if (rewardsToPlay == null || rewardsToPlay.Count == 0) return;
            List<ERewardItemStruct> rewardStructs = new List<ERewardItemStruct>();

            foreach (var reward in rewardsToPlay)
            {
                if (reward == null)
                    continue;

                ERewardType rewardType = ConvertToERewardType(ConvertToLuckySpinType(reward.type));

                rewardStructs.Add(new ERewardItemStruct()
                {
                    Type = rewardType,
                    Count = reward.amount,
                });
            }

            if (rewardStructs.Count == 0) return;

            if (FacadeEffectExtend.PlayGetRewardEffectHandle != null)
                FacadeEffectExtend.PlayGetRewardEffectHandle(rewardStructs.ToArray(), null);
            else
                GrantRewardsDirectly(rewardStructs);
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

        private void CloseAndReset()
        {
            hasCustomRewards = false;
            rewards.Clear();
            isRewardGiven = false;
            isProcessing = false;

            UIManager.instance?.CloseView(EUIType.RewardView, this);
            GameManagerWZ.instance?.RestartLevelTimer();
            if (guideType == 3)
            {
                GameManagerWZ.instance?.StartCoroutine(DelayGoToStep(1, 2f));
            }

        }

        private System.Collections.IEnumerator DelayGoToStep(int step, float delay)
        {
            yield return new WaitForSeconds(delay);
            UIManager.instance?.ShowView(EUIType.Tutorial);
            Tutorial tutorial = UIManager.instance?.GetView<Tutorial>(EUIType.Tutorial);
            if (tutorial == null) yield break;

            tutorial.currentType = TYPE.TYPE3;
            tutorial.GoToStep(step);
        }

        private ELuckySpinRewardType ConvertToLuckySpinType(int type) => type switch
        {
            0 => ELuckySpinRewardType.Money,
            1 => ELuckySpinRewardType.Gem,
            2 => ELuckySpinRewardType.AddBottle,
            3 => ELuckySpinRewardType.Clear,
            4 => ELuckySpinRewardType.Undo,
            _ => ELuckySpinRewardType.Money
        };

        private Sprite GetRewardIcon(ELuckySpinRewardType type) => type switch
        {
            ELuckySpinRewardType.Money => GameDefines.ifIAA
                ? Resources.Load<Sprite>(GameDefines.LSIAAMoneyIconPath)
                : (FacadePayTypeExtend.GetMultCoinCoinAct?.Invoke()
                   ?? Resources.Load<Sprite>(GameDefines.ERMultCoinIconPath)),
            ELuckySpinRewardType.Gem => FacadePayTypeExtend.GetMulteGem(),
            ELuckySpinRewardType.AddBottle => Resources.Load<Sprite>(GameDefines.ERAddSpaceIconPath),
            ELuckySpinRewardType.Clear => Resources.Load<Sprite>(GameDefines.ERClearIconPath),
            ELuckySpinRewardType.Undo => Resources.Load<Sprite>(GameDefines.ERHammerIconPath),
            _ => null
        };

        private float GetSafeRewardAmount(RewardItem reward)
        {
            if (reward == null)
                return 0f;

            return reward.type == 0 ? Mathf.Max(reward.amount, 0f) : reward.amount;
        }

        private string FormatRewardAmount(RewardItem reward)
        {
            if (reward == null)
                return "0";

            if (reward.type == 1)
                return ((int)reward.amount).ToString();

            return FacadePayTypeExtend.RegionalChangeHandle != null
                ? FacadePayTypeExtend.RegionalChangeHandle(reward.amount)
                : reward.amount.ToString("0.##");
        }

        private string FormatMoneyAmount(float amount)
        {
            return FacadePayTypeExtend.RegionalChangeHandle != null
                ? FacadePayTypeExtend.RegionalChangeHandle(amount)
                : amount.ToString("0.##");
        }

        private string GetOnlyRewardPrefix()
        {
            return LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText("1021")
                : string.Empty;
        }

        private RewardItem GetFirstMoneyReward()
        {
            if (rewards == null)
                return null;

            foreach (RewardItem reward in rewards)
            {
                if (reward != null && reward.type == 0)
                    return reward;
            }

            return null;
        }

        private void GrantRewardsDirectly(List<ERewardItemStruct> rewardStructs)
        {
            if (GameManagerWZ.instance == null || rewardStructs == null)
                return;

            foreach (ERewardItemStruct reward in rewardStructs)
            {
                switch (reward.Type)
                {
                    case ERewardType.Money:
                        GameManagerWZ.instance.AddCoin(reward.Count);
                        break;
                    case ERewardType.Gem:
                        GameManagerWZ.instance.AddGems(reward.Count);
                        break;
                }
            }
        }





        private void OnDestroy() => RemoveEvents();
        private void OnDisable() => RemoveEvents();

        public override void Start() { }
        public override void Update() { }
    }
}
