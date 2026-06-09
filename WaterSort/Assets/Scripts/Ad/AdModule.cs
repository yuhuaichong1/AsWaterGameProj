using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public class AdModule : BaseModule
    {
        private EAdSource rewardAdScore;                                            //激励广告来源
        private EAdSource interstitialAdScore;                                      //插屏广告来源
        private EAdSource bannerAdScore;                                            //横幅广告来源
        private EAdSource appOpenAdScore;                                           //开屏广告来源

        private Dictionary<EAdSource, Action<int>> rewardSuccessActions;            //激励广告奖励回调合集
        private Dictionary<EAdSource, Action<int>> interstitialSuccessActions;      //插屏广告奖励回调合集
        private Dictionary<EAdSource, Action<int>> bannerSuccessActions;            //横幅广告奖励回调合集
        private Dictionary<EAdSource, Action<int>> appOpenSuccessActions;           //开屏广告奖励回调合集
        private Dictionary<EAdSource, Action<string>> rewardFailActions;            //激励广告失败回调合集
        private Dictionary<EAdSource, Action<string>> interstitialFailActions;      //插屏广告失败回调合集
        private Dictionary<EAdSource, Action<string>> bannerFailActions;            //横幅广告失败回调合集
        private Dictionary<EAdSource, Action<string>> appOpenFailActions;           //开屏广告失败回调合集
        private Dictionary<EAdSource, Action> rewardHideActions;                    //激励广告关闭回调合集

        private string AdFailMsg;//广告加载失败信息

        #region 额外扩充

        private int curRefuseCount;//当前拒绝次数
        private int totalAdCount;//总看广告次数
        private double totalAdRevenue;//总看广告收入

        #endregion

        protected override void OnLoad() 
        {
            AddFacade();

            AdFailMsg = FacadeLanguage.GetText("10088") ?? "Ad loading failed, please try again later";

            rewardSuccessActions = new Dictionary<EAdSource, Action<int>>();
            interstitialSuccessActions = new Dictionary<EAdSource, Action<int>>();
            bannerSuccessActions = new Dictionary<EAdSource, Action<int>>();
            appOpenSuccessActions = new Dictionary<EAdSource, Action<int>>();
            rewardFailActions = new Dictionary<EAdSource, Action<string>>();
            interstitialFailActions = new Dictionary<EAdSource, Action<string>>();
            bannerFailActions = new Dictionary<EAdSource, Action<string>>();
            appOpenFailActions = new Dictionary<EAdSource, Action<string>>();
            rewardHideActions = new Dictionary<EAdSource, Action>();
        }

        #region Facade

        private void AddFacade()
        {
            FacadeAd.PlayRewardAd += PlayRewardAd;
            FacadeAd.PlayInterAd += PlayInterAd;
            FacadeAd.PlayBannerAd += PlayBannerAd;
            FacadeAd.StopBannerAd += StopBannerAd;
            FacadeAd.PlayROIAd += PlayROIAd;
            FacadeAd.PlayROIAdByRevenue += PlayROIAdByRevenue;
            FacadeAd.PlayROIAdByWeight += PlayROIAdByWeight;
            FacadeAd.PlayAppOpenAd += PlayAppOpenAd;

            FacadeAd.RewardAdLoaded += RewardAdLoaded;
            FacadeAd.RewardAdLoadFailed += RewardAdLoadFailed;
            FacadeAd.RewardAdDisplayed += RewardAdDisplayed;
            FacadeAd.RewardAdDisplayFailed += RewardAdDisplayFailed;
            FacadeAd.RewardAdCompleted += RewardAdCompleted;
            FacadeAd.RewardAdClicked += RewardAdClicked;
            FacadeAd.RewardAdClosed += RewardAdClosed;
            FacadeAd.RewardAdRevenuePaid += RewardAdRevenuePaid;
            FacadeAd.RewardAdNotReady += RewardAdNotReady;
            FacadeAd.RewardAdReceivedReward += RewardAdReceivedReward;

            FacadeAd.InterstitialAdLoaded += InterstitialAdLoaded;
            FacadeAd.InterstitialAdLoadFailed += InterstitialAdLoadFailed;
            FacadeAd.InterstitialAdDisplayed += InterstitialAdDisplayed;
            FacadeAd.InterstitialAdDisplayFailed += InterstitialAdDisplayFailed;
            FacadeAd.InterstitialAdCompleted += InterstitialAdCompleted;
            FacadeAd.InterstitialAdClicked += InterstitialAdClicked;
            FacadeAd.InterstitialAdClosed += InterstitialAdClosed;
            FacadeAd.InterstitialAdRevenuePaid += InterstitialAdRevenuePaid;
            FacadeAd.InterstitialAdNotReady += InterstitialAdNotReady;

            FacadeAd.BannerAdLoaded += BannerAdLoaded;
            FacadeAd.BannerAdLoadFailed += BannerAdLoadFailed;
            FacadeAd.BannerAdDisplayed += BannerAdDisplayed;
            FacadeAd.BannerAdDisplayFailed += BannerAdDisplayFailed;
            FacadeAd.BannerAdCompleted += BannerAdCompleted;
            FacadeAd.BannerAdClicked += BannerAdClicked;
            FacadeAd.BannerAdClosed += BannerAdClosed;
            FacadeAd.BannerAdRevenuePaid += BannerAdRevenuePaid;
            FacadeAd.BannerAdNotReady += BannerAdNotReady;

            FacadeAd.AppOpenAdLoaded += AppOpenAdLoaded;
            FacadeAd.AppOpenLoadFailed += AppOpenLoadFailed;
            FacadeAd.AppOpenDisplayed += AppOpenDisplayed;
            FacadeAd.AppOpenDisplayFailed += AppOpenDisplayFailed;
            FacadeAd.AppOpenCompleted += AppOpenCompleted;
            FacadeAd.AppOpenClicked += AppOpenClicked;
            FacadeAd.AppOpenClosed += AppOpenClosed;
            FacadeAd.AppOpenRevenuePaid += AppOpenRevenuePaid;
            FacadeAd.AppOpenotReady += AppOpenotReady;

            FacadeAd.AdRefuse += AdRefuse;
            FacadeAd.GetTotalAdwatch += GetTotalAdwatch;
        }

        private void RemoveFacade()
        {
            FacadeAd.PlayRewardAd -= PlayRewardAd;
            FacadeAd.PlayInterAd -= PlayInterAd;
            FacadeAd.PlayBannerAd -= PlayBannerAd;
            FacadeAd.StopBannerAd -= StopBannerAd;
            FacadeAd.PlayROIAd -= PlayROIAd;
            FacadeAd.PlayROIAdByRevenue -= PlayROIAdByRevenue;
            FacadeAd.PlayROIAdByWeight -= PlayROIAdByWeight;
            FacadeAd.PlayAppOpenAd -= PlayAppOpenAd;

            FacadeAd.RewardAdLoaded -= RewardAdLoaded;
            FacadeAd.RewardAdLoadFailed -= RewardAdLoadFailed;
            FacadeAd.RewardAdDisplayed -= RewardAdDisplayed;
            FacadeAd.RewardAdDisplayFailed -= RewardAdDisplayFailed;
            FacadeAd.RewardAdCompleted -= RewardAdCompleted;
            FacadeAd.RewardAdClicked -= RewardAdClicked;
            FacadeAd.RewardAdClosed -= RewardAdClosed;
            FacadeAd.RewardAdRevenuePaid -= RewardAdRevenuePaid;
            FacadeAd.RewardAdNotReady -= RewardAdNotReady;
            FacadeAd.RewardAdReceivedReward -= RewardAdReceivedReward;

            FacadeAd.InterstitialAdLoaded -= InterstitialAdLoaded;
            FacadeAd.InterstitialAdLoadFailed -= InterstitialAdLoadFailed;
            FacadeAd.InterstitialAdDisplayed -= InterstitialAdDisplayed;
            FacadeAd.InterstitialAdDisplayFailed -= InterstitialAdDisplayFailed;
            FacadeAd.InterstitialAdCompleted -= InterstitialAdCompleted;
            FacadeAd.InterstitialAdClicked -= InterstitialAdClicked;
            FacadeAd.InterstitialAdClosed -= InterstitialAdClosed;
            FacadeAd.InterstitialAdRevenuePaid -= InterstitialAdRevenuePaid;
            FacadeAd.InterstitialAdNotReady += InterstitialAdNotReady;

            FacadeAd.BannerAdLoaded -= BannerAdLoaded;
            FacadeAd.BannerAdLoadFailed -= BannerAdLoadFailed;
            FacadeAd.BannerAdDisplayed -= BannerAdDisplayed;
            FacadeAd.BannerAdDisplayFailed -= BannerAdDisplayFailed;
            FacadeAd.BannerAdCompleted -= BannerAdCompleted;
            FacadeAd.BannerAdClicked -= BannerAdClicked;
            FacadeAd.BannerAdClosed -= BannerAdClosed;
            FacadeAd.BannerAdRevenuePaid -= BannerAdRevenuePaid;
            FacadeAd.BannerAdNotReady += BannerAdNotReady;

            FacadeAd.AppOpenAdLoaded -= AppOpenAdLoaded;
            FacadeAd.AppOpenLoadFailed -= AppOpenLoadFailed;
            FacadeAd.AppOpenDisplayed -= AppOpenDisplayed;
            FacadeAd.AppOpenDisplayFailed -= AppOpenDisplayFailed;
            FacadeAd.AppOpenCompleted -= AppOpenCompleted;
            FacadeAd.AppOpenClicked -= AppOpenClicked;
            FacadeAd.AppOpenClosed -= AppOpenClosed;
            FacadeAd.AppOpenRevenuePaid -= AppOpenRevenuePaid;
            FacadeAd.AppOpenotReady -= AppOpenotReady;

            FacadeAd.AdRefuse -= AdRefuse;
            FacadeAd.GetTotalAdwatch -= GetTotalAdwatch;
        }

        #endregion

        #region 播放广告

        /// <summary>
        /// 播放激励广告
        /// </summary>
        /// <param name="eAdSource">广告源</param>
        /// <param name="successAction">成功回调</param>
        /// <param name="failAction">失败回调</param>
        private void PlayRewardAd(EAdSource eAdSource, Action<int> successAction, Action<string> failAction, Action rewardHideAction)
        {
            rewardAdScore = eAdSource;

            if (GameDefines.ifSkipAD)
                successAction?.Invoke(1);
            else
            {
                if(!rewardSuccessActions.ContainsKey(eAdSource))
                    rewardSuccessActions.Add(eAdSource, successAction);
                else
                    rewardSuccessActions[eAdSource] = successAction;

                if (!rewardFailActions.ContainsKey(eAdSource))
                    rewardFailActions.Add(eAdSource, failAction);
                else
                    rewardFailActions[eAdSource] = failAction;

                if(FacadeAd.ShowRewardAd != null)
                {
                    FacadeAd.ShowRewardAd();
                }
                else
                {
                    failAction?.Invoke(AdFailMsg);
                    UIManager.Instance.OpenNotice2(AdFailMsg);
                }
            }
        }

        /// <summary>
        /// 播放插屏广告
        /// </summary>
        /// <param name="eAdSource">广告源</param>
        /// <param name="successAction">成功回调</param>
        /// <param name="failAction">失败回调</param>
        private void PlayInterAd(EAdSource eAdSource, Action<int> successAction, Action<string> failAction)
        {
            interstitialAdScore = eAdSource;

            if (GameDefines.ifSkipAD)
                successAction?.Invoke(1);
            else
            {
                if (!interstitialSuccessActions.ContainsKey(eAdSource))
                    interstitialSuccessActions.Add(eAdSource, successAction);
                else
                    interstitialSuccessActions[eAdSource] = successAction;

                if (!interstitialFailActions.ContainsKey(eAdSource))
                    interstitialFailActions.Add(eAdSource, failAction);
                else
                    interstitialFailActions[eAdSource] = failAction;

                if (FacadeAd.ShowInterAd != null)
                {
                    FacadeAd.ShowInterAd();
                }
                else
                {
                    failAction?.Invoke(AdFailMsg);
                    UIManager.Instance.OpenNotice2(AdFailMsg);
                }

            }
        }

        /// <summary>
        /// 播放横幅广告
        /// </summary>
        /// <param name="eAdSource">广告源</param>
        /// <param name="successAction">成功回调</param>
        /// <param name="failAction">失败回调</param>
        private void PlayBannerAd(EAdSource eAdSource, Action<int> successAction, Action<string> failAction)
        {
            bannerAdScore = eAdSource;

            if (GameDefines.ifSkipAD)
                successAction?.Invoke(1);
            else
            {
                if (!bannerSuccessActions.ContainsKey(eAdSource))
                    bannerSuccessActions.Add(eAdSource, successAction);
                if (!bannerFailActions.ContainsKey(eAdSource))
                    bannerFailActions.Add(eAdSource, failAction);

                if (FacadeAd.ShowBannerAd != null)
                {
                    FacadeAd.ShowBannerAd();
                }
                else
                {
                    failAction?.Invoke(AdFailMsg);
                    UIManager.Instance.OpenNotice2(AdFailMsg);
                }
            }
        }

        /// <summary>
        /// 取消横幅广告
        /// </summary>
        /// <param name="eAdSource">广告源</param>
        private void StopBannerAd(EAdSource eAdSource)
        {
            if (FacadeAd.CancelBannerAd != null)
            {
                FacadeAd.CancelBannerAd();
            }
            else
            {
                D.Error("Banner ad not ready");
            }
        }

        /// <summary>
        /// 播放开屏广告
        /// </summary>
        /// <param name="eAdSource">广告源</param>
        /// <param name="successAction">成功回调</param>
        /// <param name="failAction">失败回调</param>
        private void PlayAppOpenAd(EAdSource eAdSource, Action<int> successAction, Action<string> failAction)
        {
            appOpenAdScore = eAdSource;

            if (GameDefines.ifSkipAD)
                successAction?.Invoke(1);
            else
            {
                if (!appOpenSuccessActions.ContainsKey(eAdSource))
                    appOpenSuccessActions.Add(eAdSource, successAction);
                else
                    appOpenSuccessActions[eAdSource] = successAction;

                if (!appOpenFailActions.ContainsKey(eAdSource))
                    appOpenFailActions.Add(eAdSource, failAction);
                else
                    appOpenFailActions[eAdSource] = failAction;

                if (FacadeAd.ShowAppOpenAd != null)
                {
                    FacadeAd.ShowAppOpenAd();
                }
                else
                {
                    failAction?.Invoke(AdFailMsg);
                    UIManager.Instance.OpenNotice2(AdFailMsg);
                }
            }
        }

        /// <summary>
        /// 选择播放激励or插屏
        /// </summary>
        /// <param name="eAdSource">广告源</param>
        /// <param name="roi">优先播激励/插屏</param>
        /// <param name="successAction">成功回调</param>
        /// <param name="failAction">失败回调</param>
        private void PlayROIAd(EAdSource eAdSource, bool roi, Action<int> successAction, Action<string> failAction, Action rewardHideAction)
        {
            if (roi)//优先播激励
            {
                if (FacadeAd.GetRewardAdReady())//激励准备完毕
                {
                    PlayRewardAd(eAdSource, successAction, failAction, rewardHideAction);
                }
                else
                {
                    PlayInterAd(eAdSource, successAction, failAction);
                }
            }
            else////优先播插屏
            {
                if (FacadeAd.GetInterAdReady())//插屏准备完毕
                {
                    PlayInterAd(eAdSource, successAction, failAction);
                }
                else
                {
                    PlayRewardAd(eAdSource, successAction, failAction, rewardHideAction);
                }
            }
        }

        /// <summary>
        /// 根据广告收入选择播放激励or插屏
        /// </summary>
        /// <param name="eAdSource">广告源</param>
        /// <param name="successAction">成功回调</param>
        /// <param name="failAction">失败回调</param>
        private void PlayROIAdByRevenue(EAdSource eAdSource, Action<int> successAction, Action<string> failAction, Action rewardHideAction)
        {
            if(FacadeAd.GetROIAdRevenue == null)
            {
                failAction?.Invoke(AdFailMsg);
                return;
            }

            if(FacadeAd.GetROIAdRevenue())//激励在比价中胜出
            {
                if(FacadeAd.GetRewardAdReady())//激励准备完毕
                {
                    PlayRewardAd(eAdSource, successAction, failAction, rewardHideAction);
                }
                else
                {
                    PlayInterAd(eAdSource, successAction, failAction);
                }
            }
            else//插屏在比价中胜出
            {
                if (FacadeAd.GetInterAdReady())//插屏准备完毕
                {
                    PlayInterAd(eAdSource, successAction, failAction);
                }
                else
                {
                    PlayRewardAd(eAdSource, successAction, failAction, rewardHideAction);
                }
            }
        }

        /// <summary>
        /// 根据权重选择播放激励or插屏
        /// </summary>
        /// <param name="eAdSource">广告源</param>
        /// <param name="successAction">成功回调</param>
        /// <param name="failAction">失败回调</param>
        /// <param name="rewardHideAction">广告（激励）隐藏回调</param>
        /// <param name="WeightAdRange">权重范围</param>
        /// <param name="WeightAdBoundary">权重分界值</param>
        private void PlayROIAdByWeight(EAdSource eAdSource, Action<int> successAction, Action<string> failAction, Action rewardHideAction, Vector2 WeightAdRange, int WeightAdBoundary)
        {
            if(FacadeAd.GetRewardAdReady == null)//一般情况下，GetRewardAdReady和GetInterAdReady会同时赋值
            {
                failAction?.Invoke(AdFailMsg);
                return;
            }

            int randomValue = (int)UnityEngine.Random.Range(WeightAdRange.x, WeightAdRange.y);

            if (randomValue <= WeightAdBoundary)
            {
                if (FacadeAd.GetRewardAdReady())
                {
                    PlayRewardAd(eAdSource, successAction, failAction, rewardHideAction);
                }
                else
                {
                    PlayInterAd(eAdSource, successAction, failAction);
                }
            }
            else
            {
                if (FacadeAd.GetInterAdReady())
                {
                    PlayInterAd(eAdSource, successAction, failAction);
                }
                else
                {
                    PlayRewardAd(eAdSource, successAction, failAction, rewardHideAction);
                }
            }
        }

        #endregion

        #region 广告回调

        #region 通用广告回调

        /// <summary>
        /// 广告加载成功
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="eAdSource">广告来源</param>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void OnAdLoaded(EAdType eAdType, EAdSource eAdSource, string platform, double revenue, double ecpm, string precision)
        {
            D.Log($"'{eAdType}' Ad '{eAdSource}' loaded successfully");
            FacadeAd.OnAdLoad?.Invoke(eAdType, eAdSource, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 广告加载失败
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="eAdSource">广告来源</param>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void OnAdLoadFailed(EAdType eAdType, EAdSource eAdSource, string platform, string errMsg)
        {
            D.Log($"'{eAdType}' Ad '{eAdSource}' loaded failed: '{errMsg}'");
            OnAdFailed(eAdType, eAdSource, errMsg);
            FacadeAd.OnAdLoadFailed?.Invoke(eAdType, eAdSource, platform, errMsg);
        }

        /// <summary>
        /// 广告显示成功
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="eAdSource">广告来源</param>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void OnAdDisplayed(EAdType eAdType, EAdSource eAdSource, string platform, double revenue, double ecpm, string precision)
        {
            D.Log($"'{eAdType}' Ad '{eAdSource}' displayed successfully");
            FacadeAd.OnAdDisplayed?.Invoke(eAdType, eAdSource, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 广告显示失败
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="eAdSource">广告来源</param>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void OnAdDisplayFailed(EAdType eAdType, EAdSource eAdSource, string platform, string errMsg)
        {
            D.Log($"'{eAdType}' Ad '{eAdSource}' displayed failed: '{errMsg}'");
            OnAdFailed(eAdType, eAdSource, errMsg);
            FacadeAd.OnAdDisplayFailed?.Invoke(eAdType, eAdSource, platform, errMsg);
        }

        /// <summary>
        /// 广告播放完成
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="eAdSource">广告来源</param>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void OnAdCompleted(EAdType eAdType, EAdSource eAdSource, string platform, double revenue, double ecpm, string precision)
        {
            D.Log($"'{eAdType}' Ad '{eAdSource}' completed");
            FacadeAd.OnAdCompleted?.Invoke(eAdType, eAdSource, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 广告点击
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="eAdSource">广告来源</param>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void OnAdClicked(EAdType eAdType, EAdSource eAdSource, string platform, double revenue, double ecpm, string precision)
        {
            D.Log($"'{eAdType}' Ad '{eAdSource}' clicked");
            FacadeAd.OnAdClicked?.Invoke(eAdType, eAdSource, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 广告关闭
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="eAdSource">广告来源</param>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void OnAdClosed(EAdType eAdType, EAdSource eAdSource, string platform, double revenue, double ecpm, string precision)
        {
            D.Log($"'{eAdType}' Ad '{eAdSource}' closed");
            FacadeAd.OnAdClosed?.Invoke(eAdType, eAdSource, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 广告获取收入
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="eAdSource">广告来源</param>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void OnAdRevenuePaid(EAdType eAdType, EAdSource eAdSource, string platform, double revenue, double ecpm, string precision)
        {
            D.Log($"'{eAdType}' Ad '{eAdSource}' revenue paid: '{revenue}'");
            FacadeAd.OnAdRevenuePaid?.Invoke(eAdType, eAdSource, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 广告未准备完毕
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        private void OnAdNotReady(EAdType eAdType, EAdSource eAdSource)
        {
            string notReadyMsg = $"'{eAdType}' Ad not ready";
            D.Log(notReadyMsg);
            OnAdFailed(eAdType, eAdSource, notReadyMsg);
            FacadeAd.OnAdNotReady?.Invoke(eAdType, eAdSource);
        }

        /// <summary>
        /// 广告奖励发放（位置：激励——获取激励奖励、插屏——关闭插屏、横幅——点击横幅）
        /// </summary>
        /// <param name="eAdType">广告类型</param>
        /// <param name="eAdSource">广告来源</param>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        /// <param name="rewardAmount">奖励值（仅激励广告会有值，其余类型广告为0）</param>
        private void OnAdReceivedReward(EAdType eAdType, EAdSource eAdSource, string platform, double revenue, double ecpm, string precision, int rewardAmount)
        {
            D.Log($"'{eAdType}' Ad '{eAdSource}' received reward: '{rewardAmount}'");

            switch (eAdType) 
            { 
                case EAdType.Reward:
                    if (rewardSuccessActions.ContainsKey(eAdSource))
                        rewardSuccessActions[eAdSource]?.Invoke(rewardAmount);
                    else
                        D.Error($"{eAdType} Ad :'{eAdSource}' `s successful callback is null");
                    break;
                case EAdType.Interstitial:
                    if (interstitialSuccessActions.ContainsKey(eAdSource))
                        interstitialSuccessActions[eAdSource]?.Invoke(rewardAmount);
                    else
                        D.Error($"{eAdType} Ad :'{eAdSource}' `s successful callback is null");
                    break;
                case EAdType.Banner:
                    if (bannerSuccessActions.ContainsKey(eAdSource))
                        bannerSuccessActions[eAdSource]?.Invoke(rewardAmount);
                    else
                        D.Error($"{eAdType} Ad :'{eAdSource}' `s successful callback is null");
                    break;
                case EAdType.AppOpen:
                    if (appOpenSuccessActions.ContainsKey(eAdSource))
                        appOpenSuccessActions[eAdSource]?.Invoke(rewardAmount);
                    else
                        D.Error($"{eAdType} Ad :'{eAdSource}' `s successful callback is null");
                    break;
                default:
                    D.Error($"Ad receive: unknown EAdType");
                    break;
            }

            FacadeAd.OnAdReceivedReward?.Invoke(eAdType, eAdSource, platform, revenue, ecpm, precision, rewardAmount);
        }

        /// <summary>
        /// 广告失败回调
        /// </summary>
        /// <param name="eAdType"></param>
        private void OnAdFailed(EAdType eAdType, EAdSource eAdSource, string errMsg)
        {
            UIManager.Instance.OpenNotice2(AdFailMsg);

            switch (eAdType)
            {
                case EAdType.Reward:
                    if (rewardFailActions.ContainsKey(eAdSource))
                        rewardFailActions[eAdSource]?.Invoke(errMsg);
                    else
                        D.Error($"{eAdType} Ad :'{eAdSource}' `s fail callback is null");
                    break;
                case EAdType.Interstitial:
                    if (interstitialFailActions.ContainsKey(eAdSource))
                        interstitialFailActions[eAdSource]?.Invoke(errMsg);
                    else
                        D.Error($"{eAdType} Ad :'{eAdSource}' `s fail callback is null");
                    break;
                case EAdType.Banner:
                    if (bannerFailActions.ContainsKey(eAdSource))
                        bannerFailActions[eAdSource]?.Invoke(errMsg);
                    else
                        D.Error($"{eAdType} Ad :'{eAdSource}' `s fail callback is null");
                    break;
                case EAdType.AppOpen:
                    if (appOpenFailActions.ContainsKey(eAdSource))
                        appOpenFailActions[eAdSource]?.Invoke(errMsg);
                    else
                        D.Error($"{eAdType} Ad :'{eAdSource}' `s fail callback is null");
                    break;
                default:
                    D.Error($"Ad fail: unknown EAdType");
                    break;
            }

            FacadeAd.OnAdFailed?.Invoke(eAdType, eAdSource, errMsg);
        }

        #endregion

        #region 激励广告回调

        /// <summary>
        /// 激励广告加载成功
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void RewardAdLoaded(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnRewardAdLoaded?.Invoke(platform, revenue, ecpm, precision);
            OnAdLoaded(EAdType.Reward, rewardAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 激励广告加载失败
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void RewardAdLoadFailed(string platform, string errMsg)
        {
            FacadeAd.OnRewardAdLoadFailed(platform, errMsg);
            OnAdLoadFailed(EAdType.Reward, rewardAdScore, platform, errMsg);
        }

        /// <summary>
        /// 激励广告显示成功
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void RewardAdDisplayed(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnRewardAdDisplayed?.Invoke(platform, revenue, ecpm, precision);
            OnAdDisplayed(EAdType.Reward, rewardAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 激励广告显示失败
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void RewardAdDisplayFailed( string platform, string errMsg)
        {
            FacadeAd.OnRewardAdDisplayFailed(platform, errMsg);
            OnAdDisplayFailed(EAdType.Reward, rewardAdScore, platform, errMsg);
        }

        /// <summary>
        /// 激励广告播放完成
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void RewardAdCompleted(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnRewardAdCompleted?.Invoke(platform, revenue, ecpm, precision);
            OnAdCompleted(EAdType.Reward, rewardAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 激励广告点击
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void RewardAdClicked(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnRewardAdClicked?.Invoke(platform, revenue, ecpm, precision);
            OnAdClicked(EAdType.Reward, rewardAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 激励广告关闭
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void RewardAdClosed(string platform, double revenue, double ecpm, string precision)
        {
            if(rewardHideActions.ContainsKey(rewardAdScore))
                rewardHideActions[rewardAdScore]?.Invoke();

            FacadeAd.OnRewardAdClosed?.Invoke(platform, revenue, ecpm, precision);
            OnAdClosed(EAdType.Reward, rewardAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 激励广告获取收入
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void RewardAdRevenuePaid(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnRewardAdRevenuePaid?.Invoke(platform, revenue, ecpm, precision);
            OnAdRevenuePaid(EAdType.Reward, rewardAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 激励广告未准备（加载）完毕
        /// </summary>
        private void RewardAdNotReady()
        {
            FacadeAd.OnRewardAdNotReady?.Invoke();
            OnAdNotReady(EAdType.Reward, rewardAdScore);
        }

        /// <summary>
        /// 激励广告奖励发放
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        /// <param name="rewardAmount">奖励值</param>
        private void RewardAdReceivedReward(string platform, double revenue, double ecpm, string precision, int rewardAmount)
        {
            FacadeAd.OnRewardAdReceivedReward?.Invoke(platform, revenue, ecpm, precision, rewardAmount);
            OnAdReceivedReward(EAdType.Reward, rewardAdScore, platform, revenue, ecpm, precision, rewardAmount);
        }

        #endregion

        #region 插屏广告回调

        /// <summary>
        /// 插屏广告加载成功
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void InterstitialAdLoaded(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnInterstitialAdLoaded?.Invoke(platform, revenue, ecpm, precision);
            OnAdLoaded(EAdType.Interstitial, interstitialAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 插屏广告加载失败
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void InterstitialAdLoadFailed(string platform, string errMsg)
        {
            FacadeAd.OnInterstitialAdLoadFailed?.Invoke(platform, errMsg);
            OnAdLoadFailed(EAdType.Interstitial, interstitialAdScore, platform, errMsg);
        }

        /// <summary>
        /// 插屏广告显示成功
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void InterstitialAdDisplayed(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnInterstitialAdDisplayed?.Invoke(platform, revenue, ecpm, precision);
            OnAdDisplayed(EAdType.Interstitial, interstitialAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 插屏广告显示失败
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void InterstitialAdDisplayFailed(string platform, string errMsg)
        {
            FacadeAd.OnInterstitialAdDisplayFailed?.Invoke(platform, errMsg);
            OnAdDisplayFailed(EAdType.Interstitial, interstitialAdScore, platform, errMsg);
        }

        /// <summary>
        /// 插屏广告播放完成
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void InterstitialAdCompleted(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnInterstitialAdCompleted?.Invoke(platform, revenue, ecpm, precision);
            OnAdCompleted(EAdType.Interstitial, interstitialAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 插屏广告点击
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void InterstitialAdClicked(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnInterstitialAdClicked?.Invoke(platform, revenue, ecpm, precision);
            OnAdClicked(EAdType.Interstitial, interstitialAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 插屏广告关闭
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void InterstitialAdClosed(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnInterstitialAdClosed?.Invoke(platform, revenue, ecpm, precision);
            OnAdClosed(EAdType.Interstitial, interstitialAdScore, platform, revenue, ecpm, precision);
            OnAdReceivedReward(EAdType.Interstitial, interstitialAdScore, platform, revenue, ecpm, precision, 0);
        }

        /// <summary>
        /// 插屏广告获取收入
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void InterstitialAdRevenuePaid(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnInterstitialAdRevenuePaid?.Invoke(platform, revenue, ecpm, precision);
            OnAdRevenuePaid(EAdType.Interstitial, interstitialAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 插屏广告未准备（加载）完毕
        /// </summary>
        private void InterstitialAdNotReady()
        {
            FacadeAd.OnInterstitialAdNotReady?.Invoke();
            OnAdNotReady(EAdType.Interstitial, interstitialAdScore);
        }

        #endregion

        #region 横幅广告回调

        /// <summary>
        /// 横幅广告加载成功
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void BannerAdLoaded(string platform, double revenue, double ecpm, string precision)
        {
            OnAdLoaded(EAdType.Banner, bannerAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 横幅广告加载失败
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void BannerAdLoadFailed(string platform, string errMsg)
        {
            OnAdLoadFailed(EAdType.Banner, bannerAdScore, platform, errMsg);
        }

        /// <summary>
        /// 横幅广告显示成功
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void BannerAdDisplayed(string platform, double revenue, double ecpm, string precision)
        {
            OnAdDisplayed(EAdType.Banner, bannerAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 横幅广告显示失败
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void BannerAdDisplayFailed(string platform, string errMsg)
        {
            OnAdDisplayFailed(EAdType.Banner, bannerAdScore, platform, errMsg);
        }

        /// <summary>
        /// 横幅广告播放完成
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void BannerAdCompleted(string platform, double revenue, double ecpm, string precision)
        {
            OnAdCompleted(EAdType.Banner, bannerAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 横幅广告点击
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void BannerAdClicked(string platform, double revenue, double ecpm, string precision)
        {
            OnAdClicked(EAdType.Banner, bannerAdScore, platform, revenue, ecpm, precision);
            OnAdReceivedReward(EAdType.Banner, bannerAdScore, platform, revenue, ecpm, precision, 0);
        }

        /// <summary>
        /// 横幅广告关闭
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void BannerAdClosed(string platform, double revenue, double ecpm, string precision)
        {
            OnAdClosed(EAdType.Banner, bannerAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 横幅广告获取收入
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void BannerAdRevenuePaid(string platform, double revenue, double ecpm, string precision)
        {
            OnAdRevenuePaid(EAdType.Banner, bannerAdScore, platform, revenue, ecpm, precision);
            
        }

        /// <summary>
        /// 横幅广告未准备（加载）完毕
        /// </summary>
        private void BannerAdNotReady()
        {
            OnAdNotReady(EAdType.Banner, bannerAdScore);
        }

        #endregion

        #region 开屏广告回调

        /// <summary>
        /// 开屏广告加载成功
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void AppOpenAdLoaded(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnAppOpenAdLoaded?.Invoke(platform, revenue, ecpm, precision);
            OnAdLoaded(EAdType.AppOpen, appOpenAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 开屏广告加载失败
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void AppOpenLoadFailed(string platform, string errMsg)
        {
            FacadeAd.OnAppOpenLoadFailed?.Invoke(platform, errMsg);
            OnAdLoadFailed(EAdType.AppOpen, appOpenAdScore, platform, errMsg);
        }

        /// <summary>
        /// 开屏广告显示成功
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void AppOpenDisplayed(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnAppOpenDisplayed?.Invoke(platform, revenue, ecpm, precision);
            OnAdDisplayed(EAdType.AppOpen, appOpenAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 开屏广告显示失败
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="errMsg">错误信息</param>
        private void AppOpenDisplayFailed(string platform, string errMsg)
        {
            FacadeAd.OnAppOpenDisplayFailed?.Invoke(platform, errMsg);
            OnAdDisplayFailed(EAdType.AppOpen, appOpenAdScore, platform, errMsg);
        }

        /// <summary>
        /// 开屏广告播放完成
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void AppOpenCompleted(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnAppOpenCompleted?.Invoke(platform, revenue, ecpm, precision);
            OnAdCompleted(EAdType.AppOpen, appOpenAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 开屏广告点击
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void AppOpenClicked(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnAppOpenClicked?.Invoke(platform, revenue, ecpm, precision);
            OnAdClicked(EAdType.AppOpen, appOpenAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 开屏广告关闭
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void AppOpenClosed(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnAppOpenClosed?.Invoke(platform, revenue, ecpm, precision);
            OnAdClosed(EAdType.AppOpen, appOpenAdScore, platform, revenue, ecpm, precision);
            OnAdReceivedReward(EAdType.AppOpen, appOpenAdScore, platform, revenue, ecpm, precision, 0);
        }

        /// <summary>
        /// 开屏广告获取收入
        /// </summary>
        /// <param name="platform">广告平台</param>
        /// <param name="revenue">单次收入</param>
        /// <param name="ecpm">ECPM</param>
        /// <param name="precision">精度</param>
        private void AppOpenRevenuePaid(string platform, double revenue, double ecpm, string precision)
        {
            FacadeAd.OnAppOpenRevenuePaid?.Invoke(platform, revenue, ecpm, precision);
            OnAdRevenuePaid(EAdType.AppOpen, appOpenAdScore, platform, revenue, ecpm, precision);
        }

        /// <summary>
        /// 开屏广告未准备（加载）完毕
        /// </summary>
        private void AppOpenotReady()
        {
            FacadeAd.OnAppOpenotReady?.Invoke();
            OnAdNotReady(EAdType.AppOpen, appOpenAdScore);

        }

        #endregion

        #endregion

        #region 额外扩充

        /// <summary>
        /// 拒绝X次后强制弹广告
        /// </summary>
        /// <param name="eAdSource">广告源</param>
        /// <param name="successAction">成功回调</param>
        /// <param name="failAction">失败回调</param>
        /// <param name="rewardHideAction">激励隐藏回调</param>
        private void AdRefuse(EAdSource eAdSource, Action<int> successAction, Action<string> failAction, Action rewardHideAction)
        {
            curRefuseCount++;

            if (curRefuseCount >= GameDefines.AdRefuseCount)
            {
                PlayROIAdByWeight(eAdSource, (count) =>
                {
                    curRefuseCount = 0;
                    successAction?.Invoke(count);
                }, failAction, () =>
                {
                    curRefuseCount = 0;
                    rewardHideAction?.Invoke();
                }, GameDefines.WeightAdRange, GameDefines.AdWeight);
            }
            else
            {
                failAction?.Invoke("");
            }
        }

        private int GetTotalAdwatch()
        {
            return totalAdCount;
        }

        private void AddTotalAdwatch(int count, double rCount)
        {
            totalAdRevenue += rCount;
            SPlayerPrefs.SetInt(PlayerPrefDefines.totalAdRevenue, totalAdCount);
            totalAdCount += count;
            SPlayerPrefs.SetInt(PlayerPrefDefines.totalAdCount, totalAdCount);
            SPlayerPrefs.Save();

            switch (totalAdCount)
            {
                case 5:
                    //ModuleMgr.Instance.TDAnalyticsManager.Times_5_Ad(totalAdRevenue);
                    break;
                case 10:
                    //ModuleMgr.Instance.TDAnalyticsManager.Times_10_Ad(totalAdRevenue);
                    break;
                case 15:
                    //ModuleMgr.Instance.TDAnalyticsManager.Times_15_Ad(totalAdRevenue);
                    break;
                case 20:
                    //ModuleMgr.Instance.TDAnalyticsManager.Times_20_Ad(totalAdRevenue);
                    break;
            }
        }

        #endregion

        protected override void OnDispose()
        {
            RemoveFacade();
            rewardSuccessActions = null;
            interstitialSuccessActions = null;
            bannerSuccessActions = null;
            appOpenSuccessActions = null;
            rewardFailActions = null;
            interstitialFailActions = null;
            bannerFailActions = null;
            appOpenFailActions = null;
            rewardHideActions = null;
        }
    }
}
