using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class WithDrawConfirm : BaseViewWithDraw
    {
        //private GameObject Name;
        private GameObject Content;
        private Image method;
 
        private DataModel dataModel;
        protected override void OnAwake()
        {
        
            Content = Find("PanelA/PanelC/phone");
            Find<Button>("PanelA/ReEnter").onClick.AddListener(OnReturnClick);
            Find<Button>("PanelA/Confirm").onClick.AddListener(OnCloseBtnClick);
            Find<Button>("PanelA/close").onClick.AddListener(OnCloseBtnClick);
            method = Find<Image>("PanelA/method");
        }

        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args != null && args.Length > 0 ? args[0] as DataModel : null;
            if (dataModel == null)
                dataModel = new DataModel();
            if (!string.IsNullOrEmpty(dataModel.Phone))
            {
                Content.GetComponent<Text>().text = dataModel.Phone;
            }
            else
            {
                Content.GetComponent<Text>().text = dataModel.Email;
            }
    
 
            int index = dataModel.Index;

            if (index == 3)
            {
             /*   var sprite = Resources.Load<Sprite>("Prefab/UI/Pay/paypay");
                method.sprite = sprite;*/
            }
            else if (index == 2)
            {
                var sprite = Resources.Load<Sprite>("Prefab/UI/Pay/zelle");
                method.sprite = sprite;
            }
            else
            {
                var sprite = Resources.Load<Sprite>("Prefab/UI/Pay/venmo");
                method.sprite = sprite;
            }
        }



        public void OnReturnClick()
        {
            GameApp.viewManager.Open(ViewType.WithDrawMethod, dataModel);
            GameApp.viewManager.Close(ViewId);
        }


        public void OnCloseBtnClick()
        {
            if (ShouldCloseWithoutAdvance())
            {
                GameApp.viewManager.Close(ViewId);
                // Stage3 Mission 绑邮箱：确认后回到 Mission，再次点击才进 Feedback。
                EnsureWithdrawMissionVisibleAfterStage3Bind();
                return;
            }

            if (ShouldOpenProcessView())
            {
                GameApp.viewManager.Close(ViewId);
                GameApp.viewManager.Open(ViewType.WithDrawProcess1, dataModel);
                return;
            }

            if (dataModel.CloseType == 1)
            {
                StartCoroutine(DelayedNextLevel());
            }
            else
            {
                GameApp.viewManager.Close(ViewId);
                GameApp.viewManager.Open(ViewType.WithDrawContinue, dataModel);
            }
        }

        private static void EnsureWithdrawMissionVisibleAfterStage3Bind()
        {
            if (UIManager.instance == null)
                return;

            if (!UIManager.instance.HasActiveView(EUIType.WithdrawMissionView))
                UIManager.instance.ShowView(EUIType.WithdrawMissionView);
        }

        private bool ShouldOpenProcessView()
        {
            return dataModel != null
                   && (dataModel.Special == DataModel.SpecialLuckyWalletWithdraw || dataModel.RetryWithdraw);
        }

        private bool ShouldCloseWithoutAdvance()
        {
            return dataModel == null
                   || dataModel.CloseWithoutAdvance
                   || dataModel.Special == DataModel.SpecialWithdrawMissionBindEmail
                   || dataModel.Special == DataModel.SpecialLuckyWalletWithdraw
                   || dataModel.RetryWithdraw
                   || GameManagerWZ.instance == null;
        }

        private System.Collections.IEnumerator DelayedNextLevel()
        {
            yield return new WaitForSeconds(0.2f);
            var gm = GameManagerWZ.instance;
            if (gm != null)
            {
                if (gm.Stage2CashGuidePending || gm.DeferHostLevelLoad
                    || (dataModel != null && dataModel.Num == 2))
                {
                    GameApp.viewManager.Close(ViewId);
                    gm.FinishStage2CashGuideAndShowMission();
                    yield break;
                }

                gm.NextLevel();
            }
            GameApp.viewManager.Close(ViewId);
        }
    }
}
