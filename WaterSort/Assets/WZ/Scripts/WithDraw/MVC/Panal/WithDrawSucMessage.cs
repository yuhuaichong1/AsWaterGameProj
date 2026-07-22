using UnityEngine;
using UnityEngine.UI;
using static WZSDK.Tutorial;

namespace WZSDK
{
    public class WithDrawSucMessage : BaseViewWithDraw
    {
        private int level;
        private int num;
        private double money;
        private DataModel dataModel;
        protected override void OnAwake()
        {
            Find<Button>("continue").onClick.AddListener(OnButtonClick);
        }

        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            level = dataModel.Level;
            money = dataModel.Money;
            num = dataModel.Num;
        }

        public void OnButtonClick()
        {
            GameApp.viewManager.Open(ViewType.WithDraw, dataModel);
            GameApp.viewManager.Close(ViewId);

            if (GameManagerWZ.instance.currentStage == 1 || GameManagerWZ.instance.currentStage == 2)
            {
                UIManager.instance.ShowView(EUIType.Tutorial);
                Tutorial tutorial = UIManager.instance.GetView<Tutorial>(EUIType.Tutorial);
                tutorial.currentType = TYPE.TYPE3;
                tutorial.GoToStep(0);
            }
        }


    }
}
