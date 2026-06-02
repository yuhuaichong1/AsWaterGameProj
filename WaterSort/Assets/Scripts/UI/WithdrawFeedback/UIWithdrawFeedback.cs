
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawFeedback : BaseUI
    {
        protected override void OnAwake() 
        {
            mAddressOrPhoneInput.placeholder.GetComponent<Text>().text = FacadeLanguage.GetText("10059");
        }
        protected override void OnEnable()
        {
            ShowAnim(mPlane);
        }
        	    private void OnExitBtnClickHandle()        {            HideAnim(mPlane, () =>             {                 UIManager.Instance.CloseUI(EUIType.EUIFeedback);            });        }	    private void OnConfirmBtnClickHandle()
        {
            if(string.IsNullOrEmpty(mAddressOrPhoneInput.text))
            {
                UIManager.Instance.OpenNotice(FacadeLanguage.GetText("10059"));
            }
            else
            {
                if(mAddressOrPhoneInput.text.IfEmail() || mAddressOrPhoneInput.text.IfPhoneNumber())
                {
                    HideAnim(mPlane, () =>
                    {
                        UIManager.Instance.CloseUI(EUIType.EUIFeedback);
                        UIManager.Instance.CloseUI(EUIType.EUIEnterInfomation);

                        FacadeWithdraw.SetWName("");
                        FacadeWithdraw.SetWPhoneOrEmail(mAddressOrPhoneInput.text);
                        FacadeWithdraw.SetPayType(EPayType.Other);

                        UIManager.Instance.OpenAsync<UIWithdrawConfirm>(EUIType.EUIConfirm);
                    });
                }
                else
                {
                    UIManager.Instance.OpenNotice(FacadeLanguage.GetText("10062"));
                }
            }
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}