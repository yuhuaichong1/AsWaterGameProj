using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class SuccessMessage : BaseViewWithDraw
    {
        private Text titleContent;
        private Text greenBallContent;
        private int level;
        private int num;
        private double money;
        private DataModel dataModel;
        protected override void OnAwake()
        {
            Find<Button>("continue").onClick.AddListener(OnButtonClick);
            titleContent = Find<Text>("dial/title/content");
            greenBallContent = Find<Text>("dial/Panel/PanelA/GreenBall/content");
        }

        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            level = dataModel.Level;
            money = dataModel.Money;
            num = dataModel.Num;
            titleContent.text = string.Format(LocalizationManager.Instance.GetText("2037"), level);
            greenBallContent.text = string.Format(LocalizationManager.Instance.GetText("2101"), level);
        }

        public void OnButtonClick()
        {
            GameApp.viewManager.Open(ViewType.WithDrawSucMessage, dataModel);
            GameApp.viewManager.Close(ViewId);
        }
    }
}
