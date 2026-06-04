using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UILuckySpin : BaseUI
    {	protected RectTransform mPlane;	protected RectTransform mSpinBg;	protected Text mTTItem1name;	protected Image mTTItem1Icon;	protected Text mTTItem2name;	protected Image mTTItem2Icon;	protected Text mTTItem3name;	protected Image mTTItem3Icon;	protected Text mTTItem4name;	protected Image mTTItem4Icon;	protected Text mTTItem5name;	protected Image mTTItem5Icon;	protected Text mTTItem6name;	protected Image mTTItem6Icon;	protected Button mLotteryBtn;	protected Button mExitBtn;	protected RectTransform mBlockMask;
        protected override void LoadPanel()
        {
            base.LoadPanel();
            		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();		mSpinBg = mTransform.Find("Plane/Turntable/SpinBg").GetComponent<RectTransform>();		mTTItem1name = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem1/TTItem1name").GetComponent<Text>();		mTTItem1Icon = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem1/TTItem1Icon").GetComponent<Image>();		mTTItem2name = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem2/TTItem2name").GetComponent<Text>();		mTTItem2Icon = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem2/TTItem2Icon").GetComponent<Image>();		mTTItem3name = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem3/TTItem3name").GetComponent<Text>();		mTTItem3Icon = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem3/TTItem3Icon").GetComponent<Image>();		mTTItem4name = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem4/TTItem4name").GetComponent<Text>();		mTTItem4Icon = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem4/TTItem4Icon").GetComponent<Image>();		mTTItem5name = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem5/TTItem5name").GetComponent<Text>();		mTTItem5Icon = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem5/TTItem5Icon").GetComponent<Image>();		mTTItem6name = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem6/TTItem6name").GetComponent<Text>();		mTTItem6Icon = mTransform.Find("Plane/Turntable/SpinBg/Table/TTItem6/TTItem6Icon").GetComponent<Image>();		mLotteryBtn = mTransform.Find("Plane/LotteryBtn").GetComponent<Button>();		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();		mBlockMask = mTransform.Find("BlockMask").GetComponent<RectTransform>();
        }
    
        protected override void BindButtonEvent() 
        {
            		mLotteryBtn.onClick.AddListener( OnLotteryBtnClickHandle);		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);
        }
    
        protected override void UnBindButtonEvent() 
        {
            		mLotteryBtn.onClick.RemoveAllListeners();		mExitBtn.onClick.RemoveAllListeners();
        }
    
    }
}