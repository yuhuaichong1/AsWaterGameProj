using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class WithDraw : BaseViewWithDraw
    {
        private const double MinimumCashOutAmount = 0.1d;

        private Text titleContent;
        private Text moneyContent;
        private Text numContent;
        private Text withdrawProcess;

        private Text Tips;
        private Text CurLuckyCoin;

        private Image process;
        private GameObject panelE;
        private int level;
        private int num;
        private double money;
        private DataModel dataModel;
        private bool flag = false;
        private Button CashOut;
        protected override void OnAwake()
        {
            Find<Button>("Panel/PanelB/WithDraw").onClick.AddListener(OnButtonClick);
            Find<Button>("Panel/close").onClick.AddListener(onCloseBtnClick);
        
            Find<Button>("Panel/PanelB/RecordBtn").onClick.AddListener(OnRecordBtnClickHandle);
            CashOut = Find<Button>("Panel/PanelC/CashOut");
                  titleContent = Find<Text>("Panel/title");
            moneyContent = Find<Text>("Panel/PanelA/PanelD/PanelE/content");
            Tips = Find<Text>("Panel/PanelC/Tips");
            CurLuckyCoin = Find<Text>("Panel/PanelC/CurLuckyCoin");

            numContent = Find<Text>("Panel/PanelA/PanelD/Process/num");
            process = Find<Image>("Panel/PanelA/PanelD/Process/Fill");
            panelE = Find("PanelE");
            withdrawProcess = Find<Text>("Panel/PanelB/withdrawProcess");
            CashOut.onClick.AddListener(onCashOutBtnClick);
        }

        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            level = dataModel.Level;
            money = dataModel.Money;
            num = dataModel.Num;
            flag = false;
            panelE.SetActive(false);
     
            var stageData = UserDataManager.Instance.getStageData(num);
            if (stageData == null)
                return;

            var day = GameManagerWZ.instance.getLoginDay();
            if (day == 0)
            {
                day = 1;
            }
            float value = float.Parse((string)stageData["Value"]);
            int type = int.Parse((string)stageData["Type"]);
            string str = "";

            if (type == 1)
            {
                numContent.text = level + "/" + value;
                SetProcessFillAmount(level, value);
                str = string.Format(LocalizationManager.Instance.GetText("2086"), value);
                if (level >= value)
                {
                    flag = true;
                }
            }
            else if (type == 2)
            {
                numContent.text = money + "/" + value;
                SetProcessFillAmount((float)money, value);
                str = string.Format(LocalizationManager.Instance.GetText("2091"), value);
                if (money >= value)
                {
                    flag = true;
                }
            }
            else if (type == 3)
            {
                numContent.text = day + "/" + value;
                SetProcessFillAmount(day, value);
                str = string.Format(LocalizationManager.Instance.GetText("2103"), value);
                if (day >= value)
                {
                    flag = true;
                }
            }
            else if (type == 4)
            {
                int adWatchCount = GameManagerWZ.instance.GetStageAdWatchCount();
                numContent.text = adWatchCount + "/" + value;
                SetProcessFillAmount(adWatchCount, value);
                str = string.Format(LocalizationManager.Instance.GetText("2148"), value);
                if (adWatchCount >= value)
                {
                    flag = true;
                }
            }
            else
            {
                numContent.text = "0/" + value;
                process.fillAmount = 0;
             
            }

            RefreshWithdrawProcessText();

            titleContent.text = string.Format(LocalizationManager.Instance.GetText("2084"), num);
            moneyContent.text = GameManagerWZ.instance.mark + money;
            RefreshLuckyWalletSection();
        }

        public override void InitView()
        {
            base.InitView();
            RefreshLuckyWalletBalance();
        }

        private void RefreshLuckyWalletSection()
        {
            bool isLuckyWalletUnlocked = GameManagerWZ.instance.currentLv >= GameDefines.LuckyWalletUnlockLevel;
            if (isLuckyWalletUnlocked)
            {
                Tips.gameObject.SetActive(false);
                CashOut.gameObject.SetActive(true);
                CurLuckyCoin.gameObject.SetActive(true);
                CurLuckyCoin.text = FormatLuckyWalletBalance(WithdrawApiService.GetCachedWalletBalance());
                return;
            }

            Tips.text = string.Format(LocalizationManager.Instance.GetText("3067"), GameDefines.LuckyWalletUnlockLevel - 1);
            Tips.gameObject.SetActive(true);
            CashOut.gameObject.SetActive(false);
            CurLuckyCoin.gameObject.SetActive(false);
        }

        private void RefreshLuckyWalletBalance()
        {
            if (GameManagerWZ.instance == null || GameManagerWZ.instance.currentLv < GameDefines.LuckyWalletUnlockLevel)
                return;

            StartCoroutine(WithdrawApiService.QueryWalletBalance(wallet =>
            {
                CurLuckyCoin.text = FormatLuckyWalletBalance(wallet != null ? wallet.Balance : WithdrawApiService.GetCachedWalletBalance());
            }, error =>
            {
                Debug.LogError($"WithDraw wallet balance query failed: {error}");
                CurLuckyCoin.text = FormatLuckyWalletBalance(WithdrawApiService.GetCachedWalletBalance());
            }));
        }

        private string FormatLuckyWalletBalance(double balance)
        {
            int decimals = GameManagerWZ.instance != null ? Mathf.Max(GameManagerWZ.instance.decimals, 0) : 2;
            string mark = GameManagerWZ.instance != null ? GameManagerWZ.instance.mark : "$";
            return mark + balance.ToString($"F{decimals}");
        }

        public void OnButtonClick()
        {
            var playerName = GameManagerWZ.instance.getPlayerName();
            var phone = GameManagerWZ.instance.getPlayerPhone();
            var email = GameManagerWZ.instance.getPlayerEmail();
            int index = GameManagerWZ.instance.getPlayerIndex();

            dataModel.Num = num;
            dataModel.Name = playerName;
            dataModel.Phone = phone;
            dataModel.Email = email;
            dataModel.Index = index;
            dataModel.Special = DataModel.SpecialNone;
            dataModel.Money = GameManagerWZ.instance.GetCoin();
            dataModel.UseProcessViewOnMethodExit = num == 3;

            if (!flag)
            {
                panelE.SetActive(true);
                StartCoroutine(DelayedAction());
                return;
            }

            GameManagerWZ.instance.TrySyncStageProgress(num);

            // Goal2：引导结束后进 WithdrawMissionView，不进 Process1；关卡等 Mission Continue 再加载。
            if (num == 2 || (GameManagerWZ.instance != null && GameManagerWZ.instance.Stage2CashGuidePending))
            {
                GameManagerWZ.instance?.CloseActiveTutorial();
                bool hasEmail = !string.IsNullOrEmpty(email);
                if (!hasEmail)
                {
                    index = 3;
                    dataModel.Index = 3;
                    dataModel.UseProcessViewOnMethodExit = false;
                    GameApp.viewManager.Open(ViewType.WithDrawMethod, dataModel);
                    GameApp.viewManager.Close(ViewId);
                    return;
                }

                GameApp.viewManager.Close(ViewId);
                GameManagerWZ.instance?.FinishStage2CashGuideAndShowMission();
                return;
            }

            // 进入 Method/Process 后清掉 CMBtn 引导，避免和排队页叠在一起。
            GameManagerWZ.instance?.CloseActiveTutorial();

            if (dataModel.Type == 2)
            {
                GameApp.viewManager.Open(ViewType.WithDrawProcess1, dataModel);
                GameApp.viewManager.Close(ViewId);
                return;
            }

            bool hasSavedWithdrawMethod = false;
            index = 3;
            dataModel.Index = 3;
            hasSavedWithdrawMethod = !string.IsNullOrEmpty(email);

            if (!hasSavedWithdrawMethod)
            {
                GameApp.viewManager.Open(ViewType.WithDrawMethod, dataModel);
                GameApp.viewManager.Close(ViewId);
            }
            else
            {
                GameApp.viewManager.Open(ViewType.WithDrawProcess1, dataModel);
                GameApp.viewManager.Close(ViewId);
            }
        }

        public void onCloseBtnClick()
        {
            GameApp.viewManager.Close(ViewId);
            // Goal1 用兑现替代通关页时：关闭后直接进下一关。
            if (GameManagerWZ.instance != null && GameManagerWZ.instance.IsReplacingSuccessWithWithdraw())
                GameManagerWZ.instance.EndWithdrawInsteadOfSuccessAndContinue();
        }   
        public void onCashOutBtnClick()
        {
      

            UIManager.instance.ShowView(EUIType.GameBalancView);
        }

        private void SetProcessFillAmount(float currentValue, float targetValue)
        {
            if (process == null)
                return;

            if (targetValue <= 0f)
            {
                process.fillAmount = 0f;
                return;
            }

            process.fillAmount = Mathf.Clamp01(currentValue / targetValue);
        }

        private void RefreshWithdrawProcessText()
        {
            if (withdrawProcess == null)
                return;

            int percent = process != null ? Mathf.RoundToInt(Mathf.Clamp01(process.fillAmount) * 100f) : 0;
            string progressText = percent + "%";
            string localizedText = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText("3064")
                : "Target reached {0}";

            withdrawProcess.text = string.Format(localizedText, progressText);
        }

        private void OnRecordBtnClickHandle()
        {
            GameManagerWZ Gm = GameManagerWZ.instance;
            int lv = Gm.currentLv;
            double coin = Gm.GetCoin();
            int stage = Gm.currentStage;

            DataModel dataModel = new DataModel();
            var playerName = Gm.getPlayerName();
            var phone = Gm.getPlayerPhone();
            var email = Gm.getPlayerEmail();

            dataModel.Name = playerName;
            dataModel.Phone = phone;
            dataModel.Email = email;
            dataModel.Level = lv - 1;
            dataModel.Money = coin;
            dataModel.CloseType = 1;

            GameApp.viewManager.Open(ViewType.WithDrawHistory, dataModel);
        }

        IEnumerator DelayedAction()
        {
            yield return new WaitForSeconds(2.0f); // 等待2秒
            panelE.SetActive(false);
        }

    }
}
