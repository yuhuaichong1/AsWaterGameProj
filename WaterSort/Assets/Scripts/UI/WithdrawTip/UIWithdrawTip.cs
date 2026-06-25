
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawTip : BaseUI
    {
        protected override void OnAwake() { }
        protected override void OnEnable()
        {
            //mWTypeIcon.sprite = ;
            //mTargetMoney.text = ;
            
            ShowAnim(mPlane);
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