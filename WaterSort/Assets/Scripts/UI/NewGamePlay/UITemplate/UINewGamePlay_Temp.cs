using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UINewGamePlay : BaseUI
    {	protected RectTransform mPlane;	protected RectTransform mGPIcon1;	protected RectTransform mGPIcon2;	protected RectTransform mGPIcon3;	protected RectTransform mContentText1;	protected RectTransform mContentText2;	protected RectTransform mContentText3;	protected Button mContinueBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mGPIcon1 = mTransform.Find("Plane/CenterIcon/GPIcon1").GetComponent<RectTransform>();		mGPIcon2 = mTransform.Find("Plane/CenterIcon/GPIcon2").GetComponent<RectTransform>();		mGPIcon3 = mTransform.Find("Plane/CenterIcon/GPIcon3").GetComponent<RectTransform>();		mContentText1 = mTransform.Find("Plane/ContentText/ContentText1").GetComponent<RectTransform>();		mContentText2 = mTransform.Find("Plane/ContentText/ContentText2").GetComponent<RectTransform>();		mContentText3 = mTransform.Find("Plane/ContentText/ContentText3").GetComponent<RectTransform>();		mContinueBtn = mTransform.Find("Plane/ContinueBtn").GetComponent<Button>();
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