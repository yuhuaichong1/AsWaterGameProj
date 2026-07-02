using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UITotalReward3 : BaseUI
    {	protected RectTransform mPlane;	protected Text mLuckyPlayerItem;	protected Button mClaimBtn;	protected Text mCPText;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mLuckyPlayerItem = mTransform.Find("Plane/WinnerList/Scroll View/Viewport/Content/LuckyPlayerItem").GetComponent<Text>();		mClaimBtn = mTransform.Find("Plane/ClaimBtn").GetComponent<Button>();		mCPText = mTransform.Find("Plane/ConsolationPrize/CPText").GetComponent<Text>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mClaimBtn.onClick.AddListener( OnClaimBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mClaimBtn.onClick.RemoveAllListeners();
        }
    
    }
}