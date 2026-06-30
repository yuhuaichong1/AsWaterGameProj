using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XrCode;

public class OrderItem : MonoBehaviour
{
    public GameObject Bg1;
    public GameObject Bg2;
    public Text LevelText;
    public Text StateText;
    public Text DateText;
    public Text MoneyText;

    public Button Btn;

    private WithdrawalRecordItem item;

    public void SetInfo(WithdrawalRecordItem item)
    {
        LevelText.text = string.Format(FacadeLanguage.GetText("10087"),item.LevelId);
        DateText.text = item.CreatedDate;
        MoneyText.text = FacadePayType.RegionalChange(item.WRMoney);
        this.item = item;
        bool b = (item.WRState == EWithRecordState.GoWithdrawal);
        StateText.text = FacadeLanguage.GetText(b ? "10088" : "10089");
        Bg1.gameObject.SetActive(!b);
        Bg2.gameObject.SetActive(b);

        Btn.onClick.RemoveAllListeners();
        Btn.onClick.AddListener(OnBtnClick);

        if(item.TargetType != WithdrawTarget.PassLevel)
        {
            this.gameObject.SetActive(false);
        }
    }

    public void OnBtnClick()
    {
        return;

        if(item.WRState == EWithRecordState.UnderReview)
        {
            UIManager.Instance.OpenNotice2(FacadeLanguage.GetText("10089"));
        }
        else//state == EWithRecordState.GoWithdrawal
        {
            FacadeWithdraw.CheckOpenUI(true, item);
        }
    }
}
