using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIDateShow : BaseUI
    {	protected RectTransform mPlane;	protected Image mGPIcon;	protected Text mContentText;	protected Text mTimeText;	protected Button mContinueBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mGPIcon = mTransform.Find("Plane/GPIcon").GetComponent<Image>();		mContentText = mTransform.Find("Plane/ContentText").GetComponent<Text>();		mTimeText = mTransform.Find("Plane/TimeText").GetComponent<Text>();		mContinueBtn = mTransform.Find("ContinueBtn").GetComponent<Button>();
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