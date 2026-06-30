using System;
using static MaxSdkBase;

// Max SDK 接口定义
public static class FacadeMaxSdkExtend
{
    #region 插屏广告
    public static Action ShowInterstitialAd;                                                  //展示插屏广告

    public static Action<string, AdInfo> OnInterstitialAdLoadedEvent;                         //插屏广告加载成功
    public static Action<string, ErrorInfo> OnInterstitialAdLoadFailedEvent;                  //插屏广告加载失败
    public static Action<string, AdInfo> OnInterstitialAdDisplayedEvent;                      //插屏广告展示成功
    public static Action<string, ErrorInfo, AdInfo> OnInterstitialAdFailedToDisplayEvent;     //插屏广告展示失败
    public static Action<string, AdInfo> OnInterstitialAdDismissedEvent;                      //插屏广告被隐藏
    public static Action<string, AdInfo> OnInterstitialAdClickedEvent;                        //插屏广告被点击
    public static Action<string, AdInfo> OnInterstitialAdRevenuePaidEvent;                    //插屏广告奖励给予奖励完毕
    public static Action OnInterstitialAdNotReady;                                            //插屏广告未准备完毕

    public static Func<double> GetInterAdRevenue;                                             //获取插屏广告的ecpm
    public static Func<bool> GetInterstitialReady;                                            //获取插屏广告是否准备完成


    #endregion

    #region 激励视频
    public static Action ShowRewardedAd;                                                    //展示激励视频

    public static Action<string, AdInfo> OnRewardedAdLoadEvent;                             //激励广告加载成功
    public static Action<string, ErrorInfo> OnRewardedAdLoadFailEvent;                      //激励广告加载失败
    public static Action<string, AdInfo> OnRewardedAdDisplayEvent;                          //激励广告展示成功
    public static Action<string, ErrorInfo, AdInfo> OnRewardedAdFailToDisplayEvent;         //激励广告展示失败
    public static Action<string, AdInfo> OnRewardAdClickEvent;                              //激励广告被点击
    public static Action<string, AdInfo> OnRewardAdHiddenEvent;                             //激励广告被隐藏
    public static Action<string, Reward, AdInfo> OnRewardAdReceivRewardEvent;               //激励广告展示完毕，给予奖励
    public static Action<string, AdInfo> OnRewardAdRevenuePaidedEvent;                      //激励广告奖励收入已支付
    public static Action OnRewardAdNotReadyEvent;                                           //激励广告未准备完成回调

    public static Func<double> GetRewardedAdRevenue;                                        //获取激励视频的ecpm
    public static Func<bool> GetRewardedReady;                                              //获取激励广告是否准备完成

    #endregion

    #region 横幅广告
    public static Action ToggleBannerVisibilityEvent;                                            //横幅广告展示or关闭
    public static Action BannerAdLoadEvent;                                               //横幅广告加载失败
    public static Action<string, MaxSdkBase.ErrorCode> BannerAdFailEvent;                 //横幅广告加载失败


    #endregion

    #region MREC
    public static Action ToggleMRecVisibility;                                              //展示MREC广告
    public static Action MRecAdLoadEvent;                                                 //MREC广告加载完成
    public static Action<string, MaxSdkBase.ErrorCode> MRecAdFailEvent;                   //MREC广告加载失败
    #endregion

    #region 开屏广告

    public static Action ShowAppOpenAd;

    public static Action<string, AdInfo> OnAppOpenAdLoadedEvent;                            //插屏广告加载成功
    public static Action<string, ErrorInfo> OnAppOpenAdLoadFailedEvent;                     //插屏广告加载失败
    public static Action<string, AdInfo> OnAppOpenAdDisplayedEvent;                         //插屏广告展示成功
    public static Action<string, ErrorInfo, AdInfo> OnAppOpenAdDisplayFailedEvent;          //插屏广告展示失败
    public static Action<string, AdInfo> OnAppOpenAdHiddenEvent;                            //插屏广告被隐藏
    public static Action<string, AdInfo> OnAppOpenAdClickedEvent;                           //插屏广告被点击
    public static Action<string, AdInfo> OnAppOpenAdRevenuePaidEvent;                       //插屏广告奖励给予奖励完毕
    public static Func<bool> OnAppOpenAdNotReady;                                           //插屏广告未准备完毕

    //public static Func<double> GetAppOpenAdRevenue;                                         //获取插屏广告的ecpm
    public static Func<bool> GetAppOpenAdReady;                                             //获取插屏广告是否准备完成

    #endregion

    public static Func<bool> GetParity;                                                 //比较激励和插屏的收入（加载时）
}
