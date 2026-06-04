
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UILuckySpin : BaseUI
    {
        protected override void OnAwake() { }
        protected override void OnEnable() { }
        
	    private void OnLotteryBtnClickHandle(){}
	    private void OnExitBtnClickHandle()
        {
            UIManager.Instance.CloseUI(EUIType.EUILevelCompleted);
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}