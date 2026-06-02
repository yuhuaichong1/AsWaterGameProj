using System;
using UnityEngine;

namespace AsGame.Ads
{
    /// <summary>全局广告入口，对应 Cocos PlatformManager 广告相关方法。</summary>
    public static class AdsService
    {
        static IAdsService _impl;

        public static IAdsService Instance
        {
            get
            {
                if (_impl == null)
                    _impl = CreateDefault();
                return _impl;
            }
        }

        public static void SetImplementation(IAdsService service) => _impl = service;

        static IAdsService CreateDefault()
        {
#if UNITY_EDITOR
            return new EditorAdsService();
#else
            return new MaxAdsServiceStub();
#endif
        }

        public static void Initialize() => Instance.Initialize();

        public static void ShowRewarded(RewardAdPlacement placement, Action<bool> onFinished)
        {
            if (!Instance.IsAdEnabled)
            {
                onFinished?.Invoke(true);
                return;
            }

            Instance.ShowRewardedVideo(placement, onFinished);
        }

        public static void ShowInterstitial(int adIndex = 0, Action onClosed = null) =>
            Instance.ShowInterstitial(adIndex, onClosed);
    }
}
