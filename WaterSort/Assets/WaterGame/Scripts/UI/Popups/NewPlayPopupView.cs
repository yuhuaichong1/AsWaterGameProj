using UnityEngine;
using AsGame.Core;
using XrCode;

namespace AsGame.UI.Popups
{
    public class NewPlayPopupView : BasePopupView
    {
        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            if (PopupLayoutHelper.HasPrefabLayout(content))
            {
                PopupLayoutHelper.BindButton(content, "btnClose", Close);
                PopupLayoutHelper.BindButton(content, "btnKnow", Close);
                base.Setup(ctx, content, blocker);
                return;
            }

            base.Setup(ctx, content, blocker);
            var key = ctx.Payload is int k ? k : 1;
            string desc = key switch
            {
                1 => GameConstants.NewPlayUnlockHints.TryGetValue(1, out var a) ? a : "新玩法解锁",
                2 => GameConstants.NewPlayUnlockHints.TryGetValue(2, out var b) ? b : "新玩法解锁",
                3 => GameConstants.NewPlayUnlockHints.TryGetValue(3, out var c) ? c : "新玩法解锁",
                _ => "新玩法解锁"
            };

            UIFactory.CreateLabel(content, "新玩法", 40, new Vector2(0, 240));
            UIFactory.CreateLabel(content, desc, 26, new Vector2(0, 80), new Vector2(560, 200));
            UIFactory.CreateButton(content, "知道了", new Vector2(0, -160), new Vector2(260, 56))
                .onClick.AddListener(Close);
        }
    }
}
