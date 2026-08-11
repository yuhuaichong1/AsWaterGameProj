using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIWithdrawEnterInfo : BaseUI
    {
	protected Button mExitBtn;
	protected Button mHelpBtn;
	protected Toggle mPayType1Toggle;
	protected Image mPY1Icon;
	protected Toggle mPayType2Toggle;
	protected Image mPY2Icon;
	protected Toggle mPayType3Toggle;
	protected Image mPY3Icon;
	protected Toggle mPayType4Toggle;
	protected Image mPY4Icon;
	protected Toggle mPayType5Toggle;
	protected Image mPY5Icon;
	protected Toggle mPayType6Toggle;
	protected Image mPY6Icon;
	protected InputField mNameInput;
	protected InputField mAddressInput;
	protected InputField mAddressOrPhoneInput;
	protected RectTransform mPhone;
	protected Text mAreaCodeText;
	protected InputField mPhoneInput;
	protected Button mConfirmBtn;
	protected RectTransform mPlane;
	protected RectTransform mPhoneAndCpf;
	protected InputField mPhoneAndCpfInput;
	protected InputField mCpfInput;
	protected InputField mCPFNameInput;

        protected override void LoadPanel()
        {
            base.LoadPanel();
            
		mExitBtn = mTransform.Find("Plane/ExitBtn").GetComponent<Button>();
		mHelpBtn = mTransform.Find("Plane/Payment/HelpBtn").GetComponent<Button>();
		mPayType1Toggle = mTransform.Find("Plane/Payment/ToggleGroup/PayType1Toggle").GetComponent<Toggle>();
		mPY1Icon = mTransform.Find("Plane/Payment/ToggleGroup/PayType1Toggle/PY1Icon").GetComponent<Image>();
		mPayType2Toggle = mTransform.Find("Plane/Payment/ToggleGroup/PayType2Toggle").GetComponent<Toggle>();
		mPY2Icon = mTransform.Find("Plane/Payment/ToggleGroup/PayType2Toggle/PY2Icon").GetComponent<Image>();
		mPayType3Toggle = mTransform.Find("Plane/Payment/ToggleGroup/PayType3Toggle").GetComponent<Toggle>();
		mPY3Icon = mTransform.Find("Plane/Payment/ToggleGroup/PayType3Toggle/PY3Icon").GetComponent<Image>();
		mPayType4Toggle = mTransform.Find("Plane/Payment/ToggleGroup/PayType4Toggle").GetComponent<Toggle>();
		mPY4Icon = mTransform.Find("Plane/Payment/ToggleGroup/PayType4Toggle/PY4Icon").GetComponent<Image>();
		mPayType5Toggle = mTransform.Find("Plane/Payment/ToggleGroup/PayType5Toggle").GetComponent<Toggle>();
		mPY5Icon = mTransform.Find("Plane/Payment/ToggleGroup/PayType5Toggle/PY5Icon").GetComponent<Image>();
		mPayType6Toggle = mTransform.Find("Plane/Payment/ToggleGroup/PayType6Toggle").GetComponent<Toggle>();
		mPY6Icon = mTransform.Find("Plane/Payment/ToggleGroup/PayType6Toggle/PY6Icon").GetComponent<Image>();
		mNameInput = mTransform.Find("Plane/NameInput").GetComponent<InputField>();
		mAddressInput = mTransform.Find("Plane/AddressInput").GetComponent<InputField>();
		mAddressOrPhoneInput = mTransform.Find("Plane/AddressOrPhoneInput").GetComponent<InputField>();
		mPhone = mTransform.Find("Plane/Phone").GetComponent<RectTransform>();
		mAreaCodeText = mTransform.Find("Plane/Phone/PhoneAreaCode/AreaCodeText").GetComponent<Text>();
		mPhoneInput = mTransform.Find("Plane/Phone/PhoneInput").GetComponent<InputField>();
		mConfirmBtn = mTransform.Find("Plane/ConfirmBtn").GetComponent<Button>();
		mPlane = mTransform.Find("Plane").GetComponent<RectTransform>();
		mPhoneAndCpf = mTransform.Find("Plane/PhoneAndCpf").GetComponent<RectTransform>();
		mPhoneAndCpfInput = mTransform.Find("Plane/PhoneAndCpf/PhoneAndCpfInput").GetComponent<InputField>();
		mCpfInput = mTransform.Find("Plane/PhoneAndCpf/CpfInput").GetComponent<InputField>();
		mCPFNameInput = mTransform.Find("Plane/PhoneAndCpf/CPFNameInput").GetComponent<InputField>();
        }
    
        protected override void BindButtonEvent() 
        {
            
		mExitBtn.onClick.AddListener( OnExitBtnClickHandle);
		mHelpBtn.onClick.AddListener( OnHelpBtnClickHandle);
		mConfirmBtn.onClick.AddListener( OnConfirmBtnClickHandle);
			mPayType1Toggle.onValueChanged.AddListener(OnPayType1ToggleValueChanged);
            mPayType2Toggle.onValueChanged.AddListener(OnPayType2ToggleValueChanged);
            mPayType3Toggle.onValueChanged.AddListener(OnPayType3ToggleValueChanged);
            mPayType4Toggle.onValueChanged.AddListener(OnPayType4ToggleValueChanged);
            mPayType5Toggle.onValueChanged.AddListener(OnPayType5ToggleValueChanged);
            mPayType6Toggle.onValueChanged.AddListener(OnPayType6ToggleValueChanged);
        }
    
        protected override void UnBindButtonEvent() 
        {
            
		mExitBtn.onClick.RemoveAllListeners();
		mHelpBtn.onClick.RemoveAllListeners();
		mConfirmBtn.onClick.RemoveAllListeners();
        }
    
    }
}