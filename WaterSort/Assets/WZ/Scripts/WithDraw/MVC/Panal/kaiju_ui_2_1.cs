using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class kaiju_ui_2_1 : BaseViewWithDraw
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
            greenBallContent.text = string.Format(LocalizationManager.Instance.GetText("2101"), level);
        }

        public void onButtonClick()
        {
            GameApp.viewManager.Close(ViewId);
        }
    }
}
