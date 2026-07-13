using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XrSDK
{
    public static class FacadeTenjinExtend
    {
        public static Action<string> SetEvent;
        public static Action<string, string> SetEvent2;
        public static Action<TenjinAdImpressionJson> AppLovinImpressionFromJSON;
    }
}
