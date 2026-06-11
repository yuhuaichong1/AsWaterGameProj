using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UILuckyReward : BaseUI
    {	protected RectTransform mPlane;	protected Text mMoneyText;	protected RectTransform mMoneyIcon;	protected RectTransform mIAAMoneyIcon;	protected Button mAdBtn;	protected Button mOnlyBtn;	protected Text mOnlyText;	protected RectTransform mMoneyRewardBigIcon;	protected RectTransform mIAAMoneyRewardBigIcon;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mMoneyText = mTransform.Find("Plane/MoneyText").GetComponent<Text>();		mMoneyIcon = mTransform.Find("Plane/MoneyText/MoneyIcon").GetComponent<RectTransform>();		mIAAMoneyIcon = mTransform.Find("Plane/MoneyText/IAAMoneyIcon").GetComponent<RectTransform>();		mAdBtn = mTransform.Find("Plane/AdBtn").GetComponent<Button>();		mOnlyBtn = mTransform.Find("Plane/OnlyText/OnlyBtn").GetComponent<Button>();		mOnlyText = mTransform.Find("Plane/OnlyText").GetComponent<Text>();		mMoneyRewardBigIcon = mTransform.Find("Plane/CenterIcon/MoneyRewardBigIcon").GetComponent<RectTransform>();		mIAAMoneyRewardBigIcon = mTransform.Find("Plane/CenterIcon/IAAMoneyRewardBigIcon").GetComponent<RectTransform>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mAdBtn.onClick.AddListener( OnAdBtnClickHandle);		mOnlyBtn.onClick.AddListener( OnOnlyBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mAdBtn.onClick.RemoveAllListeners();		mOnlyBtn.onClick.RemoveAllListeners();
        }
    
    }
}