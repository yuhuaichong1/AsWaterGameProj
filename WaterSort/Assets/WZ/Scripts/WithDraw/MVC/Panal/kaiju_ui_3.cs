using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class kaiju_ui_3 : BaseViewWithDraw
    {
        private Text titleContent;
        private Text greenBallContent;
        private int level;
        private DataModel dataModel;
        protected override void OnAwake()
        {
            Find<Button>("continue").onClick.AddListener(onButtonClick);

            titleContent = Find<Text>("dial/title/content");
            greenBallContent = Find<Text>("dial/ImageA/content");
        }
        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            level = dataModel.Level;
            if (level >= GameDefines.SmallLevelGroupStart && level <= GameDefines.SmallLevelGroupEnd)
            {
                int progress = level - GameDefines.SmallLevelGroupStart + 2;
                string text = string.Format(LocalizationManager.Instance.GetText("2095"), progress, 10);
                greenBallContent.text = text;
            }
            else
            {
                greenBallContent.text = string.Format(LocalizationManager.Instance.GetText("1004"), level);
            }
        }

        public void onButtonClick()
        {
            GameApp.viewManager.Close(ViewId);
            FacadeEffectExtend.PlayCongratulationEffectHandle?.Invoke();
        }
    }
}
