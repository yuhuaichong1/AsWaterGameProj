using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawProgress : BaseUI
    {	protected RectTransform mPlane;	protected Button mExitBtn;	protected Text mCurMoneyText;	protected Button mConfirmBtn;	protected WPOP mProgress1;	protected WPOP mProgress2;	protected Text mP2_3_Title;	protected RectTransform mP2_4_Content;	protected Text mP2_4_ErrorContent;	protected WPOP mProgress3;	protected Text mP3_4_Title;	protected RectTransform mP3_3_Content;	protected Text mP3_3_ErrorContent;	protected Text mP3_3_Title;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();		mCurMoneyText = mTransform.Find("Plane/CurMoneyBg/CurMoneyText").GetComponent<Text>();		mConfirmBtn = mTransform.Find("Plane/ConfirmBtn").GetComponent<Button>();		mProgress1 = mTransform.Find("Plane/OrderDetials/Progress1").GetComponent<WPOP>();		mProgress2 = mTransform.Find("Plane/OrderDetials/Progress2").GetComponent<WPOP>();		mP2_3_Title = mTransform.Find("Plane/OrderDetials/Progress2/Progress2_3/P2_3_Title").GetComponent<Text>();		mP2_4_Content = mTransform.Find("Plane/OrderDetials/Progress2/Progress2_4/P2_4_Content").GetComponent<RectTransform>();		mP2_4_ErrorContent = mTransform.Find("Plane/OrderDetials/Progress2/Progress2_4/P2_4_ErrorContent").GetComponent<Text>();		mProgress3 = mTransform.Find("Plane/OrderDetials/Progress3").GetComponent<WPOP>();		mP3_4_Title = mTransform.Find("Plane/OrderDetials/Progress3/Progress3_4/P3_4_Title").GetComponent<Text>();		mP3_3_Content = mTransform.Find("Plane/OrderDetials/Progress3/Progress3_3/P3_3_Content").GetComponent<RectTransform>();		mP3_3_ErrorContent = mTransform.Find("Plane/OrderDetials/Progress3/Progress3_3/P3_3_ErrorContent").GetComponent<Text>();		mP3_3_Title = mTransform.Find("Plane/OrderDetials/Progress3/Progress3_3/P3_3_Title").GetComponent<Text>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);		mConfirmBtn.onClick.AddListener( OnConfirmBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mExitBtn.onClick.RemoveAllListeners();		mConfirmBtn.onClick.RemoveAllListeners();
        }
    
    }
}