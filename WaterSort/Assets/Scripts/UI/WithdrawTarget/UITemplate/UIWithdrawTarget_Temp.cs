using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawTarget : BaseUI
    {	protected RectTransform mPlane;	protected RectTransform mTitle;	protected Text mTitleContent;	protected RectTransform mWaiter1;	protected RectTransform mWaiter2;	protected RectTransform mWaiter3;	protected RectTransform mName1;	protected RectTransform mName2;	protected RectTransform mName3;	protected Text mContentText1;	protected Text mContentText2;	protected RectTransform mWTargetSign1;	protected Text mWTS1Text;	protected RectTransform mLevelTargetTip;	protected Text mLTText;	protected Button mContinueBtn;	protected RectTransform mWTargetSign2;	protected Text mWTS2Text;	protected RectTransform mWTargetSign3;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mTitle = mTransform.Find("Plane/Title").GetComponent<RectTransform>();		mTitleContent = mTransform.Find("Plane/Title/TitleContent").GetComponent<Text>();		mWaiter1 = mTransform.Find("Plane/Waiters/Waiter1").GetComponent<RectTransform>();		mWaiter2 = mTransform.Find("Plane/Waiters/Waiter2").GetComponent<RectTransform>();		mWaiter3 = mTransform.Find("Plane/Waiters/Waiter3").GetComponent<RectTransform>();		mName1 = mTransform.Find("Plane/Names/Name1").GetComponent<RectTransform>();		mName2 = mTransform.Find("Plane/Names/Name2").GetComponent<RectTransform>();		mName3 = mTransform.Find("Plane/Names/Name3").GetComponent<RectTransform>();		mContentText1 = mTransform.Find("Plane/Content/ContentText1").GetComponent<Text>();		mContentText2 = mTransform.Find("Plane/Content/ContentText2").GetComponent<Text>();		mWTargetSign1 = mTransform.Find("Plane/Content/WTargetSign1").GetComponent<RectTransform>();		mWTS1Text = mTransform.Find("Plane/Content/WTargetSign1/Left/greenBall/WTS1Text").GetComponent<Text>();		mLevelTargetTip = mTransform.Find("Plane/LevelTargetTip").GetComponent<RectTransform>();		mLTText = mTransform.Find("Plane/LevelTargetTip/LTText").GetComponent<Text>();		mContinueBtn = mTransform.Find("Continue/ContinueBtn").GetComponent<Button>();		mWTargetSign2 = mTransform.Find("Plane/Content/WTargetSign2").GetComponent<RectTransform>();		mWTS2Text = mTransform.Find("Plane/Content/WTargetSign2/Left/greenBall/WTS2Text").GetComponent<Text>();		mWTargetSign3 = mTransform.Find("Plane/Content/WTargetSign3").GetComponent<RectTransform>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mContinueBtn.onClick.AddListener( OnContinueBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mContinueBtn.onClick.RemoveAllListeners();
        }
    
    }
}