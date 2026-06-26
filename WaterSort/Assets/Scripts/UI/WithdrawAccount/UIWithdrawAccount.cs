
using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawAccount : BaseUI
    {
        EPayType curChannel;

        protected override void OnAwake()
        {
            mIIPlaceholder.text = FacadeLanguage.GetText("10147");
        }

        protected override void OnSetParam(params object[] args)
        {
            curChannel = (EPayType)args[0];
        }

        protected override void OnEnable()
        {
            mInfoInoutField.text = FacadeWithdraw.GetWPhoneOrEmail2(curChannel);
            List<PayNode> nodes = FacadePayType.GetPayItems();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (curChannel == nodes[i].payType)
                {
                    mIcon.sprite = nodes[i].picture;
                }
            }
            ShowAnim(mPlane);
        }
        	    private void OnSubmitBtnClickHandle()        {            if(string.IsNullOrEmpty(mInfoInoutField.text))
            {
                UIManager.Instance.OpenNotice2(FacadeLanguage.GetText("10059"));
            }            else
            {
                if(mInfoInoutField.text.IfEmail() || mInfoInoutField.text.IfPhoneNumber())
                {
                    HideAnim(mPlane, () =>
                    {
                        UIManager.Instance.CloseUI(EUIType.EUIWithdrawAccount);
                        FacadeWithdraw.SetWPhoneOrEmail2(curChannel, mInfoInoutField.text);
                    });
                }
                else
                {
                    UIManager.Instance.OpenNotice3(FacadeLanguage.GetText("10062"));
                }
            }                    }	    private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawAccount);
            });
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}