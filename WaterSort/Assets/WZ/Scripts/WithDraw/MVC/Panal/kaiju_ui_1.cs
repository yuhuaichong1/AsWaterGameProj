using UnityEngine;
using UnityEngine.UI;

namespace WZSDK
{
    public class kaiju_ui_1 : BaseViewWithDraw
    {
        protected override void OnAwake()
        {
            var continueBtn = Find<Button>("continue");
            if (continueBtn != null)
                continueBtn.onClick.AddListener(onButtonClick);

            // 兼容 Start Game 绑定在其它按钮名上的情况
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (btn == null || btn == continueBtn)
                    continue;
                string n = btn.gameObject.name.ToLowerInvariant();
                if (n.Contains("continue") || n.Contains("start") || n.Contains("play"))
                    btn.onClick.AddListener(onButtonClick);
            }
        }

        protected override void HandleViewArgs(object[] args)
        {
            base.HandleViewArgs(args);
            WaterSortWZBridge.NotifyLevel1IntroOpened();
        }

        public override void InitView()
        {
            base.InitView();
            WaterSortWZBridge.NotifyLevel1IntroOpened();
        }

        public void onButtonClick()
        {
            GameApp.viewManager.Close(ViewId);
            WaterSortWZBridge.NotifyLevel1IntroDismissed();
        }
    }
}
