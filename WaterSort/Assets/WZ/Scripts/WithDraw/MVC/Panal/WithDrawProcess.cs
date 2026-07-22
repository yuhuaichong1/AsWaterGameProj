using System;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class WithDrawProcess : BaseViewWithDraw
    {
        private GameObject panel2;
        private GameObject panel3;
        private GameObject buttonContinue;
        private GameObject image;
        private GameObject imageA;
        private GameObject imageB;
        private GameObject wait;
        private GameObject wait1;
        private GameObject wait2;
        private double time = 0;
        private bool isStart = true;
        private double money;
        private int level;
        private Text moneyContent;
        private Text Num;
        private DataModel dataModel;

        protected override void OnAwake()
        {
            Find<Button>("PanelA/Continue").onClick.AddListener(OnButtonClick);
            Find<Button>("PanelA/close").onClick.AddListener(OnCloseBtnClick);
            moneyContent = Find<Text>("PanelA/PanelC/content");
            Num = Find<Text>("Panel/titleContent");
            panel2 = Find("PanelA/PanelB/Panel2");
            panel3 = Find("PanelA/PanelB/Panel3");
            image = Find("PanelA/PanelB/Panel1/Image");
            wait = Find("PanelA/PanelB/Panel1/wait");
            imageA = Find("PanelA/PanelB/Panel2/ImageA");
            wait1 = Find("PanelA/PanelB/Panel2/wait1");
            imageB = Find("PanelA/PanelB/Panel3/ImageB");
            wait2 = Find("PanelA/PanelB/Panel3/wait2");
            buttonContinue = Find("PanelA/Continue");
        }

        protected override void HandleViewArgs(object[] args)
        {
            dataModel = args[0] as DataModel;
            level = dataModel.Level;
            money = dataModel.Money;
            int num = dataModel.Num;
            moneyContent.text = GameManagerWZ.instance.mark + money;
            Num.text = string.Format(LocalizationManager.Instance.GetText("2062"), num);
        }

        public void OnButtonClick()
        {
            GameManagerWZ.instance.StartCoroutine(DelayedNextLevel());
            /*       GameApp.viewManager.Open(ViewType.WithDrawKeepEarn, dataModel);
                   GameApp.viewManager.Close(ViewId);*/
        }

        public void OnCloseBtnClick()
        {
            GameApp.viewManager.Close(ViewId);

            /*  if (dataModel.CloseType == 1)
              {
                  GameApp.viewManager.Close(ViewId);
              }
              else
              {
                  GameApp.viewManager.Close(ViewId);
                  GameApp.viewManager.Open(ViewType.WithDrawContinue, dataModel);
              }*/
            GameManagerWZ.instance.StartCoroutine(DelayedNextLevel());
        }

        private System.Collections.IEnumerator DelayedNextLevel()
        {
            yield return new WaitForSeconds(0.2f);
            GameManagerWZ.instance.NextLevel();
            GameApp.viewManager.Close(ViewId);
        }

        private void Update()
        {
            if (isStart)
            {
                if (time > 1)
                {
                    wait.SetActive(false);
                    image.SetActive(true);
                    panel2.SetActive(true);
                }

                if (time > 2)
                {
                    wait1.SetActive(false);
                    imageA.SetActive(true);
                    panel3.SetActive(true);
                }

                if (time > 3)
                {
                    wait2.SetActive(false);
                    imageB.SetActive(true);
                    buttonContinue.SetActive(true);
                    isStart = false;
                }

                time += Time.deltaTime;
            }

        }
    }
}
