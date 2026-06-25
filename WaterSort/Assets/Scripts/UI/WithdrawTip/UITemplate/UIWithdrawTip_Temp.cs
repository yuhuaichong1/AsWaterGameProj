using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawTip : BaseUI
    {	protected RectTransform mPlane;	protected Button mExitBtn;	protected Image mWTypeIcon;	protected Text mTargetMoney;	protected Button mCashOutBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();		mWTypeIcon = mTransform.Find("Plane/Bg/Bg2/Mask/WTypeIcon").GetComponent<Image>();		mTargetMoney = mTransform.Find("Plane/Bg/Bg2/TargetMoney").GetComponent<Text>();		mCashOutBtn = mTransform.Find("Plane/CashOutBtn").GetComponent<Button>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);		mCashOutBtn.onClick.AddListener( OnCashOutBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mExitBtn.onClick.RemoveAllListeners();		mCashOutBtn.onClick.RemoveAllListeners();
        }
    
    }
}