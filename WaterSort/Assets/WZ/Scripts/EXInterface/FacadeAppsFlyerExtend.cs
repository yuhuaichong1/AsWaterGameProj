namespace WZSDK
{
    using AppLovinMax;
    using System;
    
    
    public static class FacadeAppsFlyerExtend
    {
        public static Action<int, double> SendAdSuccessCountEvent;                                   //发送广告完播事件
        public static Action<MaxSdkBase.AdInfo, EAdType> SendAdRevenue;                                    //发送广告收益事件
        public static Action<string> OnAppsFlyerIdReady;
        public static Action<bool> OnAttributionResolved;
        public static Action<bool> OnUploadPolicyResolved;
    
        public static string CurrentAppsFlyerId { get; private set; }
        public static bool HasResolvedAttribution { get; private set; }
        public static bool IsPaidUser { get; private set; }
        public static bool HasResolvedUploadPolicy { get; private set; }
        public static bool ForcedUpload { get; private set; }
    
        public static void NotifyAppsFlyerIdReady(string appsFlyerId)
        {
            if (string.IsNullOrEmpty(appsFlyerId)) return;
            if (string.Equals(CurrentAppsFlyerId, appsFlyerId, StringComparison.Ordinal)) return;
    
            CurrentAppsFlyerId = appsFlyerId;
            OnAppsFlyerIdReady?.Invoke(appsFlyerId);
        }
    
        public static void NotifyAttributionResolved(bool isPaidUser)
        {
            bool stateChanged = !HasResolvedAttribution || IsPaidUser != isPaidUser;
    
            HasResolvedAttribution = true;
            IsPaidUser = isPaidUser;
    
            if (stateChanged)
            {
                OnAttributionResolved?.Invoke(isPaidUser);
            }
        }
    
        public static void NotifyUploadPolicyResolved(bool forcedUpload)
        {
            bool stateChanged = !HasResolvedUploadPolicy || ForcedUpload != forcedUpload;
    
            HasResolvedUploadPolicy = true;
            ForcedUpload = forcedUpload;
    
            if (stateChanged)
            {
                OnUploadPolicyResolved?.Invoke(forcedUpload);
            }
        }
    
        public static bool CanUploadServerParams()
        {
#if UNITY_EDITOR
            return false;
#else
            return HasResolvedUploadPolicy && (ForcedUpload || (HasResolvedAttribution && IsPaidUser));
#endif
        }
    }
    
    public enum EAdType
    {
        EReward = 1,
        EInterstitial = 2,
        EBanner = 3,
        ERec = 4,
    }
}
