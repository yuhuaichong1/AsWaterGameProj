using System;
using System.Collections.Generic;

public static class FacadeWithdraw
{
    public static Func<string> GetWName;                                                //获取兑现名称
    public static Action<string> SetWName;                                              //设置兑现名称
    public static Func<string> GetWPhoneOrEmail;                                        //获取兑现信息
    public static Action<string> SetWPhoneOrEmail;                                      //设置兑现信息
    public static Func<EPayType> GetPayType;                                            //获取兑现信息类型
    public static Action<EPayType> SetPayType;                                          //设置兑现信息类型
    public static Func<int, float, WithdrawalRecordItem> CreateOrder;                   //创建订单
    public static Action SaveCurWithdrawalRecordItems;                                  //保存单当前订单数据
    public static Func<float> GetTotalRecordMoney;                                      //获得总可兑现金额
    public static Func<List<WithdrawalRecordItem>> GetWithdrawalRecordItems;            //获取所有兑现记录数据
    public static Func<int, WithdrawalRecordItem> GetWithdrawalRecordItemById;          //获得某条兑现记录数据
    public static Action<bool, WithdrawalRecordItem> CheckOpenUI;                       //检测应该打开UIEnterInfo还是UIConfirm

    public static Func<float> GetLuckySpinReward;                                       //获取幸运转盘金额奖励值
    public static Func<float> GetLuckyReward;                                           //获取幸运奖励金额奖励值
    public static Func<float> GetLevelComplateReward;                                   //获取通关奖励金额奖励值

    public static Func<string> GetWithdrawHighValueStr;                                 //获得兑现提示区间文本

    public static Action OpenTotalRewardUI;                                             //决定现在应该打开哪个TotalReward界面UI
    public static Func<int> GetCurTRDay;                                                //获取当前总奖励时间
    public static Func<int> GetRemainTRDay;                                             //获取剩余总奖励时间
    public static Action<int> SetCurTRDay;                                              //设置当前总奖励时间
    public static Action<TrStatus> SetTrStatus;                                         //设置当前总奖励的类型
    public static Action<bool> RefushWaitDay;                                           //刷新等待时间

    public static Action<EPayType, string> SetWPhoneOrEmail2;                           //设置兑现渠道的信息
    public static Func<EPayType, string> GetWPhoneOrEmail2;                             //获取兑现渠道的信息
}
