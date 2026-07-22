using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace WZSDK
{
    public class LoadingView : BaseView
    {
        public Image progressImage;
        public Text progressText;
        public Text LoadingText;

        private Tween progressTween;
        private Tween backgroundScaleTween;
        private Sequence logoFloatSequence;
        private RectTransform backgroundRect;
        private Vector3 backgroundStartScale;
        private RectTransform logoRect;
        private Vector2 logoStartAnchoredPosition;
        private Vector3 logoStartEulerAngles;
        private Vector3 logoStartScale;


        private Tween loadingTextTween;
        private string[] loadingTexts = { "LOADING", "LOADING.", "LOADING..", "LOADING..." };
        private int loadingTextIndex = 0;

        public override void Start()
        {
            ///CacheBackground();
            CacheLogo();
        }

        public override void Update()
        {
        }

        public override void InitView()
        {
            ResetProgress();
            PlayBackgroundIntro();
            PlayLogoFloat();
            StartLoadingTextAnimation();
        }

        private void StartLoadingTextAnimation()
        {
            StopLoadingTextAnimation();

            loadingTextIndex = 0;
            if (LoadingText != null)
            {
                LoadingText.text = loadingTexts[0];
                loadingTextTween = DOTween.To(() => loadingTextIndex, x => loadingTextIndex = x, loadingTexts.Length - 1, 1.5f)
                    .SetEase(Ease.Linear)
                    .OnUpdate(() =>
                    {
                        if (LoadingText != null)
                        {
                            LoadingText.text = loadingTexts[loadingTextIndex];
                        }
                    })
                    .OnComplete(() =>
                    {
                        loadingTextIndex = 0;
                        if (LoadingText != null) LoadingText.text = loadingTexts[0];
                        StartLoadingTextAnimation();
                    });
            }
        }

        private void StopLoadingTextAnimation()
        {
            if (loadingTextTween != null)
            {
                loadingTextTween.Kill();
                loadingTextTween = null;
            }
        }

        public void StartFill(float duration, System.Action onComplete = null)
        {
            StopFill();

            if (progressImage != null)
            {
                progressImage.fillAmount = 0;

                progressTween = progressImage.DOFillAmount(1f, duration)
                    .SetEase(Ease.Linear)
                    .OnUpdate(() => UpdateProgressText())
                    .OnComplete(() => onComplete?.Invoke());
            }
        }

        public void StartSimulatedProgress(float duration = 8f, float targetProgress = 0.92f)
        {
            StopFill();

            if (progressImage == null)
                return;

            progressImage.fillAmount = 0f;
            UpdateProgressText();
            progressTween = progressImage.DOFillAmount(Mathf.Clamp01(targetProgress), Mathf.Max(0.1f, duration))
                .SetEase(Ease.Linear)
                .OnUpdate(UpdateProgressText);
        }

        public void CompleteProgress(System.Action onComplete = null, float duration = 0.25f)
        {
            StopFill();

            if (progressImage == null)
            {
                onComplete?.Invoke();
                return;
            }

            float remaining = Mathf.Clamp01(1f - progressImage.fillAmount);
            float tweenDuration = remaining <= 0f ? 0f : Mathf.Max(0.05f, duration * remaining);
            progressTween = progressImage.DOFillAmount(1f, tweenDuration)
                .SetEase(Ease.OutCubic)
                .OnUpdate(UpdateProgressText)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void ResetProgress()
        {
            StopFill();

            if (progressImage != null)
            {
                progressImage.fillAmount = 0;
                UpdateProgressText();
            }
        }

        public void SetProgress(float value)
        {
            if (progressImage != null)
            {
                progressImage.fillAmount = Mathf.Clamp01(value);
                UpdateProgressText();
            }
        }

        private void UpdateProgressText()
        {
            if (progressText != null && progressImage != null)
            {
                progressText.text = Mathf.RoundToInt(progressImage.fillAmount * 100) + "%";
            }
        }

        private void StopFill()
        {
            if (progressTween != null)
            {
                progressTween.Kill();
                progressTween = null;
            }
        }

        private void CacheLogo()
        {
            if (logoRect != null)
            {
                return;
            }

            var logoTransform = transform.Find("Top/logo");
            if (logoTransform == null)
            {
                return;
            }

            logoRect = logoTransform as RectTransform;
            if (logoRect != null)
            {
                logoStartAnchoredPosition = logoRect.anchoredPosition;
                logoStartEulerAngles = logoRect.localEulerAngles;
                logoStartScale = logoRect.localScale;
            }
        }

        private void CacheBackground()
        {
            if (backgroundRect != null)
            {
                return;
            }

            var backgroundTransform = transform.Find("bg");
            if (backgroundTransform == null)
            {
                return;
            }

            backgroundRect = backgroundTransform as RectTransform;
            if (backgroundRect != null)
            {
                backgroundStartScale = backgroundRect.localScale;
            }
        }

        private void PlayBackgroundIntro()
        {
            //CacheBackground();
            if (backgroundRect == null)
            {
                return;
            }

            StopBackgroundIntro();
            backgroundRect.localScale = backgroundStartScale * 1.5f;
            backgroundScaleTween = backgroundRect
                .DOScale(backgroundStartScale, 1.2f)
                .SetEase(Ease.OutCubic);
        }

        private void PlayLogoFloat()
        {
            CacheLogo();
            if (logoRect == null)
            {
                return;
            }

            StopLogoFloat();
            logoRect.anchoredPosition = logoStartAnchoredPosition;
            logoRect.localEulerAngles = logoStartEulerAngles;
            logoRect.localScale = logoStartScale;

            logoFloatSequence = DOTween.Sequence();
            logoFloatSequence.Append(logoRect.DOScale(logoStartScale * 1.12f, 1.2f).SetEase(Ease.OutBack));
            logoFloatSequence.Append(logoRect.DOScale(logoStartScale * 0.92f, 1.2f).SetEase(Ease.InOutBack));
            logoFloatSequence.SetLoops(-1, LoopType.Yoyo);
        }

        private void StopLogoFloat()
        {
            if (logoFloatSequence != null)
            {
                logoFloatSequence.Kill();
                logoFloatSequence = null;
            }

            if (logoRect != null)
            {
                logoRect.anchoredPosition = logoStartAnchoredPosition;
                logoRect.localEulerAngles = logoStartEulerAngles;
                logoRect.localScale = logoStartScale;
            }
        }

        private void StopBackgroundIntro()
        {
            if (backgroundScaleTween != null)
            {
                backgroundScaleTween.Kill();
                backgroundScaleTween = null;
            }

            if (backgroundRect != null)
            {
                backgroundRect.localScale = backgroundStartScale;
            }
        }

        public void Close()
        {
            StopFill();
            StopBackgroundIntro();
            StopLogoFloat();
            StopLoadingTextAnimation();
            HideView();
        }

        private void OnDestroy()
        {
            StopFill();
            StopBackgroundIntro();
            StopLogoFloat();
            StopLoadingTextAnimation();
        }
    }
}
