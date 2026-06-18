using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerPrefDefines
{
    private static string GetKey(string baseKey)
    {
        if (GameDefines.ifIAA)
        {
            return $"IAA_{baseKey}";
        }
        return baseKey;
    }

    #region AudioModule

    public static string musicToggle => GetKey("WS_musicToggle");                //AudioModule_音乐开关
    public static string soundToggle => GetKey("WS_soundToggle");                //AudioModule_音效开关
    public static string vibrateToggle => GetKey("WS_vibrateToggle");            //AudioModule_震动开关

    #endregion

    #region PlayerModule

    public static string money => GetKey("WS_money");                            //PlayerModule_玩家金钱数
    public static string diamond => GetKey("WS_diamond");                        //PlayerModule_玩家钻石数
    public static string energy => GetKey("WS_energy");                          //PlayerModule_玩家体力
    public static string level => GetKey("WS_level");                            //PlayerModule_当前关卡
    public static string prop1Num => GetKey("WS_prop1Num");                      //PlayerModule_“A”道具数量
    public static string prop2Num => GetKey("WS_prop2Num");                      //PlayerModule_“B”道具数量
    public static string prop3Num => GetKey("WS_prop3Num");                      //PlayerModule_“C”道具数量
    public static string userLevel => GetKey("WS_userLevel");                    //PlayerModule_玩家等级
    public static string userExp => GetKey("WS_userExp");                        //PlayerModule_玩家当前经验
    public static string userName => GetKey("WS_userName");                      //PlayerModule_玩家姓名
    public static string userID => GetKey("WS_userID");                          //PlayerModule_玩家ID
    public static string lastRecoverTime => GetKey("WS_lastRecoverTime");         //PlayerModule_上次体力恢复时间

    #endregion

    #region WithdrawModule

    public static string withdrawalCoin => GetKey("WS_withdrawalCoin");          //WithdrawModule_玩家累计兑现金额
    public static string wName => GetKey("WS_wName");                            //WithdrawModule_兑现姓名
    public static string wPhoneOrEmail => GetKey("WS_wPhoneOrEmail");            //WithdrawModule_兑现信息
    public static string poeType => GetKey("WS_poeType");                        //WithdrawModule_兑现信息类型
    public static string isEarning => GetKey("WS_isEarning");                    //WithdrawModule_是否处于争取指定金额兑现过程中
    public static string wrisTemp => GetKey("WS_wrisTemp");                      //WithdrawModule_兑现记录
    public static string curWithdrawTarget => GetKey("WS_curWithdrawTarget");    //WithdrawModule_当前兑现目标
    public static string curCheckInDay => GetKey("WS_curCheckInDay");            //WithdrawModule_当前签到天数
    public static string curCheckInLevel => GetKey("WS_curCheckInLevel");        //WithdrawModule_当前签到关卡
    public static string wTarget => GetKey("WS_wTarget");                        //WithdrawModule_当前兑现金额目标
    public static string canWithdraw => GetKey("WS_canWithdraw");                //WithdrawModule_当前是否可兑现

    #endregion

    #region GamePlayModule

    public static string ifContinue => GetKey("WS_ifContinue");                             //GamePlayModule_是否继续上一局游戏（数据持久化判断）

    public static string showUITip1 => GetKey("WS_showUITip1");                              //GanePlayModule_展示提示UI
    public static string showUITip2 => GetKey("WS_showUITip2");                              //GanePlayModule_展示提示UI2
    public static string showUITip3 => GetKey("WS_showUITip3");                              //GanePlayModule_展示提示UI3

    #endregion

    #region GuideModule

    public static string curStep => GetKey("WS_curStep");                                   //GuideModule_当前引导步骤
    public static string ifTutorial => GetKey("WS_ifTutorial");                             //GuideModule_是否处于引导状态

    #endregion

    #region AdModule

    public static string totalAdCount => GetKey("WS_totalAdCount");                          //AdModule_总广告次数
    public static string totalAdRevenue => GetKey("WS_totalAdRevenue");                      //AdModule_总广告收入

    #endregion
}
