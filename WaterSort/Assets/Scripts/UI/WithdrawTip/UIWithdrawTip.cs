
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawTip : BaseUI
    {
        private PayoutEntryKey entryKey;
        private Action onRefreshParent;
        

        protected override void OnAwake() { }
        protected override void OnEnable()
        {
            //mWTypeIcon.sprite = ;
            //mTargetMoney.text = ;
            
            ShowAnim(mPlane);
        }

        protected override void OnSetParam(params object[] args)
        {
            entryKey = (PayoutEntryKey)args[0];
            onRefreshParent = (Action)args[1];
            
        }

        private void OnExitBtnClickHandle()        {            HideAnim(mPlane, () =>             {                UIManager.Instance.CloseUI(EUIType.EUIWithdrawTip);            });        }	    private void OnCashOutBtnClickHandle()
        {
            HideAnim(mPlane, () => 
            {
                UIManager.Instance.CloseUI(EUIType.EUIWithdrawTip);
                UIManager.Instance.OpenAsync<UIWithdrawGoal2>(EUIType.EUIWithdrawGoal2);
            });
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}