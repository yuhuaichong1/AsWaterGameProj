using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIProgressPanel : BaseUI
    {
	protected RectTransform mPlane;
	protected LanguageText mLabTitle;
	protected RectTransform mLineConnect;
	protected Image mPreviousState;
	protected LanguageText mPreviousCondition;
	protected Image mNextState;
	protected LanguageText mNextCondition;
	protected LanguageText mRuleExplain;
	protected Button mBtnContinue;
	protected LanguageText mLabContinue;

        protected override void LoadPanel()
        {
            base.LoadPanel();
            
		mPlane = mTransform.Find("Panel").GetComponent<RectTransform>();
		mLabTitle = mTransform.Find("Panel/Top/LabTitle").GetComponent<LanguageText>();
		mLineConnect = mTransform.Find("Panel/Top/Previous/LineConnect").GetComponent<RectTransform>();
		mPreviousState = mTransform.Find("Panel/Top/Previous/PreviousState").GetComponent<Image>();
		mPreviousCondition = mTransform.Find("Panel/Top/Previous/PreviousCondition").GetComponent<LanguageText>();
		mNextState = mTransform.Find("Panel/Top/Next/NextState").GetComponent<Image>();
		mNextCondition = mTransform.Find("Panel/Top/Next/NextCondition").GetComponent<LanguageText>();
		mRuleExplain = mTransform.Find("Panel/Buttom/ContentBox/RuleExplain").GetComponent<LanguageText>();
		mBtnContinue = mTransform.Find("Panel/Buttom/BtnContinue").GetComponent<Button>();
		mLabContinue = mTransform.Find("Panel/Buttom/BtnContinue/LabContinue").GetComponent<LanguageText>();
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