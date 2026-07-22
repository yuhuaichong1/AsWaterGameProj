using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static WZSDK.LevelRewardStatic;
using static WZSDK.Tutorial;

namespace WZSDK
{

    public class WelcomeView : BaseView
    {
        public Button AdBtn;
        public Button OnlyBtn;
        public Button Claim;
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
            isRewardGiven = false;
            isProcessing = false;

            if (!hasCustomRewards)
            {
                rewards.Clear();
                rewards = GetRewards();
            }

            RefreshRewardUI();
            BindEvents();
        }

        public List<RewardItem> GetRewards()
        {
            List<RewardItem> rewardList = new List<RewardItem>();

            float coinAmount = GameManagerWZ.instance != null
                ? GameManagerWZ.instance.GetCoinRewardAmount(1)
                : 0f;
            if (coinAmount > 0)
            {
                rewardList.Add(new RewardItem
                {
                    type = 0,
                    amount = coinAmount
                });
            }
            float gemAmount = GameManagerWZ.instance.getResourceNum(2, 1, 0);
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
                            rewards.Add(new LevelRewardStatic.RewardItem
                            {
                                type = item.type,
                                amount = item.amount
                            });
                        }
                    }
                }
                else if (args[i] is LevelRewardStatic.RewardItem singleReward)
                {
                    hasCustomRewards = true;
                    rewards = new List<LevelRewardStatic.RewardItem>
                {
                    new LevelRewardStatic.RewardItem
                    {
                        type = singleReward.type,
                        amount = singleReward.amount
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
                            iconImage.sprite = iconSprite;
                    }
                }
                else
                {
                    rewardObj.SetActive(false);
                }
            }

            /*  if (OnlyText != null)
              {
                  if (rewards.Count > 0)
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

                      OnlyText.transform.parent.gameObject.SetActive(true);
                  }
                  else
                  {
                      OnlyText.transform.parent.gameObject.SetActive(false);
                  }
              }*/
            OnlyText.transform.parent.gameObject.SetActive(false);
        }

        private void BindEvents()
        {
            if (AdBtn != null)
                AdBtn.onClick.RemoveAllListeners();
            if (Claim != null)
                Claim.onClick.RemoveAllListeners();
            if (OnlyBtn != null)
                OnlyBtn.onClick.RemoveAllListeners();
            if (AdBtn != null)
                AdBtn.onClick.AddListener(OnAdClick);
            if (Claim != null)
                Claim.onClick.AddListener(OnClalm);
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
        private void OnClalm()
        {
            PlayRewardEffects(rewards);
            CloseAndReset();
        }
        private void OnAdClick()
        {
            if (isProcessing || isRewardGiven) return;
            isProcessing = true;

            SoundManager.Instance?.PlayUIClickSFX();

            AdsControl.Instance.ShowRewardedAd("challenge_success_reward", success =>
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

            AdsControl.Instance.HandleRefuseAd("challenge_success",
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

                        FacadeEffectExtend.PlayFlyMoneyHandle(transform, GameDefines.Elimination_FlyMoneyCount, rewards[0].amount / 10f, null, ERewardType.Money);
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

        private void CloseAndReset()
        {
            hasCustomRewards = false;
            rewards.Clear();
            isRewardGiven = false;
            isProcessing = false;

            UIManager.instance.CloseView(EUIType.WelcomeView, this);
            GameManagerWZ.instance.RestartLevelTimer();
            if (guideType == 3)
            {
                GameManagerWZ.instance.StartCoroutine(DelayGoToStep(1, 2f));
            }

        }

        private System.Collections.IEnumerator DelayGoToStep(int step, float delay)
        {
            yield return new WaitForSeconds(delay);
            UIManager.instance.ShowView(EUIType.Tutorial);
            Tutorial tutorial = UIManager.instance.GetView<Tutorial>(EUIType.Tutorial);
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





        private void OnDestroy() => RemoveEvents();
        private void OnDisable() => RemoveEvents();

        public override void Start() { }
        public override void Update() { }
    }
}
