using System;
using System.Collections.Generic;

public static class ThinkingDataDefines
{
    public static Action<string, Dictionary<string, object>> Track;                             //埋点
    public static Action<Dictionary<string, object>> UserSet;                                   //设置玩家属性
    public static Action<Dictionary<string, object>> UserSetOnce;                               //仅一次设置玩家属性
    public static Action<Dictionary<string, object>> UserAdd;                                   //增添玩家属性
}
