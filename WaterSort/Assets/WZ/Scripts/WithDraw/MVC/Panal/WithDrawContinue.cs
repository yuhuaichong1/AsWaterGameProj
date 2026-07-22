using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
   
    public class WithDrawContinue : BaseViewWithDraw
    {
        private Text moneyContent;
        private Text levelContent;
        private GameObject panel;
        private DataModel dataModel;
        protected override void OnAwake()
        {
            moneyContent = Find<Text>("PanelA/PanelC/WithDrawItem/PanelA/TextUp");
            panel = Find("PanelA/PanelC");
            levelContent = Find<Text>("PanelA/PanelC/WithDrawItem1/PanelA/TextDown");
            Find<Button>("PanelA/Continue").onClick.AddListener(onContinueBtnClick);
        }



        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            int level = dataModel.Level;
            double money = dataModel.Money;
            int num = dataModel.Num;
            var count = panel.transform.childCount;
            levelContent.text = string.Format(LocalizationManager.Instance.GetText("2015"), level + 1);
            for (int i = 0; i < count; i++)
            {
                Text itemText = panel.transform.GetChild(i).Find("PanelA/TextUp").GetComponent<Text>();
                if (itemText != null)
                {
                    if (i == 0 || i == 1)
                    {
                        itemText.text = GameManagerWZ.instance.mark + "0.04";
                    }
                    else if (i == 2)
                    {
                        itemText.text = GameManagerWZ.instance.mark + "200-" + GameManagerWZ.instance.mark + "1000";
                    }
                    else
                    {
                        itemText.text = GameManagerWZ.instance.mark + money.ToString("F2");
                    }
                }
                if (i < num)
                {
                    panel.transform.GetChild(i).GetComponent<WithDrawItem>().changeStyle(i + 1);
                }
            }

            GameManagerWZ.instance.AddCoinTem(0 - (float)money);
            List<HistoryModel> historyModels = SPlayerPrefs.GetListTem<HistoryModel>(FacadePlayerPrefExtend.History);
            foreach (var historyModel in historyModels)
            {
                if (historyModel.ID.Equals(dataModel.HistoryId))
                {
                    historyModel.Status = 1;
                    break;
                }
            }
            SPlayerPrefs.SetListTem(FacadePlayerPrefExtend.History, historyModels);
        }
        private void onContinueBtnClick()
        {
            /* if (GameManager.instance.currentStage == 1)
             {
                 GameManager.instance.StartCoroutine(DelayedNextLevel());
             }
             else if (GameManager.instance.currentStage == 2)
             {

                 GameManager.instance.StartCoroutine(DelayedNextLevel());
             }
             else
             {

             }*/
            GameManagerWZ.instance.StartCoroutine(DelayedNextLevel());
        }

        private System.Collections.IEnumerator DelayedNextLevel()
        {
            yield return new WaitForSeconds(0.2f);
            GameManagerWZ.instance.NextLevel();
            GameApp.viewManager.Close(ViewId);
        }
    }
}
