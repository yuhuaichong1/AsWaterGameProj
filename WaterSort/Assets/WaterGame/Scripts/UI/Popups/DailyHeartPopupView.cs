using UnityEngine;
using UnityEngine.UI;
using AsGame.Ads;
using AsGame.Data;

namespace AsGame.UI.Popups
{
    /// <summary>每日奖励：原 Cocos 为分享，Google Play 版改为激励视频。</summary>
    public class DailyHeartPopupView : BasePopupView
    {
        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            if (content.childCount > 0)
            {
                WirePrefabButtons(content);
                base.Setup(ctx, content, blocker);
                return;
            }

            UIFactory.CreateLabel(content, "每日好礼", 40, new Vector2(0, 200));
            UIFactory.CreateLabel(content, "观看视频获得 30 分钟无限体力", 26, new Vector2(0, 100));

            UIFactory.CreateButton(content, "领取", new Vector2(0, -20), new Vector2(280, 64))
                .onClick.AddListener(() =>
                {
                    AdsService.ShowRewarded(RewardAdPlacement.HeartInfinite, ok =>
                    {
                        if (!ok) return;
                        GameSaveData.SetInfiniteHeart(30 * 60);
                        GameSaveData.DailyHeartShown = true;
                        Close();
                    });
                });

            UIFactory.CreateButton(content, "关闭", new Vector2(0, -120), new Vector2(200, 52))
                .onClick.AddListener(() =>
                {
                    GameSaveData.DailyHeartShown = true;
                    Close();
                });

            base.Setup(ctx, content, blocker);
        }

        void WirePrefabButtons(RectTransform content)
        {
            var share = content.Find("btnShare")?.GetComponent<Button>();
            if (share != null)
            {
                share.onClick.RemoveAllListeners();
                share.onClick.AddListener(() =>
                {
                    AdsService.ShowRewarded(RewardAdPlacement.HeartInfinite, ok =>
                    {
                        if (!ok) return;
                        GameSaveData.SetInfiniteHeart(30 * 60);
                        GameSaveData.DailyHeartShown = true;
                        Close();
                    });
                });
            }

            var no = content.Find("btnNo")?.GetComponent<Button>();
            if (no != null)
            {
                no.onClick.RemoveAllListeners();
                no.onClick.AddListener(() =>
                {
                    GameSaveData.DailyHeartShown = true;
                    Close();
                });
            }
        }
    }
}
