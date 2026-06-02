using UnityEngine;
using UnityEngine.UI;
using AsGame.Ads;
using AsGame.Core;
using AsGame.Data;
using AsGame.Events;
using AsGame.UI;

namespace AsGame.Water
{
    public class PropButtonView : MonoBehaviour
    {
        [SerializeField] GameProp prop;
        [SerializeField] Text countLabel;
        [SerializeField] GameObject addBadge;

        bool _gray;
        bool _wired;

        void Awake() => EnsureWired();

        void EnsureWired()
        {
            if (_wired) return;
            countLabel ??= transform.Find("Count")?.GetComponent<Text>();
            addBadge ??= transform.Find("Add")?.gameObject;
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnClick);
                btn.onClick.AddListener(OnClick);
            }

            _wired = true;
            Refresh();
        }

        public void Refresh()
        {
            if (countLabel == null || addBadge == null) return;
            var n = GameSaveData.GetPropCount(prop);
            countLabel.text = n > 0 ? n.ToString() : "";
            addBadge.SetActive(n <= 0);
        }

        public void SetGray(bool gray)
        {
            _gray = gray;
            var img = GetComponent<Image>();
            if (img != null)
                img.color = gray ? new Color(0.65f, 0.65f, 0.65f, 0.75f) : Color.white;
        }

        void OnClick()
        {
            if (_gray) return;
            var n = GameSaveData.GetPropCount(prop);
            if (n > 0)
            {
                EventBus.Publish(GameEvents.UseProp, prop);
                return;
            }

            var placement = prop switch
            {
                GameProp.Shuffle => RewardAdPlacement.GameShuffle,
                GameProp.Undo => RewardAdPlacement.GameUndo,
                _ => RewardAdPlacement.GameAddBottle
            };

            AdsService.ShowRewarded(placement, ok =>
            {
                if (!ok) return;
                GameSaveData.SetPropCount(prop, GameSaveData.GetPropCount(prop) + 1);
                EventBus.Publish(GameEvents.UpdateProp);
                Refresh();
            });
        }
    }
}
