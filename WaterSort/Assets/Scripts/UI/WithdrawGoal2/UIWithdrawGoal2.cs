
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIWithdrawGoal2 : BaseUI
    {
        protected override void OnAwake() { }
        protected override void OnEnable() { }
        	    private void OnExitBtnClickHandle()        {            UIManager.Instance.CloseUI(EUIType.EUIWithdrawGoal2);        }	    private void OnSetCurWInfoBtnClickHandle()
        {
            //FacadeWithdraw.CheckOpenUI();
        }

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}