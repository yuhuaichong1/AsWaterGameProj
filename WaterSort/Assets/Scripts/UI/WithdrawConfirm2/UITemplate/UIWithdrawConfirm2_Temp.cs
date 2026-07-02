using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawConfirm2 : BaseUI
    {	protected RectTransform mPlane;	protected Image mIcon;	protected Text mMoneyText;	protected Text mO1Content;	protected Text mO2Content;	protected Text mO3Content;	protected Text mO4Content;	protected Button mWithdrawBtn;	protected Button mExitBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mIcon = mTransform.Find("Plane/Bg3/Mask/Icon").GetComponent<Image>();		mMoneyText = mTransform.Find("Plane/Bg3/Money/MoneyText").GetComponent<Text>();		mO1Content = mTransform.Find("Plane/OrderInfoText/O1/O1Content").GetComponent<Text>();		mO2Content = mTransform.Find("Plane/OrderInfoText/O2/O2Content").GetComponent<Text>();		mO3Content = mTransform.Find("Plane/OrderInfoText/O3/O3Content").GetComponent<Text>();		mO4Content = mTransform.Find("Plane/OrderInfoText/O4/O4Content").GetComponent<Text>();		mWithdrawBtn = mTransform.Find("Plane/WithdrawBtn").GetComponent<Button>();		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mWithdrawBtn.onClick.AddListener( OnWithdrawBtnClickHandle);		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mWithdrawBtn.onClick.RemoveAllListeners();		mExitBtn.onClick.RemoveAllListeners();
        }
    
    }
}