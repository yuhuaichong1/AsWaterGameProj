using UnityEngine.UI;
namespace WZSDK
{
    public class WithDrawQues : BaseViewWithDraw
    {
        protected override void OnAwake()
        {
            Find<Button>("PanelA/Confirm").onClick.AddListener(OnButtonClick);
            Find<Button>("PanelA/close").onClick.AddListener(OnCloseBtnClick);
        }

        public void OnButtonClick()
        {
            GameApp.viewManager.Close(ViewId);
        }

        public void OnCloseBtnClick()
        {
            GameApp.viewManager.Close(ViewId);
        }
    }
}