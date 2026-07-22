using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class WithDrawItem : MonoBehaviour
    {
        public void changeStyle(int stage)
        {
            double money = 0;
            var panel = transform.Find("PanelA");
            var textUp = transform.Find("PanelA/TextUp");
            var textDown = transform.Find("PanelA/TextDown");
            var image = panel.GetComponent<Image>();
            var sprite = Resources.Load<Sprite>("Prefab/UI/Withdraw/hui_bj");
            image.sprite = sprite;
            List<HistoryModel> historyModels = SPlayerPrefs.GetListTem<HistoryModel>(FacadePlayerPrefExtend.History);
            foreach (var historyModel in historyModels)
            {
                if (historyModel.Stage.Equals(stage))
                {
                    money = historyModel.Money;
                    break;
                }
            }
            Color hexColor;
            if (ColorUtility.TryParseHtmlString("#4F4F4F", out hexColor))
            {
                textUp.GetComponent<Text>().color = hexColor;
                textUp.GetComponent<Text>().text = GameManagerWZ.instance.mark + money.ToString("F2");
                textDown.GetComponent<Text>().color = hexColor;
                textDown.GetComponent<Text>().text = LocalizationManager.Instance.GetText("2013");
            }

        }
    }
}