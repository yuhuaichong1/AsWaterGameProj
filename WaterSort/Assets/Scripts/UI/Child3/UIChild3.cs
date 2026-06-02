
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIChild3 : BaseUI
    {
        protected override void OnAwake() { }
        protected override void OnEnable() { }
        
	    private void OnExitBtnClickHandle()
        {
            UIManager.Instance.CloseUI(UIType, UICloseType.Pop);
        }

	    private void OnGoBtn1ClickHandle()
        {
            UIManager.Instance.OpenAsync<UIChild4>(EUIType.EUIChild4, UIOpenType.Cover);
        }

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}