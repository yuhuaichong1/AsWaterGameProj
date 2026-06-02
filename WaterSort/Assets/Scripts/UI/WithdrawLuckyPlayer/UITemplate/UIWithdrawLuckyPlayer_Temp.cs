using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawLuckyPlayer : BaseUI
    {	protected RectTransform mPlane;	protected Button mExitBtn;	protected Text mLPMoney;	protected Text mDesc;	protected Button mConfirmBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();		mLPMoney = mTransform.Find("Plane/Content/LPMoney").GetComponent<Text>();		mDesc = mTransform.Find("Plane/Desc").GetComponent<Text>();		mConfirmBtn = mTransform.Find("Plane/ConfirmBtn").GetComponent<Button>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);		mConfirmBtn.onClick.AddListener( OnConfirmBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mExitBtn.onClick.RemoveAllListeners();		mConfirmBtn.onClick.RemoveAllListeners();
        }
    
    }
}