
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawConfirm2 : BaseUI
    {
        private PayoutEntryKey entryKey;
        private Action onRefreshParent;
        private float amount;
        private Sprite icon;

        protected override void OnAwake() { }
        protected override void OnEnable()
        {
            ShowAnim(mPlane);
            mIcon.sprite = icon;
            mO1Content.text = FacadeWithdraw.GetWPhoneOrEmail2(entryKey.Channel);
            mO2Content.text = FacadePayType.RegionalChange(amount);
            mO3Content.text = "2%";
            mO4Content.text = DateTime.Now.ToString("M/d/yyyy h:mm:ss tt");
        }

        protected override void OnSetParam(params object[] args)
        {
            entryKey = (PayoutEntryKey)args[0];
            onRefreshParent = (Action)args[1];
            amount = (float)args[2];
            icon = (Sprite)args[3];
        }

        private void OnWithdrawBtnClickHandle()        {
            HideAnim(mPlane, () =>
            {
                if (FacadePayout.EnsureStartedAndOpen(entryKey))
                    onRefreshParent?.Invoke();

                UIManager.Instance.CloseUI(EUIType.EUIWithdrawConfirm2);
            });        }	    private void OnExitBtnClickHandle()
        {
            HideAnim(mPlane, () =>
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawConfirm2);
            });
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}