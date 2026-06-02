using System;
using System.Collections.Generic;
using UnityEngine;

public static class FacadeTracking
{
    public static Action<Dictionary<string, object>> AdStart;                   //广告开始播放
    public static Action<Dictionary<string, object>> AdComplete;                //广告观看成功
    public static Action<Dictionary<string, object>> AdFail;                    //广告观看失败
    public static Action<Dictionary<string, object>> AdRevenuePaid;             //广告收入发放成功

    public static Action<Dictionary<string, object>> Times_5_Ad;                //5次广告收入成功时累计收入
    public static Action<Dictionary<string, object>> Times_10_Ad;               //10次广告收入成功时累计收入
    public static Action<Dictionary<string, object>> Times_15_Ad;               //15次广告收入成功时累计收入
    public static Action<Dictionary<string, object>> Times_20_Ad;               //20次广告收入成功时累计收入

    public static Action<Dictionary<string, object>> RegisterFinish;            //玩家注册成功
    public static Action<Dictionary<string, object>> LoginSuccess;              //玩家登录成功
    public static Action<Dictionary<string, object>> LoadingStart;              //游戏加载成功
    public static Action<Dictionary<string, object>> EnterMainUI;               //游戏进入了主界面

    public static Action<Dictionary<string, object>> ButtonClick;               //按钮点击
    public static Action<Dictionary<string, object>> GuideStep;                 //引导步骤

    public static Action<DateTime> SetFirstRegisterTime;                        //设置第一次注册时间
    public static Action<DateTime> SetFirstLoginTime;                           //设置第一次登录时间
    public static Action<DateTime> SetLastLoginTime;                            //设置最后一次登录时间

    public static Action<float> SetUserMoney;                                   //设置玩家当前金额
    public static Action<int> SetLevel;                                         //设置玩家当前关卡

    public static Action<Dictionary<string, object>> SetAttributionData;        //获取设置归因信息
    public static Action<int, float, float> SetAdInfo;                          //设置玩家广告相关数据
}
