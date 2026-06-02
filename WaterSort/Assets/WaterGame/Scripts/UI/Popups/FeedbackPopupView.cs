using UnityEngine;
using UnityEngine.UI;
using AsGame.Platform;

namespace AsGame.UI.Popups
{
    public class FeedbackPopupView : BasePopupView
    {
        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            base.Setup(ctx, content, blocker);
            if (content.childCount > 0)
            {
                var close = content.Find("btnClose")?.GetComponent<Button>();
                if (close != null)
                {
                    close.onClick.RemoveAllListeners();
                    close.onClick.AddListener(Close);
                }

                return;
            }

            UIFactory.CreateLabel(content, "意见反馈", 40, new Vector2(0, 280));

            var inputGo = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputGo.transform.SetParent(content, false);
            var irt = inputGo.GetComponent<RectTransform>();
            irt.anchoredPosition = new Vector2(0, 80);
            irt.sizeDelta = new Vector2(540, 200);
            inputGo.GetComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(inputGo.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = DefaultFont;
            text.supportRichText = false;
            text.color = Color.white;
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10, 10);
            trt.offsetMax = new Vector2(-10, -10);

            var field = inputGo.GetComponent<InputField>();
            field.textComponent = text;
            field.lineType = InputField.LineType.MultiLineNewline;

            UIFactory.CreateButton(content, "提交", new Vector2(-120, -160), new Vector2(200, 56))
                .onClick.AddListener(() =>
                {
                    PlatformService.SendEvent("feedback_submit", field.text);
                    Close();
                });

            UIFactory.CreateButton(content, "关闭", new Vector2(120, -160), new Vector2(200, 56))
                .onClick.AddListener(Close);
        }
    }
}
