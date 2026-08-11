using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIUserAgreementPanel : BaseUI
    {
        protected override void OnAwake() { }

        protected override void OnEnable()
        {
            RefreshScrollContent();
        }

        private void RefreshScrollContent()
        {
            var content = mTransform.Find("Plane/Bg/Scroll View/Viewport/Content") as RectTransform;
            if (content == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            var scroll = mTransform.Find("Plane/Bg/Scroll View")?.GetComponent<ScrollRect>();
            if (scroll != null)
                scroll.verticalNormalizedPosition = 1f;
        }
        
	    private void OnExitBtnClickHandle()
        {
            UIManager.Instance.CloseUI(EUIType.EUIUserAgreement);
            UIManager.Instance.OpenAsync<UISetting>(EUIType.EUISetting);
        }
        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}
