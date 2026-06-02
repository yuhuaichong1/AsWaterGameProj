using System;

namespace AsGame.Ads
{
    /// <summary>
    /// 广告门面接口：保留 Cocos PlatformManager 全部广告能力签名，内部不播放任何第三方广告。
    /// 由 <see cref="MaxAdsServiceStub"/> 或你们自研的 Max 实现类替换。
    /// </summary>
    public interface IAdsService
    {
        bool IsAdEnabled { get; }

        void Initialize();

        /// <summary>激励视频。onFinished(true) 表示完整观看。</summary>
        void ShowRewardedVideo(RewardAdPlacement placement, Action<bool> onFinished);

        void CreateRewardedVideo();

        void ShowBanner(int adIndex);
        void HideBanner(int adIndex);
        void DestroyBanner(int adIndex);
        void RefreshBanner(int adIndex);

        void ShowInterstitial(int adIndex, Action onClosed = null);

        void OnApplicationPause(bool pause);
    }
}
