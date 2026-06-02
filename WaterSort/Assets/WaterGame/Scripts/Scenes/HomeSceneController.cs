using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Data;
using AsGame.Water;
using AsGame.UI;

namespace AsGame.Scenes
{
    /// <summary>Logic for HomeCanvas prefab. UI hierarchy is authored in the prefab, not built at runtime.</summary>
    public class HomeSceneController : MonoBehaviour
    {
        [SerializeField] Text levelLabel;
        [SerializeField] Button btnStartGame;
        [SerializeField] Button btnSettings;
        [SerializeField] Button btnLeaderboard;
        [SerializeField] Button btnCollection;
        [SerializeField] Button btnFeedback;
        [SerializeField] Transform startButtonPulseTarget;

        void Start()
        {
            WireButtons();
            RefreshLevelLabel();
            AudioManager.Instance?.PlayBgm();
            CheckDailyHeart();
            if (startButtonPulseTarget != null)
                StartCoroutine(PulseStart(startButtonPulseTarget));
        }

        void WireButtons()
        {
            btnStartGame?.onClick.AddListener(() =>
            {
                if (!GameSaveData.UseHeart())
                {
                    PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.RecoverHeart });
                    return;
                }

                SceneFlowManager.LoadScene(SceneId.Game);
            });
            btnSettings?.onClick.AddListener(() =>
                PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.Setting }));
            btnLeaderboard?.onClick.AddListener(() =>
                PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.Rank }));
            btnCollection?.onClick.AddListener(() =>
                PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.Collect }));
            btnFeedback?.onClick.AddListener(() =>
                PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.Feedback }));
        }

        public void RefreshLevelLabel()
        {
            if (levelLabel != null)
                levelLabel.text = "第 " + GameSaveData.CurrentLevel + " 关";
        }

        IEnumerator PulseStart(Transform btn)
        {
            if (btn == null) yield break;
            while (btn != null)
            {
                yield return TweenHelper.ToFloatWhile(btn, 1f, 1.08f, 0.3f, s => btn.localScale = Vector3.one * s);
                if (btn == null) yield break;
                yield return TweenHelper.ToFloatWhile(btn, 1.08f, 1f, 0.3f, s => btn.localScale = Vector3.one * s);
            }
        }

        void CheckDailyHeart()
        {
            if (GameSaveData.DailyHeartShown) return;
            PopupManager.Instance.ShowAtOnce(new PopupContext { Type = PopupType.DailyHeart });
        }
    }
}
