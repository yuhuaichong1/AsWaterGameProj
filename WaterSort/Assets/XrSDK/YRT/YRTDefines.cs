using System;
using System.Collections.Generic;

public static class YRTDefines
{
    public static Func<Dictionary<string, object>> GetAttributionInfo;
    public static Func<bool> GetInitSuccess;

    public static Action<int> GuideStepFinish;
    public static Action EnterMainUI;
    public static Action LoginSuccess;
    public static Action RegisterFinish;
}
