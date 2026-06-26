using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIEffect : BaseUI
    {	protected RectTransform mGREMask;	protected RectTransform mGetRewardEffect;	protected RectTransform mRewardGroup;	protected EffectRewardItem mEffectRewardItem;	protected RectTransform mCongratulationEffect;	protected Text mCEContent;	protected RectTransform mFlyEffectParent;	protected RectTransform mFlyProp;	protected RectTransform mFlyMoney;	protected RectTransform mFlyIAAMoney;	protected RectTransform mFlyMoney2;	protected RectTransform mFlyIAAMoney2;	protected RectTransform mFlyMoneyTip;	protected RectTransform mFlyIAAMoneyTip;	protected RectTransform mDifficultyUpEffect;	protected LanguageText mDUEText;	protected RectTransform mGetRewardEffect2;	protected CanvasGroup mGRE2_Bg;	protected RectTransform mGRE2_Plane;	protected RectTransform mGRE2_MoneyIcon;	protected RectTransform mGRE2_IAAIcon;	protected Image mGRE2_PorpIcon;	protected Text mGRE2_Count;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mGREMask = mTransform.Find("GetRewardEffect/GREMask").GetComponent<RectTransform>();		mGetRewardEffect = mTransform.Find("GetRewardEffect").GetComponent<RectTransform>();		mRewardGroup = mTransform.Find("GetRewardEffect/Plane/RewardGroup").GetComponent<RectTransform>();		mEffectRewardItem = mTransform.Find("GetRewardEffect/Plane/RewardGroup/EffectRewardItem").GetComponent<EffectRewardItem>();		mCongratulationEffect = mTransform.Find("CongratulationEffect").GetComponent<RectTransform>();		mCEContent = mTransform.Find("CongratulationEffect/CEContent").GetComponent<Text>();		mFlyEffectParent = mTransform.Find("FlyEffectParent").GetComponent<RectTransform>();		mFlyProp = mTransform.Find("FlyEffectParent/FlyProp").GetComponent<RectTransform>();		mFlyMoney = mTransform.Find("FlyEffectParent/FlyMoney").GetComponent<RectTransform>();		mFlyIAAMoney = mTransform.Find("FlyEffectParent/FlyIAAMoney").GetComponent<RectTransform>();		mFlyMoney2 = mTransform.Find("FlyEffectParent/FlyMoney2").GetComponent<RectTransform>();		mFlyIAAMoney2 = mTransform.Find("FlyEffectParent/FlyIAAMoney2").GetComponent<RectTransform>();		mFlyMoneyTip = mTransform.Find("FlyEffectParent/FlyMoneyTip").GetComponent<RectTransform>();		mFlyIAAMoneyTip = mTransform.Find("FlyEffectParent/FlyIAAMoneyTip").GetComponent<RectTransform>();		mDifficultyUpEffect = mTransform.Find("DifficultyUpEffect").GetComponent<RectTransform>();		mDUEText = mTransform.Find("DifficultyUpEffect/DUEText").GetComponent<LanguageText>();		mGetRewardEffect2 = mTransform.Find("GetRewardEffect2").GetComponent<RectTransform>();		mGRE2_Bg = mTransform.Find("GetRewardEffect2/GRE2_Plane/GRE2_Bg").GetComponent<CanvasGroup>();		mGRE2_Plane = mTransform.Find("GetRewardEffect2/GRE2_Plane").GetComponent<RectTransform>();		mGRE2_MoneyIcon = mTransform.Find("GetRewardEffect2/GRE2_Plane/GRE2_Bg/GRE2_MoneyIcon").GetComponent<RectTransform>();		mGRE2_IAAIcon = mTransform.Find("GetRewardEffect2/GRE2_Plane/GRE2_Bg/GRE2_IAAIcon").GetComponent<RectTransform>();		mGRE2_PorpIcon = mTransform.Find("GetRewardEffect2/GRE2_Plane/GRE2_Bg/GRE2_PorpIcon").GetComponent<Image>();		mGRE2_Count = mTransform.Find("GetRewardEffect2/GRE2_Plane/GRE2_Bg/GRE2_Count").GetComponent<Text>();
        }
    
        protected override void BindButtonEvent() 
        {
            
        }
    
        protected override void UnBindButtonEvent() 
        {
            
        }
    
    }
}