
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawConfirm2 : BaseUI
    {
        protected override void OnAwake() { }
        protected override void OnEnable()
        {
            ShowAnim(mPlane);
        }
        	    private void OnWithdrawBtnClickHandle()        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawConfirm2);
            });        }	    private void OnExitBtnClickHandle()
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