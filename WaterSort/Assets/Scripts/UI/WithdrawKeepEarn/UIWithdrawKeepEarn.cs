
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawKeepEarn : BaseUI
    {
        protected override void OnAwake() { }
        protected override void OnEnable()
        {
            mOMText.text = FacadePayType.RegionalChange(FacadeWithdraw.GetTotalRecordMoney());

            ShowAnim(mPlane);
        }
        	    private void OnExitBtnClickHandle()        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.OpenAsync<UIWithdrawContinue>(EUIType.EUIWithdrawContinue);
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawKeepEarn);
            });        }	    private void OnKeepEarnBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.OpenAsync<UIWithdrawContinue>(EUIType.EUIWithdrawContinue);
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawKeepEarn);
            });
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}