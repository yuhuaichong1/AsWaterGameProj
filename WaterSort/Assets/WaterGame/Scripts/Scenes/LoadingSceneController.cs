using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using AsGame.Core;
using AsGame.Data;
using AsGame.UI;
using XrCode;

namespace AsGame.Scenes
{
    /// <summary>Logic for LoadingCanvas prefab. UI hierarchy is authored in the prefab, not built at runtime.</summary>
    public class LoadingSceneController : MonoBehaviour
    {
        const float ProgressWidth = 490f;
        const float ProgressHeight = 36f;

        [SerializeField] RectTransform progressFill;
        [SerializeField] Text percentLabel;

        float _progress;
        bool _configReady;

        void Start() => StartCoroutine(LoadRoutine());

        public void SetProgress(float value)
        {
            _progress = value;
            if (progressFill != null)
                progressFill.sizeDelta = new Vector2(ProgressWidth * value, ProgressHeight);
            if (percentLabel != null)
                percentLabel.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        IEnumerator LoadRoutine()
        {
            yield return null;
            _configReady = LevelConfigLoader.GetLevelCount() > 0;

            var elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                SetProgress(Mathf.Lerp(0f, 0.95f, Mathf.Clamp01(elapsed)));
                yield return null;
            }

            SetProgress(0.95f);
            var stay = 1.5f;
            while (stay > 0f || (!_configReady && _progress < 0.98f))
            {
                stay -= Time.deltaTime;
                if (!_configReady)
                    SetProgress(Mathf.MoveTowards(_progress, 0.98f, Time.deltaTime * 0.05f));
                else
                    SetProgress(_progress);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                SetProgress(Mathf.Lerp(0.95f, 1f, Mathf.Clamp01(elapsed / 0.2f)));
                yield return null;
            }

            SetProgress(1f);
            SceneFlowManager.LoadScene(SceneId.Home);
        }
    }
}
