using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Events;
using AsGame.Water;
using AsGame.Scenes;

namespace AsGame.UI.Popups
{
    public class SuccessPopupView : BasePopupView
    {
        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            base.Setup(ctx, content, blocker);
            AudioManager.Instance?.PlaySfx("Win");

            var btnNext = content.Find("btnNext")?.GetComponent<Button>();
            var btnHome = content.Find("btnHome")?.GetComponent<Button>();
            if (btnNext != null && btnHome != null)
            {
                btnNext.onClick.AddListener(() =>
                {
                    Close();
                    EventBus.Publish(GameEvents.NextLevel);
                });
                btnHome.onClick.AddListener(() =>
                {
                    Close();
                    SceneFlowManager.LoadScene(SceneId.Home);
                });
                return;
            }

            UIFactory.CreateLabel(content, "过关啦!", 44, new Vector2(0, 280));
            UIFactory.CreateLabel(content, "恭喜完成本关", 28, new Vector2(0, 200));

            UIFactory.CreateButton(content, "下一关", new Vector2(0, 40), new Vector2(280, 64))
                .onClick.AddListener(() =>
                {
                    Close();
                    EventBus.Publish(GameEvents.NextLevel);
                });

            UIFactory.CreateButton(content, "返回主页", new Vector2(0, -80), new Vector2(280, 64))
                .onClick.AddListener(() =>
                {
                    Close();
                    SceneFlowManager.LoadScene(SceneId.Home);
                });
        }
    }
}
