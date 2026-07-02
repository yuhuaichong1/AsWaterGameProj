using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawGoal2 : BaseUI
    {	protected Button mExitBtn;	protected Text mTBContent;	protected Image mCurWIcon;	protected Text mCurWInfo;	protected Button mSetCurWInfoBtn;	protected RectTransform mWOTContent;	protected WOTypeItem mWOTypeItem;	protected RectTransform mWOLContent;	protected WOListItem mWOListItem;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();		mTBContent = mTransform.Find("Plane/TotalBalance/TBContent").GetComponent<Text>();		mCurWIcon = mTransform.Find("Plane/WithdrawInfo/Mask/CurWIcon").GetComponent<Image>();		mCurWInfo = mTransform.Find("Plane/WithdrawInfo/CurWInfo").GetComponent<Text>();		mSetCurWInfoBtn = mTransform.Find("Plane/WithdrawInfo/SetCurWInfoBtn").GetComponent<Button>();		mWOTContent = mTransform.Find("Plane/WOptions/WOType/Viewport/WOTContent").GetComponent<RectTransform>();		mWOTypeItem = mTransform.Find("Plane/WOptions/WOType/Viewport/WOTContent/WOTypeItem").GetComponent<WOTypeItem>();		mWOLContent = mTransform.Find("Plane/WOptions/WOList/Viewport/WOLContent").GetComponent<RectTransform>();		mWOListItem = mTransform.Find("Plane/WOptions/WOList/Viewport/WOLContent/WOListItem").GetComponent<WOListItem>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);		mSetCurWInfoBtn.onClick.AddListener( OnSetCurWInfoBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mExitBtn.onClick.RemoveAllListeners();		mSetCurWInfoBtn.onClick.RemoveAllListeners();
        }
    
    }
}