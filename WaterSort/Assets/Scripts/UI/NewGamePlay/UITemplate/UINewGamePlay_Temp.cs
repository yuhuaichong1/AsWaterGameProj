using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UINewGamePlay : BaseUI
    {	protected RectTransform mPlane;	protected Image mGPIcon1;	protected Image mGPIcon2;	protected RectTransform mContentText1;	protected RectTransform mContentText2;	protected Button mContinueBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mGPIcon1 = mTransform.Find("Plane/LightBg/GPIcon1").GetComponent<Image>();		mGPIcon2 = mTransform.Find("Plane/LightBg/GPIcon2").GetComponent<Image>();		mContentText1 = mTransform.Find("Plane/ContentText/ContentText1").GetComponent<RectTransform>();		mContentText2 = mTransform.Find("Plane/ContentText/ContentText2").GetComponent<RectTransform>();		mContinueBtn = mTransform.Find("Plane/ContinueBtn").GetComponent<Button>();
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