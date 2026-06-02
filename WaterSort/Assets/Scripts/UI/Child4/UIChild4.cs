
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIChild4 : BaseUI
    {
        protected override void OnAwake() { }
        protected override void OnEnable() { }
        
	    private void OnExitBtnClickHandle()
        {
            UIManager.Instance.CloseUI(UIType, UICloseType.Pop);
        }
	    private void OnGoBtn1ClickHandle()
        {
            UIManager.Instance.OpenAsync<UIChild1>(EUIType.EUIChild1, UIOpenType.Back);
        }

        private void OnGoBtn2ClickHandle()
        {
            UIManager.Instance.CloseUI(UIType, UICloseType.Clear);
        }
        

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}