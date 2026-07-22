using AsGame.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public static class FacadeGamePlay
    {
        #region GamePlayModule

        public static Action StartLevel;                                                //开始关卡
        public static Action CreateLevel;                                               //创建关卡
        public static Action Func_Porp1;                                                //功能1
        public static Action Func_Porp2;                                                //功能2
        public static Action Func_Porp3;                                                //功能3
        public static Action RePlay;                                                    //重新开始游戏
        public static Func<GameStatus> GetStatus;                                       //获取当前关卡状态
        public static Action EndPorp1;                                                  //结束Prop1功能
        public static Action ReStartLRTimer;                                            //重启幸运奖励计时器
        public static Func<string> GetLevelProgress;                                    //获取当前关卡进度

        public static Action IfLevelGuide;//为了适配GM而写，不需请删
        #endregion

        #region UIGamePlay

        public static Action SetCurMoneyShow;                                           //设置当前金钱数的显示
        public static Func<Vector3> GetCMDialogTextPos;                                 //获取关卡目标提示框位置
        public static Func<RectTransform> GetCashOutBtnRect;                            //获取 Cash Out(CMBtn) 区域
        public static Action SetProp1CountShow;                                         //设置道具1“添加额外格子”的显示
        public static Action SetProp2CountShow;                                         //设置道具2“清理”的显示
        public static Action SetProp3CountShow;                                         //设置道具3“锤子”的显示
        public static Func<Dictionary<ERewardType, Vector3>> GetFlyObjGoalPos;          //获取飞行物体特效的终点
        public static Action SetWithdrawalTip;                                          //设置当前兑现通知
        public static Action<bool> SetShuffleTipShow;                                   //设置刷新功能的提示的显影

        public static Func<Transform> GetCupPart;                                       //获取瓶子父物体
        public static Func<Transform> GetCupPartShadow;                                 //获取瓶子阴影父物体
        public static Func<Transform> GetPockets;                                       //获取包装袋父物体

        public static Action<bool> AbleProp1Btn;                                        //是否启用功能1的按钮
        public static Action<bool> AbleProp2Btn;                                        //是否启用功能2的按钮
        public static Action<bool> AbleProp3Btn;                                        //是否启用功能3的按钮

        public static Action SetLevelShow;                                              //设置关卡目标

        public static Action ScrollingTipAnim;                                          //显示滑动动画

        #endregion
    }
}
