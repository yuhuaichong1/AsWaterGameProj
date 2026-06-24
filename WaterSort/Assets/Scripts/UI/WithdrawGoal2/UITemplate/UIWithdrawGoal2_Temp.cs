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
            		mExitBtn = mTransform.Find("ExitBtn").GetComponent<Button>();		mTBContent = mTransform.Find("TotalBalance/TBContent").GetComponent<Text>();		mCurWIcon = mTransform.Find("WithdrawInfo/Mask/CurWIcon").GetComponent<Image>();		mCurWInfo = mTransform.Find("WithdrawInfo/CurWInfo").GetComponent<Text>();		mSetCurWInfoBtn = mTransform.Find("WithdrawInfo/SetCurWInfoBtn").GetComponent<Button>();		mWOTContent = mTransform.Find("WOptions/WOType/Viewport/WOTContent").GetComponent<RectTransform>();		mWOTypeItem = mTransform.Find("WOptions/WOType/Viewport/WOTContent/WOTypeItem").GetComponent<WOTypeItem>();		mWOLContent = mTransform.Find("WOptions/WOList/Viewport/WOLContent").GetComponent<RectTransform>();		mWOListItem = mTransform.Find("WOptions/WOList/Viewport/WOLContent/WOListItem").GetComponent<WOListItem>();
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