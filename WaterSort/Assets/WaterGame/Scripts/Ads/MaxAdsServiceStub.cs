using System;
using UnityEngine;

namespace AsGame.Ads
{
    /// <summary>
    /// AppLovin MAX 占位实现：不加载、不展示任何广告。
    /// 接入 MAX 时：复制此类为 MaxAdsService，在 ShowRewardedVideo 等中调用你们的 SDK。
    /// </summary>
    public class MaxAdsServiceStub : IAdsService
    {
        public bool IsAdEnabled => true;

        public void Initialize()
        {
            Debug.Log("[MaxAdsServiceStub] Initialize — 请替换为 AppLovin MAX 初始化");
        }

        public void ShowRewardedVideo(RewardAdPlacement placement, Action<bool> onFinished)
        {
            Debug.Log($"[MaxAdsServiceStub] ShowRewardedVideo placement={placement} — 未接入 MAX，回调 false");
            onFinished?.Invoke(false);
        }

        public void CreateRewardedVideo()
        {
            Debug.Log("[MaxAdsServiceStub] CreateRewardedVideo — 预加载占位");
        }

        public void ShowBanner(int adIndex) =>
            Debug.Log($"[MaxAdsServiceStub] ShowBanner index={adIndex}");

        public void HideBanner(int adIndex) =>
            Debug.Log($"[MaxAdsServiceStub] HideBanner index={adIndex}");

        public void DestroyBanner(int adIndex) =>
            Debug.Log($"[MaxAdsServiceStub] DestroyBanner index={adIndex}");

        public void RefreshBanner(int adIndex) =>
            Debug.Log($"[MaxAdsServiceStub] RefreshBanner index={adIndex}");

        public void ShowInterstitial(int adIndex, Action onClosed = null)
        {
            Debug.Log($"[MaxAdsServiceStub] ShowInterstitial index={adIndex}");
            onClosed?.Invoke();
        }

        public void OnApplicationPause(bool pause) { }
    }
}
