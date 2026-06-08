using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawKeepEarn : BaseUI
    {	protected RectTransform mPlane;	protected Button mExitBtn;	protected Image mOMIcon;	protected Text mOMText;	protected Button mKeepEarnBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();		mOMIcon = mTransform.Find("Plane/OrderMoney/OMIcon").GetComponent<Image>();		mOMText = mTransform.Find("Plane/OrderMoney/OMText").GetComponent<Text>();		mKeepEarnBtn = mTransform.Find("Plane/KeepEarnBtn").GetComponent<Button>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);		mKeepEarnBtn.onClick.AddListener( OnKeepEarnBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mExitBtn.onClick.RemoveAllListeners();		mKeepEarnBtn.onClick.RemoveAllListeners();
        }
    
    }
}