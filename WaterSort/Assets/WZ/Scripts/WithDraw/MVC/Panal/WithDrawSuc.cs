using System;
using UnityEngine.UI;

namespace WZSDK
{
    public class WithDrawSuc : BaseViewWithDraw
    {
        private Text moneyContent;
      
        private double money;
        private double rewardAmount;
        private int level;
        private DataModel dataModel;

        protected override void OnAwake()
        {
           // Find<Button>("PanelA/Continue").onClick.AddListener(OnButtonClick);
            Find<Button>("PanelA/bg/close").onClick.AddListener(OnButtonClick);
            moneyContent = Find<Text>("PanelA/bg/content");
      
        }

        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            level = dataModel.Level;
            money = dataModel.Money;
            rewardAmount = Math.Max(GameManagerWZ.Stage5CompensationTargetCoin - money, 0d);

            moneyContent.text = GameManagerWZ.instance.mark + rewardAmount;
      

            if (dataModel.Special != DataModel.SpecialRewardAlreadyGranted)
                GameManagerWZ.instance.AddCoin((float)rewardAmount);
        }

        public void OnButtonClick()
        {
            GameApp.viewManager.Close(ViewId);
            UIManager.instance.ShowView(EUIType.BloatProgressView);
       
        }
    }
}
