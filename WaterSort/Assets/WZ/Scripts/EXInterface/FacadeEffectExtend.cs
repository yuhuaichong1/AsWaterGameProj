using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WZSDK
{
    public static class FacadeEffectExtend
    {

        public static Action<ERewardItemStruct[], Action> PlayGetRewardEffectHandle;  //播放获取奖励特效
        public static Action PlayCongratulationEffectHandle;                          //播放祝贺特效
        public static Action<Transform, int, float, Action, ERewardType> PlayFlyMoneyHandle;       //播放飞钱特效
        public static Action<float, ERewardType> PlayFlyMoneyTipHandle;                            //播放飞钱提示特效


    }
}