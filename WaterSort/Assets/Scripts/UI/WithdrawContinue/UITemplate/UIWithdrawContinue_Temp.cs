using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawContinue : BaseUI
    {	protected RectTransform mPlane;	protected Text mWithText;	protected RectTransform mChat_1_GrayBg;	protected Text mChat_1_Money;	protected Text mChat_1_Statu;	protected RectTransform mChat_2_GrayBg;	protected Text mChat_2_Money;	protected Text mChat_2_Statu;	protected RectTransform mChat_3_GrayBg;	protected Text mChat_3_Money;	protected Text mChat_3_Statu;	protected Button mContiuneBtn;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mWithText = mTransform.Find("Plane/WithText").GetComponent<Text>();		mChat_1_GrayBg = mTransform.Find("Plane/OrderMoney/Chat_1/PanelA/Chat_1_GrayBg").GetComponent<RectTransform>();		mChat_1_Money = mTransform.Find("Plane/OrderMoney/Chat_1/PanelA/Chat_1_Money").GetComponent<Text>();		mChat_1_Statu = mTransform.Find("Plane/OrderMoney/Chat_1/PanelA/Chat_1_Statu").GetComponent<Text>();		mChat_2_GrayBg = mTransform.Find("Plane/OrderMoney/Chat_2/PanelA/Chat_2_GrayBg").GetComponent<RectTransform>();		mChat_2_Money = mTransform.Find("Plane/OrderMoney/Chat_2/PanelA/Chat_2_Money").GetComponent<Text>();		mChat_2_Statu = mTransform.Find("Plane/OrderMoney/Chat_2/PanelA/Chat_2_Statu").GetComponent<Text>();		mChat_3_GrayBg = mTransform.Find("Plane/OrderMoney/Chat_3/PanelA/Chat_3_GrayBg").GetComponent<RectTransform>();		mChat_3_Money = mTransform.Find("Plane/OrderMoney/Chat_3/PanelA/Chat_3_Money").GetComponent<Text>();		mChat_3_Statu = mTransform.Find("Plane/OrderMoney/Chat_3/PanelA/Chat_3_Statu").GetComponent<Text>();		mContiuneBtn = mTransform.Find("Plane/ContiuneBtn").GetComponent<Button>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mContiuneBtn.onClick.AddListener( OnContiuneBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mContiuneBtn.onClick.RemoveAllListeners();
        }
    
    }
}