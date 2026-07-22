using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static WZSDK.Tutorial;

namespace WZSDK
{
    public class kaiju_ui_4 : BaseViewWithDraw
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

            var stage4Data = GameManagerWZ.instance.GetStageData(4);
            float targetWithdraw = 0f;
            if (stage4Data != null && stage4Data.ContainsKey("Value"))
            {
                float.TryParse((string)stage4Data["Value"], out targetWithdraw);
            }
            var Gap = targetWithdraw - Money;
            content1.text = string.Format(LocalizationManager.Instance.GetText("2100"), Gap.ToString("F2"), targetWithdraw);
        }

        public void onButtonClick()
        {
            GameApp.viewManager.Close(ViewId);
               FacadeEffectExtend.PlayCongratulationEffectHandle?.Invoke();
         
        }
    }
}
