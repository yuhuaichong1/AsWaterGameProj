using System;
using System.Collections.Generic;

public static class ThinkingDataDefines
{
    public static Action<EAdType, EAdSource, double, string, string> AdStart;                   //广告开始播放
    public static Action<EAdType, EAdSource, string> AdFail;                                    //广告播放/加载失败
    public static Action<EAdType, EAdSource, double, string, string> AdComplete;                //广告完成
    public static Action<EAdType, double, string, EAdSource, string> AdRevenuePaid;             //广告收入获取
    public static Action<double> Times_5_Ad;                                                    //累计看了5次广告
    public static Action<double> Times_10_Ad;                                                   //累计看了10次广告
    public static Action<double> Times_15_Ad;                                                   //累计看了15次广告
    public static Action<double> Times_20_Ad;                                                   //累计看了20次广告
    public static Action<string> RegisterFinish;                                                //注册完成
    public static Action LoginSuccess;                                                          //登录完成
    public static Action LoadingStart;                                                          //开始加载
    public static Action EnterMainUI;                                                           //进入主界面
    public static Action<string> ButtonClick;                                                   //按钮被点击
    public static Action<int> GuideStep;                                                        //当前引导步骤
    public static Action SetFirstRegisterTime;                                                  //设置第一次注册时间
    public static Action SetFirstLoginTime;                                                     //设置第一次登录时间
    public static Action SetLastRegisterTime;                                                   //设置最后一次登录时间
    public static Action<float> SetUserMoney;                                                   //设置玩家金钱
    public static Action<int> SetLevel;                                                         //设置当前关卡等级
    public static Action<Dictionary<string, object>> SetAttributionData;                        //设置归因数据
    public static Action<int, float, float> SetAdInfo;                                          //设置广告数据

 
}
