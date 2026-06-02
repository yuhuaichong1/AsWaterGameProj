// 接入 AppLovin MAX 时：去掉文件名中的 .Template，实现下列方法，并在启动时 AdsService.SetImplementation(new MaxAdsService());
#if false
using System;
using UnityEngine;

namespace WaterSort.Ads
{
    public class MaxAdsService : IAdsService
    {
        public bool IsAdEnabled => true;

        public void Initialize()
        {
            // MaxSdk.InitializeSdk();
        }

        public void ShowRewardedVideo(RewardAdPlacement placement, Action<bool> onFinished)
        {
            // string adUnitId = MapPlacement(placement);
            // MaxSdk.ShowRewardedAd(adUnitId, ... onFinished(true/false));
            onFinished?.Invoke(false);
        }

        public void CreateRewardedVideo() { }
        public void ShowBanner(int adIndex) { }
        public void HideBanner(int adIndex) { }
        public void DestroyBanner(int adIndex) { }
        public void RefreshBanner(int adIndex) { }
        public void ShowInterstitial(int adIndex, Action onClosed = null) => onClosed?.Invoke();
        public void OnApplicationPause(bool pause) { }

        static string MapPlacement(RewardAdPlacement p) => p switch
        {
            RewardAdPlacement.GameShuffle => "YOUR_MAX_UNIT_SHUFFLE",
            RewardAdPlacement.GameUndo => "YOUR_MAX_UNIT_UNDO",
            RewardAdPlacement.GameAddBottle => "YOUR_MAX_UNIT_ADD_BOTTLE",
            RewardAdPlacement.UnlockBag => "YOUR_MAX_UNIT_UNLOCK_BAG",
            RewardAdPlacement.UnlockBottle => "YOUR_MAX_UNIT_UNLOCK_BOTTLE",
            RewardAdPlacement.HeartFull => "YOUR_MAX_UNIT_HEART_FULL",
            RewardAdPlacement.HeartInfinite => "YOUR_MAX_UNIT_HEART_INF",
            _ => ""
        };
    }
}
#endif
