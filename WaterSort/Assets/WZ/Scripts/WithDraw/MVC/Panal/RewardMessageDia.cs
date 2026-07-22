using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class RewardMessageDia : BaseViewWithDraw
    {
        protected override void OnAwake()
        {
            Find<Button>("Panel/close").onClick.AddListener(onCloseBtnClick);
            Find<Button>("Bottom/continue").onClick.AddListener(onCloseBtnClick);
        }

        protected override void HandleViewArgs(object[] args)
        {

        }

        private void onCloseBtnClick()
        {
            GameApp.viewManager.Close(ViewId);
            var singleReward = new LevelRewardStatic.RewardItem { type = 1, amount = 5 };
            UIManager.instance.ShowView(EUIType.WelcomeView, singleReward, 3);
        }
    }
}
