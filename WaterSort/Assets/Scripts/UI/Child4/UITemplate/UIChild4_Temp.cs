using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIChild4 : BaseUI
    {
	protected RectTransform mPlane;
	protected Button mExitBtn;
	protected Button mGoBtn1;
	protected Button mGoBtn2;

        protected override void LoadPanel()
        {
            base.LoadPanel();
            
		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();
		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();
		mGoBtn1 = mTransform.Find("Plane/GoBtn1").GetComponent<Button>();
		mGoBtn2 = mTransform.Find("Plane/GoBtn2").GetComponent<Button>();
        }
    
        protected override void BindButtonEvent() 
        {
            
		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);
		mGoBtn1.onClick.AddListener( OnGoBtn1ClickHandle);
		mGoBtn2.onClick.AddListener( OnGoBtn2ClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            
		mExitBtn.onClick.RemoveAllListeners();
		mGoBtn1.onClick.RemoveAllListeners();
		mGoBtn2.onClick.RemoveAllListeners();
        }
    
    }
}