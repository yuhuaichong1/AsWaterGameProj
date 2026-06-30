using AppsFlyerSDK;
using System;

namespace XrSDK
{
    public static class FacadeAppsFlyerExtend
    {
        public static Action<int> SendAdSuccessCountEvent;                                                      //发送广告完播事件
        public static Action<EAppsFlyerAdType, string, string, string, MediationNetwork, string, double> SendAdRevenue;  //发送广告收益事件

        public static Action Init;
    }
}