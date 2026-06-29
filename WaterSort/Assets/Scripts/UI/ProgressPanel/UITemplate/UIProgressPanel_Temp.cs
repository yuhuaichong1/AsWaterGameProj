using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIProgressPanel : BaseUI
    {
	protected LanguageText mLabTitle;
	protected RectTransform mLineConnect;
	protected Image mPreviousState;
	protected LanguageText mPreviousCondition;
	protected Image mNextState;
	protected LanguageText mNextCondition;
	protected LanguageText mRuleExplain;
	protected Button mBtnContinue;
	protected LanguageText mLabContinue;
	protected RectTransform mPayOutPanel;
	protected RectTransform mLine1;
	protected RectTransform mBox;
	protected RectTransform mLine2;
	protected LanguageText mPrevioursStepFinish;
	protected RectTransform mArrow;
	protected RectTransform mAnimPanel;

        protected override void LoadPanel()
        {
            base.LoadPanel();
            
		mLabTitle = mTransform.Find("Panel/PayOutPanel/Top/LabTitle").GetComponent<LanguageText>();
		mLineConnect = mTransform.Find("Panel/PayOutPanel/Top/Previous/LineConnect").GetComponent<RectTransform>();
		mPreviousState = mTransform.Find("Panel/PayOutPanel/Top/Previous/PreviousState").GetComponent<Image>();
		mPreviousCondition = mTransform.Find("Panel/PayOutPanel/Top/Previous/PreviousCondition").GetComponent<LanguageText>();
		mNextState = mTransform.Find("Panel/PayOutPanel/Top/Next/NextState").GetComponent<Image>();
		mNextCondition = mTransform.Find("Panel/PayOutPanel/Top/Next/NextCondition").GetComponent<LanguageText>();
		mRuleExplain = mTransform.Find("Panel/PayOutPanel/Buttom/ContentBox/RuleExplain").GetComponent<LanguageText>();
		mBtnContinue = mTransform.Find("Panel/PayOutPanel/Buttom/BtnContinue").GetComponent<Button>();
		mLabContinue = mTransform.Find("Panel/PayOutPanel/Buttom/BtnContinue/LabContinue").GetComponent<LanguageText>();
		mPayOutPanel = mTransform.Find("Panel/PayOutPanel").GetComponent<RectTransform>();
		mLine1 = mTransform.Find("Panel/AnimPanel/Line1").GetComponent<RectTransform>();
		mBox = mTransform.Find("Panel/AnimPanel/Box").GetComponent<RectTransform>();
		mLine2 = mTransform.Find("Panel/AnimPanel/Line2").GetComponent<RectTransform>();
		mPrevioursStepFinish = mTransform.Find("Panel/AnimPanel/Box/PrevioursStepFinish").GetComponent<LanguageText>();
		mArrow = mTransform.Find("Panel/AnimPanel/Box/Arrow").GetComponent<RectTransform>();
		mAnimPanel = mTransform.Find("Panel/AnimPanel").GetComponent<RectTransform>();
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