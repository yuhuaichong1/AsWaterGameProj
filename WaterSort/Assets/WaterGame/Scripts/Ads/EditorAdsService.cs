using System;
using UnityEngine;

namespace AsGame.Ads
{
    /// <summary>编辑器/调试：激励视频直接成功，便于测道具与体力流程。</summary>
    public class EditorAdsService : IAdsService
    {
        public bool IsAdEnabled => true;

        public void Initialize() { }

        public void ShowRewardedVideo(RewardAdPlacement placement, Action<bool> onFinished)
        {
            Debug.Log($"[EditorAdsService] 模拟激励成功 placement={placement}");
            onFinished?.Invoke(true);
        }

        public void CreateRewardedVideo() { }
        public void ShowBanner(int adIndex) { }
        public void HideBanner(int adIndex) { }
        public void DestroyBanner(int adIndex) { }
        public void RefreshBanner(int adIndex) { }
        public void ShowInterstitial(int adIndex, Action onClosed = null) => onClosed?.Invoke();
        public void OnApplicationPause(bool pause) { }
    }
}
