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
    public static Func<WithdrawTarget> GetCurWithdrawTarget;                            //获取当前兑现目标
    public static Action<WithdrawTarget> SetCurWithdrawTarget;                          //设置当前兑现目标
    public static Func<int> GetCurCheckInDay;                                           //获取当前签到天数
    public static Action<int> SetCurCheckInDay;                                         //设置当前签到天数
    public static Action<int> AddCurCheckInDay;                                         //添加当前签到天数
    public static Func<int> GetCurCheckLevel;                                           //获取当前签到关卡
    public static Action<int> SetCurCheckLevel;                                         //设置当前签到关卡
    public static Action<int> AddCurCheckInLevel;                                       //添加当前签到关卡
    public static Func<float> GetWTarget;                                               //获取兑现金钱目标
    public static Action SetWTarget;                                                    //设置兑现金钱目标
    public static Action<int, float> CreateOrder;                                       //创建订单
    public static Action SaveCurWithdrawalRecordItems;                                  //保存单当前订单数据
    public static Func<float> GetTotalRecordMoney;                                      //获得总可兑现金额
    public static Func<List<WithdrawalRecordItem>> GetWithdrawalRecordItems;            //获取所有兑现记录数据
    public static Func<int, WithdrawalRecordItem> GetWithdrawalRecordItemById;          //获得某条兑现记录数据
    public static Action<bool, WithdrawalRecordItem> CheckOpenUI;                       //检测应该打开UIEnterInfo还是UIConfirm
    public static Action<Action<int>, Action<double>, Action<int>> ActionByCurWTarget;  //根据当前兑现目标执行不同的方法
    public static Func<float> GetRemainTarget;                                          //获取兑现金钱目标剩余值

    public static Func<bool> GetCanWithdraw;                                            //设置当前兑现按钮是否可点击（仅WithdrawTarget == PassLevel）
    public static Action<bool> SetCanWithdraw;                                          //设置当前兑现按钮是否可点击（仅WithdrawTarget == PassLevel）

    public static Func<float> GetLuckySpinReward;                                       //获取幸运转盘金额奖励值

}
