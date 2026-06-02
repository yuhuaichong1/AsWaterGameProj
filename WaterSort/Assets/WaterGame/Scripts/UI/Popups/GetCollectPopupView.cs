using System.Collections;
using UnityEngine;
using AsGame.Data;

namespace AsGame.UI.Popups
{
    public class GetCollectPopupView : BasePopupView
    {
        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            var entry = ctx.Payload as CollectEntry? ?? CollectCatalog.Drinks[0];

            if (PopupLayoutHelper.HasPrefabLayout(content))
            {
                PopupLayoutHelper.SetText(content, "top/label", "获得收藏");
                PopupLayoutHelper.SetText(content, "label", entry.name);
                base.Setup(ctx, content, blocker);
                StartCoroutine(AutoClose());
                return;
            }

            base.Setup(ctx, content, blocker);
            UIFactory.CreateLabel(content, "获得收藏", 40, new Vector2(0, 260));
            UIFactory.CreateLabel(content, entry.name, 36, new Vector2(0, 120));
            StartCoroutine(AutoClose());
        }

        IEnumerator AutoClose()
        {
            yield return new WaitForSeconds(1.5f);
            Close();
            PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.Success });
        }
    }
}
