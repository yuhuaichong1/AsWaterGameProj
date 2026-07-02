using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UITotalReward2 : BaseUI
    {	protected RectTransform mPlane;	protected Text mMoneyText;	protected Text mContent;	protected Button mExitBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mMoneyText = mTransform.Find("Plane/Bg3/Money/MoneyText").GetComponent<Text>();		mContent = mTransform.Find("Plane/Content").GetComponent<Text>();		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mExitBtn.onClick.RemoveAllListeners();
        }
    
    }
}