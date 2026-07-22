using Spine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class kaiju_ui_5 : BaseViewWithDraw
    {
        private Text content1;
        private Text Num1;
        double Money;
        private DataModel dataModel;

        protected override void OnAwake()
        {
            Find<Button>("continue").onClick.AddListener(onButtonClick);
            Find<Button>("dial/Panel/GenerateMoreBtn").onClick.AddListener(onButtonClick);
            Find<Button>("dial/Panel/bg/close").onClick.AddListener(onButtonClick);
            content1 = Find<Text>("dial/Panel/content1");
        }

        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            Money = dataModel.Money;

            if (!GameManagerWZ.instance.TryGetStageRequirement(5, out int stageType, out int stageTarget))
                return;

            if (stageType == 3)
            {
                int loginDay = GameManagerWZ.instance.getLoginDay();
                int remainingDays = stageTarget - loginDay;

                if (remainingDays <= 0)
                {
                    content1.text = string.Format(LocalizationManager.Instance.GetText("2098"), 0);
                }
                else
                {
                    string format = LocalizationManager.Instance.GetText("2098");
                    content1.text = string.Format(format, remainingDays);
                }
            }
            else
            {
                double remainingMoney = stageTarget - Money;
                if (remainingMoney < 0)
                    remainingMoney = 0;

                content1.text = string.Format(LocalizationManager.Instance.GetText("2100"), remainingMoney.ToString("F2"), stageTarget);
            }
        }

        public void onButtonClick()
        {
            GameApp.viewManager.Close(ViewId);
            FacadeEffectExtend.PlayCongratulationEffectHandle?.Invoke();
        }
    }
}
