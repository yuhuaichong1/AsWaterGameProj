using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawGoal : BaseUI
    {	protected RectTransform mPlane;	protected Button mExitBtn;	protected Text mGoalTitle;	protected Image mMoneyIcon;	protected Text mBlanceMoney;	protected Slider mGoalSlider;	protected Text mGoalText;	protected Button mWithdrawBtn;	protected Text mLevelStatsText;	protected Text mTCValue;	protected Text mAAValue;	protected Text mAWValue;	protected AutoCarousel mNoticeScrollView;	protected Text mGoldSliderText;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mExitBtn = mTransform.Find("Plane/Plane1/ExitBtn").GetComponent<Button>();		mGoalTitle = mTransform.Find("Plane/Plane1/GoalTitle").GetComponent<Text>();		mMoneyIcon = mTransform.Find("Plane/Plane1/Bg2/MoneyIcon").GetComponent<Image>();		mBlanceMoney = mTransform.Find("Plane/Plane1/Bg2/BlanceMoney").GetComponent<Text>();		mGoalSlider = mTransform.Find("Plane/Plane1/BottomPart/GoalSlider").GetComponent<Slider>();		mGoalText = mTransform.Find("Plane/Plane1/BottomPart/GoalText").GetComponent<Text>();		mWithdrawBtn = mTransform.Find("Plane/Plane1/BottomPart/WithdrawBtn").GetComponent<Button>();		mLevelStatsText = mTransform.Find("Plane/Plane2/LevelStatsText").GetComponent<Text>();		mTCValue = mTransform.Find("Plane/Plane2/CurLevelInfo/TodayCompletions/TCValue").GetComponent<Text>();		mAAValue = mTransform.Find("Plane/Plane2/CurLevelInfo/AverageAttempts/AAValue").GetComponent<Text>();		mAWValue = mTransform.Find("Plane/Plane2/CurLevelInfo/AverageWithdrawal/AWValue").GetComponent<Text>();		mNoticeScrollView = mTransform.Find("Plane/Plane2/NoticeScrollView").GetComponent<AutoCarousel>();		mGoldSliderText = mTransform.Find("Plane/Plane1/BottomPart/GoalSlider/GoldSliderText").GetComponent<Text>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);		mWithdrawBtn.onClick.AddListener( OnWithdrawBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mExitBtn.onClick.RemoveAllListeners();		mWithdrawBtn.onClick.RemoveAllListeners();
        }
    
    }
}