using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public static class FacadeGamePlayExtend
    {
        public static Action<int> OnCountMoveChangeHandle;
        public static Action<int> OnCountRollbackChangeHandle;
        public static Action<int> OnCountRefreshChangeHandle;

        public static Action OnAddSpacePassHandle;

        public static Action OnMinuteChangeHandle;


        public static Func<RectTransform> GetGamePlayMoneyRectHandle;
        public static Func<RectTransform> GetGamePlayDiamondRectHandle;
        public static Func<RectTransform> GetGamePlayMovePropRectHandle;
        public static Func<RectTransform> GetGamePlayRollbackPropRectHandle;
        public static Func<RectTransform> GetGamePlayRefreshPropRectHandle;
        public static Action<int, bool> ChangeCountBoolHandle;

        public static Action OnMoneyFlyFinishHandle;
        public static Action OnDiamondFlyFinishHandle;

        public static Action ShowCompleteMaskHandle;
        public static Action OpenWidthdrawTipHandle;

        //GamePlayModule
        public static Action<int> CreateLevelHandle;                                                              //创建关卡

        public static Action OnTouchStartHandle;                                                                  //点击判断逻辑
        public static Action<Collision2D> OnBeginContactHandle;                                                   //板子碰到检测区（去除板子）
        public static Action Func_INCREASEHandle;                                                                 //增添空格功能
        public static Action<bool> Func_CLEARHandle;                                                              //清除功能
        public static Action Func_HAMMERHandle;                                                                   //锤子功能（阻挡层触摸处理（用于击碎板块））
        public static Action RePlayHandle;                                                                        //重新开始游戏
        public static Action<int> SavePaintingDataHandle;                                                         //保存画数据

        //UIGamePlay
        public static Action<float> SetCurMoneyShowHandle;                                                        //设置当前金钱数的显示
        public static Action<float> SetBarValueHandle;                                                            //设置进度条的值
        public static Func<Transform> GetSaoBaHandle;                                                             //获取扫把特效物体
        public static Func<RectTransform> GetTargetsNodeHandle;                                                   //获取关卡生成父节点
        public static Func<RectTransform> GetLevelRootHandle;                                                     //获取线轴父节点
        public static Func<Transform> GetHideTmpNodeHandle;                                                       //获取临时线存放父节点
        public static Func<Transform> GetTmpNodeHandle;                                                           //获得隐藏临时线父节点
        public static Func<Transform> GetCMDialogTextTransHandle;                                                 //获取关卡目标提示框位置
        public static Action<int> SetBATCountShowHandle;                                                          //设置道具1“添加额外格子”的显示
        public static Action<int> SetBCTCountShowHandle;                                                          //设置道具2“清理”的显示
        public static Action<int> SetBRBCountShowHandle;                                                          //设置道具3“锤子”的显示
        public static Action<bool> SetBlockLayerActiveHandle;                                                     //设置阻挡按钮层的显影（辅助锤子功能）
        public static Func<Dictionary<ERewardType, Vector3>> GetFlyObjGoldHandle;                                 //获取飞行物体特效的终点
        public static Action<bool> SetCurLevelTextHandle;                                                         //设置当前关卡的显示
        public static Action SetWithdrawalTipHandle;                                                              //设置当前兑现通知
        public static Action CheckWLProgressActiveHandle;                                                         //检测是否应该显示关卡进度
        public static Action<bool> SetCanClickCashBtnHandle;                                                        //=====!临时的让兑现按钮只能在引导后点击一次!=====

        //PaintBoard
        public static Func<RectTransform> GetGridContainerHandle;                                                 //获取编制图片父对象
        public static Func<int, int, Vector3> GetCellPositionHandle;                                              //获取编制图片子对象位置
        public static Func<int, Action<Vector2Int, bool>, (float duration, bool isFinished)> DrawWithColorHandle; //填充子对象

 
        public static Action<int> FixHistoryHandle;                                                               //?
        public static Action<int> DrawQuickHandle;                                                                //快速绘制图片，用于辅助持久化
        public static Action ClearAllCellsHandle;                                                                 //清除图案

        //TargetModule
     
        public static Action OnUnlockTmpHandle;                                                                   //添加新的额外格子  


        public static Func<int, string> GetLevelTxtHandle;                                                          //获取当前关卡设置的文本
        public static Func<int, bool> GetIsInSmallLevelGroupHandle;                                                 //判断是否在小关卡组内

        public static Action ReSetRefuseCountHandle;                                                              //重置强制看广告次数

        public static Func<bool> GetIsVIPHandle;                                                                  //获取是否应该出现VIP界面
        public static Action<bool> SetIsVIPHandle;                                                                //设置是否应该出现VIP界面

        public static Func<bool> GetFirstAddHandle;                                                               //获取是否是第一次生成目标金额订单
        public static Action<bool> SetFirstAddHandle;                                                             //设置是否是第一次生成目标金额订单

        public static Action<int> CheckGuideHandle;                                                               //检测引导和加载关卡的先后顺序问题

        public static Action<bool> SetGetBrb_SpineBgState;                                                        //设置锤子按钮的状态
        public static Func<int, List<Vector3>> GetSpoolPos;                                                       //根据剩余线轴数量
        public static Func<List<Transform>> GetLockSpool;                                                         //获取未解锁的线轴的Transform
        public static Action ReSetLockSpool;                                                                      //重置未解锁的线轴
        public static Func<string> GetLevelProgressText;                                                          //获取当前进度（UIGamePlay上的值）
        public static Func<string> GetCurMoneyTextText;                                                           //获取当前金额（UIGamePlay上的值）
        public static Func<string> GetCMDialogTextText;                                                           //获取当前兑现提示（UIGamePlay上的值）
    }
}
