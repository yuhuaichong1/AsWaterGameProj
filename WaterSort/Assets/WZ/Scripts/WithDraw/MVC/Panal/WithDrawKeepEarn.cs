using UnityEngine.UI;
namespace WZSDK
{
    public class WithDrawKeepEarn : BaseViewWithDraw
    {
        private Text moneyContent;
        private double money;
        private int level;
        private DataModel dataModel;
        protected override void OnAwake()
        {
            Find<Button>("PanelA/Continue").onClick.AddListener(OnButtonClick);
            Find<Button>("PanelA/close").onClick.AddListener(OnCloseBtnClick);
            moneyContent = Find<Text>("PanelA/PanelC/content");
        }

        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            level = dataModel.Level;
            money = dataModel.Money;
            moneyContent.text = GameManagerWZ.instance.mark + money;
        }

        public void OnButtonClick()
        {
            GameApp.viewManager.Open(ViewType.WithDrawContinue, dataModel);
            GameApp.viewManager.Close(ViewId);
        }

        public void OnCloseBtnClick()
        {
            GameApp.viewManager.Close(ViewId);
        }
    }
}
