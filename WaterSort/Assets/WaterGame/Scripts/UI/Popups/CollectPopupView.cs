using UnityEngine;
using UnityEngine.UI;
using AsGame.Data;

namespace AsGame.UI.Popups
{
    public class CollectPopupView : BasePopupView
    {
        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            base.Setup(ctx, content, blocker);
            GameSaveData.CollectRed = false;
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

            UIFactory.CreateLabel(content, "图鉴", 40, new Vector2(0, 300));

            var items = CollectCatalog.Drinks;
            var y = 200f;
            foreach (var item in items)
            {
                var unlocked = GameSaveData.CurrentLevel >= item.unlockLevel;
                var state = unlocked ? "已解锁" : $"第{item.unlockLevel}关解锁";
                UIFactory.CreateLabel(content, $"{item.name}  —  {state}", 24,
                    new Vector2(0, y), new Vector2(540, 50));
                y -= 55;
            }

            UIFactory.CreateButton(content, "关闭", new Vector2(0, -300), new Vector2(240, 56))
                .onClick.AddListener(Close);
        }
    }
}
