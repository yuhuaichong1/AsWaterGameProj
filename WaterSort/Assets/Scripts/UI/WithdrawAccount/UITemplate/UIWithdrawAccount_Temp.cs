using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawAccount : BaseUI
    {	protected RectTransform mPlane;	protected Image mIcon;	protected Text mIIPlaceholder;	protected InputField mInfoInoutField;	protected Button mSubmitBtn;	protected Button mExitBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mIcon = mTransform.Find("Plane/Bg3/Icon").GetComponent<Image>();		mIIPlaceholder = mTransform.Find("Plane/InputInfo/InfoInoutField/IIPlaceholder").GetComponent<Text>();		mInfoInoutField = mTransform.Find("Plane/InputInfo/InfoInoutField").GetComponent<InputField>();		mSubmitBtn = mTransform.Find("Plane/SubmitBtn").GetComponent<Button>();		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mSubmitBtn.onClick.AddListener( OnSubmitBtnClickHandle);		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mSubmitBtn.onClick.RemoveAllListeners();		mExitBtn.onClick.RemoveAllListeners();
        }
    
    }
}