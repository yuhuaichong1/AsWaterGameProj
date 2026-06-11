using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIDateShow : BaseUI
    {	protected RectTransform mPlane;	protected Image mGPIcon;	protected Text mContentText;	protected Button mContinueBtn;	protected RectTransform mTitle;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mGPIcon = mTransform.Find("Plane/LightBg/GPIcon").GetComponent<Image>();		mContentText = mTransform.Find("Plane/ContentText").GetComponent<Text>();		mContinueBtn = mTransform.Find("ContinueBtn").GetComponent<Button>();		mTitle = mTransform.Find("Plane/Title").GetComponent<RectTransform>();
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