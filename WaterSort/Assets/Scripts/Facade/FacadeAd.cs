using System;
using UnityEngine;

public static class FacadeAd
{
    #region 播放广告

    public static Action<EAdSource, Action<int>, Action<string>, Action> PlayRewardAd;                          //播放激励广告
    public static Action<EAdSource, Action<int>, Action<string>> PlayInterAd;                                   //播放插屏广告
    public static Action<EAdSource, Action<int>, Action<string>> PlayBannerAd;                                  //播放横幅广告
    public static Action<EAdSource> StopBannerAd;                                                               //停止横幅广告
    public static Action<EAdSource, Action<int>, Action<string>> PlayAppOpenAd;                                 //播放开屏广告
    public static Action<EAdSource, bool, Action<int>, Action<string>, Action> PlayROIAd;                       //选择播放激励or插屏
    public static Action<EAdSource, Action<int>, Action<string>, Action> PlayROIAdByRevenue;                    //根据广告收入选择播放激励or插屏
    public static Action<EAdSource, Action<int>, Action<string>, Action, Vector2, int> PlayROIAdByWeight;       //根据权重选择播放激励or插屏
    public static Func<bool> GetROIAdRevenue;                                                                   //获取激励or插屏的收入并进行比较（ture时为激励）
    public static Func<bool> GetRewardAdReady;                                                                  //激励广告是否准备完毕
    public static Func<bool> GetInterAdReady;                                                                   //插屏广告是否准备完毕

    public static Action ShowRewardAd;                                                                          //播放激励广告（广告类监听）
    public static Action ShowInterAd;                                                                           //播放插屏广告（广告类监听）
    public static Action ShowBannerAd;                                                                          //播放横幅广告（广告类监听）
    public static Action CancelBannerAd;                                                                        //停止横幅广告（广告类监听）
    internal static Action ShowAppOpenAd;                                                                       //播放开屏广告（广告类监听）

    #endregion

    #region 激励广告

    public static Action<string, double, double, string> RewardAdLoaded;                                        //激励广告加载成功（广告类调用）
    public static Action<string, string> RewardAdLoadFailed;                                                    //激励广告加载失败（广告类调用）
    public static Action<string, double, double, string> RewardAdDisplayed;                                     //激励广告播放成功（广告类调用）
    public static Action<string, string> RewardAdDisplayFailed;                                                 //激励广告播放失败（广告类调用）
    public static Action<string, double, double, string> RewardAdCompleted;                                     //激励广告结束（广告类调用）
    public static Action<string, double, double, string> RewardAdClicked;                                       //激励广告点击（广告类调用）
    public static Action<string, double, double, string> RewardAdClosed;                                        //激励广告关闭（广告类调用）
    public static Action<string, double, double, string> RewardAdRevenuePaid;                                   //激励广告收入（广告类调用）
    public static Action RewardAdNotReady;                                                                      //激励广告未准备（广告类调用）
    public static Action<string, double, double, string, int> RewardAdReceivedReward;                           //激励广告获取奖励（广告类调用）

    public static Action<string, double, double, string> OnRewardAdLoaded;                                      //激励广告加载成功回调
    public static Action<string, string> OnRewardAdLoadFailed;                                                  //激励广告加载失败回调
    public static Action<string, double, double, string> OnRewardAdDisplayed;                                   //激励广告播放成功回调
    public static Action<string, string> OnRewardAdDisplayFailed;                                               //激励广告播放失败回调
    public static Action<string, double, double, string> OnRewardAdCompleted;                                   //激励广告结束回调
    public static Action<string, double, double, string> OnRewardAdClicked;                                     //激励广告点击回调
    public static Action<string, double, double, string> OnRewardAdClosed;                                      //激励广告关闭回调
    public static Action<string, double, double, string> OnRewardAdRevenuePaid;                                 //激励广告收入回调
    public static Action OnRewardAdNotReady;                                                                    //激励广告未准备回调
    public static Action<string, double, double, string, int> OnRewardAdReceivedReward;                         //激励广告获取奖励回调

    #endregion

    #region 插屏广告

    public static Action<string, double, double, string> InterstitialAdLoaded;                                  //插屏广告加载成功（广告类调用）
    public static Action<string, string> InterstitialAdLoadFailed;                                              //插屏广告加载失败（广告类调用）
    public static Action<string, double, double, string> InterstitialAdDisplayed;                               //插屏广告播放成功（广告类调用）
    public static Action<string, string> InterstitialAdDisplayFailed;                                           //插屏广告播放失败（广告类调用）
    public static Action<string, double, double, string> InterstitialAdCompleted;                               //插屏广告结束（广告类调用）
    public static Action<string, double, double, string> InterstitialAdClicked;                                 //插屏广告点击（广告类调用）
    public static Action<string, double, double, string> InterstitialAdClosed;                                  //插屏广告关闭（广告类调用）
    public static Action<string, double, double, string> InterstitialAdRevenuePaid;                             //插屏广告收入（广告类调用）
    public static Action InterstitialAdNotReady;                                                                //插屏广告未准备（广告类调用）

    public static Action<string, double, double, string> OnInterstitialAdLoaded;                                //插屏广告加载成功回调
    public static Action<string, string> OnInterstitialAdLoadFailed;                                            //插屏广告加载失败回调
    public static Action<string, double, double, string> OnInterstitialAdDisplayed;                             //插屏广告播放成功回调
    public static Action<string, string> OnInterstitialAdDisplayFailed;                                         //插屏广告播放失败回调
    public static Action<string, double, double, string> OnInterstitialAdCompleted;                             //插屏广告结束回调
    public static Action<string, double, double, string> OnInterstitialAdClicked;                               //插屏广告点击回调
    public static Action<string, double, double, string> OnInterstitialAdClosed;                                //插屏广告关闭回调
    public static Action<string, double, double, string> OnInterstitialAdRevenuePaid;                           //插屏广告收入回调
    public static Action OnInterstitialAdNotReady;                                                              //插屏广告未准备回调

    #endregion

    #region 横幅广告

    public static Action<string, double, double, string> BannerAdLoaded;                                        //横幅广告加载成功（广告类调用）
    public static Action<string, string> BannerAdLoadFailed;                                                    //横幅广告加载失败（广告类调用）
    public static Action<string, double, double, string> BannerAdDisplayed;                                     //横幅广告播放成功（广告类调用）
    public static Action<string, string> BannerAdDisplayFailed;                                                 //横幅广告播放失败（广告类调用）
    public static Action<string, double, double, string> BannerAdCompleted;                                     //横幅广告结束（广告类调用）
    public static Action<string, double, double, string> BannerAdClicked;                                       //横幅广告点击（广告类调用）
    public static Action<string, double, double, string> BannerAdClosed;                                        //横幅广告关闭（广告类调用）
    public static Action<string, double, double, string> BannerAdRevenuePaid;                                   //横幅广告收入（广告类调用）
    public static Action BannerAdNotReady;                                                                      //横幅广告未准备（广告类调用）

    public static Action<string, double, double, string> OnBannerAdLoaded;                                      //横幅广告加载成功回调
    public static Action<string, string> OnBannerAdLoadFailed;                                                  //横幅广告加载失败回调
    public static Action<string, double, double, string> OnBannerAdDisplayed;                                   //横幅广告播放成功回调
    public static Action<string, string> OnBannerAdDisplayFailed;                                               //横幅广告播放失败回调
    public static Action<string, double, double, string> OnBannerAdCompleted;                                   //横幅广告结束回调
    public static Action<string, double, double, string> OnBannerAdClicked;                                     //横幅广告点击回调
    public static Action<string, double, double, string> OnBannerAdClosed;                                      //横幅广告关闭回调
    public static Action<string, double, double, string> OnBannerAdRevenuePaid;                                 //横幅广告收入回调
    public static Action OnBannerAdNotReady;                                                                    //横幅广告未准备回调

    #endregion

    #region 开屏广告

    public static Action<string, double, double, string> AppOpenAdLoaded;                                       //开屏广告加载成功（广告类调用）
    public static Action<string, string> AppOpenLoadFailed;                                                     //开屏广告加载失败（广告类调用）
    public static Action<string, double, double, string> AppOpenDisplayed;                                      //开屏广告播放成功（广告类调用）
    public static Action<string, string> AppOpenDisplayFailed;                                                  //开屏广告播放失败（广告类调用）
    public static Action<string, double, double, string> AppOpenCompleted;                                      //开屏广告结束（广告类调用）
    public static Action<string, double, double, string> AppOpenClicked;                                        //开屏广告点击（广告类调用）
    public static Action<string, double, double, string> AppOpenClosed;                                         //开屏广告关闭（广告类调用）
    public static Action<string, double, double, string> AppOpenRevenuePaid;                                    //开屏广告收入（广告类调用）
    public static Action AppOpenotReady;                                                                        //开屏广告未准备（广告类调用）

    public static Action<string, double, double, string> OnAppOpenAdLoaded;                                     //开屏广告加载成功回调
    public static Action<string, string> OnAppOpenLoadFailed;                                                   //开屏广告加载失败回调
    public static Action<string, double, double, string> OnAppOpenDisplayed;                                    //开屏广告播放成功回调
    public static Action<string, string> OnAppOpenDisplayFailed;                                                //开屏广告播放失败回调
    public static Action<string, double, double, string> OnAppOpenCompleted;                                    //开屏广告结束回调
    public static Action<string, double, double, string> OnAppOpenClicked;                                      //开屏广告点击回调
    public static Action<string, double, double, string> OnAppOpenClosed;                                       //开屏广告关闭回调
    public static Action<string, double, double, string> OnAppOpenRevenuePaid;                                  //开屏广告收入回调
    public static Action OnAppOpenotReady;                                                                      //开屏广告未准备回调

    #endregion

    #region 通用

    public static Action<EAdType, EAdSource, string, double, double, string> OnAdLoad;                          //广告加载成功回调
    public static Action<EAdType, EAdSource, string, string> OnAdLoadFailed;                                    //广告加载失败回调
    public static Action<EAdType, EAdSource, string, double, double, string> OnAdDisplayed;                     //广告播放成功回调
    public static Action<EAdType, EAdSource, string, string> OnAdDisplayFailed;                                 //广告播放失败回调
    public static Action<EAdType, EAdSource, string, double, double, string> OnAdCompleted;                     //广告结束回调
    public static Action<EAdType, EAdSource, string, double, double, string> OnAdClicked;                       //广告点击回调
    public static Action<EAdType, EAdSource, string, double, double, string> OnAdClosed;                        //广告关闭回调
    public static Action<EAdType, EAdSource, string, double, double, string> OnAdRevenuePaid;                   //广告收入回调
    public static Action<EAdType, EAdSource> OnAdNotReady;                                                      //广告未准备回调
    public static Action<EAdType, EAdSource, string, double, double, string, int> OnAdReceivedReward;           //广告奖励回调
    public static Action<EAdType, EAdSource, string> OnAdFailed;                                                //广告失败回调(包含加载失败和未准备)

    #endregion

    #region 扩充

    public static Action<EAdSource, Action<int>, Action<string>, Action> AdRefuse;                              //拒绝X次后强制看广告
    public static Func<int> GetTotalAdwatch;                                                                    //获取总广告观看次数

    #endregion
}
