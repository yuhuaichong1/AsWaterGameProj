using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WOListItem : MonoBehaviour
{
    public Image Icon;
    public Text TargetText;
    public Slider MoneySlider;
    public Text OtherText;
    public Button CashOutBtn;
    public Button ContinueBtn;
    public Button UnContinueBtn;

    void Awake()
    {
        CashOutBtn.onClick.AddListener(OnCashOutBtnClick);
        ContinueBtn.onClick.AddListener(OnContinueBtnClick);
        UnContinueBtn.onClick.AddListener(OnUnContinueBtnClick);
    }

    public void SetTTInfo(Sprite wTypeIcon, float target)
    {
        Icon.sprite = wTypeIcon;
        TargetText.text = FacadePayType.RegionalChange(target);
    }

    public void SetPInfo()
    {

    }

    private void OnCashOutBtnClick()
    {

    }

    private void OnContinueBtnClick()
    {

    }

    private void OnUnContinueBtnClick()
    {
        
    }
}
