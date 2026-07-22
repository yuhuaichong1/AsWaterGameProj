using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class kaiju_ui_2 : BaseViewWithDraw
    {
        private Text titleContent;
        private Text greenBallContent;
        private int level;

        private DataModel dataModel;
        protected override void OnAwake()
        {
            Find<Button>("continue").onClick.AddListener(onButtonClick);

            greenBallContent = Find<Text>("dial/Panel/PanalIn/Panel/greenBall/content");
        }

        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            level = dataModel.Level;
            var stageData3 = UserDataManager.Instance.getStageData(3);
            if (stageData3 == null)
                return;

            int targetValue = int.Parse((string)stageData3["Value"]);
            int stageType = int.Parse((string)stageData3["Type"]);
            if (stageType == 4)
                greenBallContent.text = string.Format(LocalizationManager.Instance.GetText("2148"), targetValue);
            else
                greenBallContent.text = string.Format(LocalizationManager.Instance.GetText("2101"), targetValue);
        }

        public void onButtonClick()
        {
            GameApp.viewManager.Close(ViewId);
            FacadeEffectExtend.PlayCongratulationEffectHandle?.Invoke();
        }
    }
}
