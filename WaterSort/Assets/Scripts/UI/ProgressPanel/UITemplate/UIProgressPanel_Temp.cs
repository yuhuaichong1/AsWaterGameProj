using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIProgressPanel : BaseUI
    {	protected LanguageText mLabTitle;	protected RectTransform mLineConnect;	protected Image mPreviousState;	protected LanguageText mPreviousCondition;	protected Image mNextState;	protected LanguageText mNextCondition;	protected LanguageText mRuleExplain;	protected Button mBtnContinue;	protected LanguageText mLabContinue;	protected RectTransform mPayOutPanel;	protected CanvasGroup mAnimPanel;	protected RectTransform mBox;	protected Text mPrevioursStepFinish;	protected CanvasGroup mArrow;	protected RectTransform mLine1;	protected RectTransform mLine2;	protected RectTransform mBoxPos1;	protected RectTransform mBoxPos2;	protected RectTransform mLine1Pos1;	protected RectTransform mLine1Pos2;	protected RectTransform mLine1Pos3;	protected RectTransform mLine1Pos4;	protected RectTransform mLine2Pos1;	protected RectTransform mLine2Pos2;	protected RectTransform mArrowPos1;	protected RectTransform mArrowPos2;	protected RectTransform mTextPos1;	protected RectTransform mTextPos2;	protected RectTransform mAnimPlanePos1;	protected RectTransform mAnimPlanePos2;	protected RectTransform mPOPPlanePos2;	protected RectTransform mPOPPlanePos1;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mLabTitle = mTransform.Find("Panel/PayOutPanel/Top/LabTitle").GetComponent<LanguageText>();		mLineConnect = mTransform.Find("Panel/PayOutPanel/Top/Previous/LineConnect").GetComponent<RectTransform>();		mPreviousState = mTransform.Find("Panel/PayOutPanel/Top/Previous/PreviousState").GetComponent<Image>();		mPreviousCondition = mTransform.Find("Panel/PayOutPanel/Top/Previous/PreviousCondition").GetComponent<LanguageText>();		mNextState = mTransform.Find("Panel/PayOutPanel/Top/Next/NextState").GetComponent<Image>();		mNextCondition = mTransform.Find("Panel/PayOutPanel/Top/Next/NextCondition").GetComponent<LanguageText>();		mRuleExplain = mTransform.Find("Panel/PayOutPanel/Buttom/ContentBox/RuleExplain").GetComponent<LanguageText>();		mBtnContinue = mTransform.Find("Panel/PayOutPanel/Buttom/BtnContinue").GetComponent<Button>();		mLabContinue = mTransform.Find("Panel/PayOutPanel/Buttom/BtnContinue/LabContinue").GetComponent<LanguageText>();		mPayOutPanel = mTransform.Find("Panel/PayOutPanel").GetComponent<RectTransform>();		mAnimPanel = mTransform.Find("Panel/AnimPanel").GetComponent<CanvasGroup>();		mBox = mTransform.Find("Panel/AnimPanel/Box").GetComponent<RectTransform>();		mPrevioursStepFinish = mTransform.Find("Panel/AnimPanel/PrevioursStepFinish").GetComponent<Text>();		mArrow = mTransform.Find("Panel/AnimPanel/Arrow").GetComponent<CanvasGroup>();		mLine1 = mTransform.Find("Panel/AnimPanel/Line1").GetComponent<RectTransform>();		mLine2 = mTransform.Find("Panel/AnimPanel/Line2").GetComponent<RectTransform>();		mBoxPos1 = mTransform.Find("AnimPos/BoxPos1").GetComponent<RectTransform>();		mBoxPos2 = mTransform.Find("AnimPos/BoxPos2").GetComponent<RectTransform>();		mLine1Pos1 = mTransform.Find("AnimPos/Line1Pos1").GetComponent<RectTransform>();		mLine1Pos2 = mTransform.Find("AnimPos/Line1Pos2").GetComponent<RectTransform>();		mLine1Pos3 = mTransform.Find("AnimPos/Line1Pos3").GetComponent<RectTransform>();		mLine1Pos4 = mTransform.Find("AnimPos/Line1Pos4").GetComponent<RectTransform>();		mLine2Pos1 = mTransform.Find("AnimPos/Line2Pos1").GetComponent<RectTransform>();		mLine2Pos2 = mTransform.Find("AnimPos/Line2Pos2").GetComponent<RectTransform>();		mArrowPos1 = mTransform.Find("AnimPos/ArrowPos1").GetComponent<RectTransform>();		mArrowPos2 = mTransform.Find("AnimPos/ArrowPos2").GetComponent<RectTransform>();		mTextPos1 = mTransform.Find("AnimPos/TextPos1").GetComponent<RectTransform>();		mTextPos2 = mTransform.Find("AnimPos/TextPos2").GetComponent<RectTransform>();		mAnimPlanePos1 = mTransform.Find("AnimPos/AnimPlanePos1").GetComponent<RectTransform>();		mAnimPlanePos2 = mTransform.Find("AnimPos/AnimPlanePos2").GetComponent<RectTransform>();		mPOPPlanePos2 = mTransform.Find("AnimPos/POPPlanePos2").GetComponent<RectTransform>();		mPOPPlanePos1 = mTransform.Find("AnimPos/POPPlanePos1").GetComponent<RectTransform>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mBtnContinue.onClick.AddListener( OnBtnContinueClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mBtnContinue.onClick.RemoveAllListeners();
        }
    
    }
}