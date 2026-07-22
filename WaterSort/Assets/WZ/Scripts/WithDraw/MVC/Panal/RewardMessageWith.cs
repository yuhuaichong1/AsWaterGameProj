using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class RewardMessageWith : BaseViewWithDraw
    {

        protected override void OnAwake()
        {
            Find<Button>("Panel/close").onClick.AddListener(onCloseBtnClick);
            Find<Button>("Bottom/continue").onClick.AddListener(onCloseBtnClick);
            Find<Text>("Panel/PanelA/contentB").text = GameManagerWZ.instance.mark + "200—" + GameManagerWZ.instance.mark + "1000";
            Find<Text>("Panel/PanelA/contentC").text = GameManagerWZ.instance.mark + "1000";
        }



        protected override void HandleViewArgs(object[] args)
        {

        }
        private void onCloseBtnClick()
        {
            GameApp.viewManager.Close(ViewId);
            GameApp.viewManager.Open(ViewType.RewardMessageMoney);
        }
    }
}
