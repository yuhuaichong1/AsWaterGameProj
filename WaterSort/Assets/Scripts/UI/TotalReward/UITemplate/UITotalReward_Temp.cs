using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UITotalReward : BaseUI
    {	protected RectTransform mPlane;	protected Text mMoneyText;	protected Text mContent;	protected Slider mLevelSlider;	protected Text mSliderText;	protected Button mClaimBtn;	protected Button mExitBtn;	protected RectTransform mUnClaimBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mMoneyText = mTransform.Find("Plane/Bg3/Money/MoneyText").GetComponent<Text>();		mContent = mTransform.Find("Plane/Content").GetComponent<Text>();		mLevelSlider = mTransform.Find("Plane/LevelSlider").GetComponent<Slider>();		mSliderText = mTransform.Find("Plane/LevelSlider/SliderText").GetComponent<Text>();		mClaimBtn = mTransform.Find("Plane/ClaimBtn").GetComponent<Button>();		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();		mUnClaimBtn = mTransform.Find("Plane/UnClaimBtn").GetComponent<RectTransform>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mClaimBtn.onClick.AddListener( OnClaimBtnClickHandle);		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mClaimBtn.onClick.RemoveAllListeners();		mExitBtn.onClick.RemoveAllListeners();
        }
    
    }
}