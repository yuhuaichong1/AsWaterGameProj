using UnityEngine;
using UnityEngine.UI;
using AsGame.Ads;
using AsGame.Core;
using AsGame.Data;
using AsGame.Events;
using AsGame.UI;

namespace AsGame.UI.Popups
{
    public class RecoverHeartPopupView : BasePopupView
    {
        RectTransform _content;

        public override void Setup(PopupContext ctx, RectTransform content, CanvasGroup blocker)
        {
            base.Setup(ctx, content, blocker);
            _content = content;
            if (PopupLayoutHelper.HasPrefabLayout(content))
                WirePrefabUi(content);
            else
                BuildFallbackUi(content);
        }

        void WirePrefabUi(RectTransform content)
        {
            ApplyCocosLayout(content);
            RefreshDynamicText();

            PopupLayoutHelper.BindButton(content, "btnClose", Close);
            PopupLayoutHelper.BindButton(content, "center/btnAddFull", OnAddFull);
            PopupLayoutHelper.BindButton(content, "bottom/btnVideo", OnInfiniteVideo);
        }

        static void ApplyCocosLayout(RectTransform content)
        {
            // 预制体已含 Cocos 坐标，仅校正按钮文字区域
            FixButtonLabel(content, "center/btnAddFull");
            FixButtonLabel(content, "bottom/btnVideo");
        }

        static void FixButtonLabel(Transform root, string buttonPath)
        {
            var label = root.Find(buttonPath + "/label") as RectTransform;
            if (label == null) return;
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = label.offsetMax = Vector2.zero;
            var text = label.GetComponent<Text>();
            if (text != null)
                text.alignment = TextAnchor.MiddleCenter;
        }

        void RefreshDynamicText()
        {
            if (_content == null) return;
            PopupLayoutHelper.SetText(_content, "center/heart/num", GameSaveData.Heart.ToString());
            PopupLayoutHelper.SetText(_content, "bottom/btnVideo/num",
                GameSaveData.HeartInfiniteVideoCount + "/3");
        }

        void OnAddFull()
        {
            if (GameSaveData.Heart > GameConstants.MaxHeart)
            {
                ToastService.Show("体力已满!!!");
                return;
            }

            AdsService.ShowRewarded(RewardAdPlacement.HeartFull, ok =>
            {
                if (!ok) return;
                GameSaveData.SetFullHeart();
                EventBus.Publish(GameEvents.UpdateHeart);
                PopupManager.Instance.ShowAtOnce(new PopupContext
                {
                    Type = PopupType.GetHeart,
                    Payload = 1
                });
                Close();
            });
        }

        void OnInfiniteVideo()
        {
            AdsService.ShowRewarded(RewardAdPlacement.HeartInfinite, ok =>
            {
                if (!ok) return;
                GameSaveData.HeartInfiniteVideoCount++;
                if (GameSaveData.HeartInfiniteVideoCount >= 3)
                {
                    GameSaveData.HeartInfiniteVideoCount = 0;
                    var sec = 60 * Random.Range(GameConstants.HeartInfiniteTime[0],
                        GameConstants.HeartInfiniteTime[1] + 1);
                    GameSaveData.SetInfiniteHeart(sec);
                    PopupManager.Instance.ShowAtOnce(new PopupContext
                    {
                        Type = PopupType.GetHeart,
                        Payload = 2
                    });
                    EventBus.Publish(GameEvents.UpdateHeart);
                    Close();
                }
                else
                {
                    RefreshDynamicText();
                }
            });
        }

        void BuildFallbackUi(RectTransform content)
        {
            UIFactory.CreateLabel(content, "恢复体力", 40, new Vector2(0, 280));
            UIFactory.CreateLabel(content, $"当前体力: {GameSaveData.Heart}/{GameConstants.MaxHeart}", 28,
                new Vector2(0, 200));

            UIFactory.CreateButton(content, "看视频回满", new Vector2(0, 60), new Vector2(320, 64))
                .onClick.AddListener(OnAddFull);

            var vidLabel = UIFactory.CreateLabel(content,
                $"无限体力 ({GameSaveData.HeartInfiniteVideoCount}/3 次)", 24, new Vector2(0, -40));

            UIFactory.CreateButton(content, "看视频攒次数", new Vector2(0, -120), new Vector2(320, 64))
                .onClick.AddListener(() =>
                {
                    OnInfiniteVideo();
                    vidLabel.text = $"无限体力 ({GameSaveData.HeartInfiniteVideoCount}/3 次)";
                });

            UIFactory.CreateButton(content, "关闭", new Vector2(0, -260), new Vector2(200, 52))
                .onClick.AddListener(Close);
        }
    }
}
