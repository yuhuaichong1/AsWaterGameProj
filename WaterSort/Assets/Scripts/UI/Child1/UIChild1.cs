
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIChild1 : BaseUI
    {
        protected override void OnAwake() { }
        protected override void OnEnable() { }
        
	    private void OnExitBtnClickHandle()
        {
            UIManager.Instance.CloseUI(UIType, UICloseType.Pop);
        }
	    private void OnGoBtn1ClickHandle()
        {
            UIManager.Instance.OpenAsync<UIChild2>(EUIType.EUIChild2, UIOpenType.Replace);
        }
	    private void OnGoBtn2ClickHandle()
        {
            UIManager.Instance.OpenAsync<UIChild3>(EUIType.EUIChild3, UIOpenType.Replace);
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}