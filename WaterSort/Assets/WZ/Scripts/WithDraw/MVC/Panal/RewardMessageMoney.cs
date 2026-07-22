using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class RewardMessageMoney : BaseViewWithDraw
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
            GameApp.viewManager.Open(ViewType.RewardMessageDia);
        }
    }
}
