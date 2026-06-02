using UnityEngine;
using UnityEngine.UI;
using AsGame.UI;

namespace AsGame.UI.Popups
{
    public class GetHeartPopupView : BasePopupView
    {
        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            if (PopupLayoutHelper.HasPrefabLayout(content))
            {
                var type = ctx.Payload is int t ? t : 1;
                var msg = type == 2 ? "获得无限体力!" : "体力已回满!";
                var label = content.Find("top/label")?.GetComponent<Text>()
                            ?? content.Find("label")?.GetComponent<Text>();
                if (label != null)
                    label.text = msg;

                PopupLayoutHelper.BindButton(content, "btnHome", Close);
                base.Setup(ctx, content, blocker);
                return;
            }

            base.Setup(ctx, content, blocker);
            var type2 = ctx.Payload is int t2 ? t2 : 1;
            var msg2 = type2 == 2 ? "获得无限体力!" : "体力已回满!";
            UIFactory.CreateLabel(content, msg2, 36, new Vector2(0, 80));
            UIFactory.CreateButton(content, "好的", new Vector2(0, -80), new Vector2(240, 56))
                .onClick.AddListener(Close);
        }
    }
}
