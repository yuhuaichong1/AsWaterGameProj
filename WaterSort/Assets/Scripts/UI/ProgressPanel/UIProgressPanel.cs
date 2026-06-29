

using System;

using DG.Tweening;

using UnityEngine;

using UnityEngine.UI;



namespace XrCode

{



    public partial class UIProgressPanel : BaseUI

    {

        private const string FinishBannerLangId = "10236";

        private const string CheckIconPath = "UI/WithdrawProgress/icon_check2.png";

        private const string WaitingIconPath = "UI/WithdrawProgress/icon_waiting.png";

        private const float PayOutPanelShowY = 0f;

        private const float BannerFadeSeconds = 1f;

        private const float BannerTopMargin = 40f;



        private PayoutEntryKey entryKey;

        private Sprite checkSprite;

        private Sprite waitingSprite;

        private float payOutPanelHiddenY;

        private Vector2 animPanelShowPos;

        private CanvasGroup bannerCanvasGroup;

        private Tween waitingRotateTween;

        private Tween payOutPanelSlideTween;

        private Tween bannerTween;

        private Tween bannerFadeTween;



        protected override void OnAwake()

        {

            if (mPayOutPanel != null)

                payOutPanelHiddenY = mPayOutPanel.anchoredPosition.y;



            if (mAnimPanel != null)

            {

                animPanelShowPos = mAnimPanel.anchoredPosition;

                bannerCanvasGroup = mAnimPanel.GetComponent<CanvasGroup>();

                if (bannerCanvasGroup == null)

                    bannerCanvasGroup = mAnimPanel.gameObject.AddComponent<CanvasGroup>();

            }



            checkSprite = ResourceMod.Instance.SyncLoad<Sprite>(CheckIconPath);

            waitingSprite = ResourceMod.Instance.SyncLoad<Sprite>(WaitingIconPath);



            if (checkSprite == null && mPreviousState != null)

                checkSprite = mPreviousState.sprite;

            if (waitingSprite == null && mNextState != null)

                waitingSprite = mNextState.sprite;

        }



        protected override void OnSetParam(params object[] args)

        {

            if (args.Length > 0)

                entryKey = (PayoutEntryKey)args[0];

        }



        protected override void OnEnable()

        {

            FacadeEvent.AddEventListener(PayoutEventTypes.ENTRY_UPDATED, OnEntryUpdated);

            if (mLabContinue != null)

            {

                mLabContinue.languageId = "10235";

                mLabContinue.UpdateLanguage();

            }

            RefreshView();

            PlayOpenAnim();

        }



        protected override void OnDisable()

        {

            FacadeEvent.RemoveEventListener(PayoutEventTypes.ENTRY_UPDATED, OnEntryUpdated);

            StopAllPanelTweens();

            ResetPanelTransforms();

        }



        private void OnEntryUpdated(EventStruct evt)

        {

            RefreshView();

        }



        private void PlayOpenAnim()

        {

            PreparePayOutPanelHidden();



            if (mAnimPanel != null)

            {

                PrepareBannerVisible();

                PlayBannerPopIn(PlayPanelSlideWithBannerPush);

            }

            else

            {

                PlayPanelSlideWithBannerPush();

            }

        }



        private void PrepareBannerVisible()

        {

            mAnimPanel.gameObject.SetActive(true);

            mAnimPanel.SetAsLastSibling();

            mAnimPanel.anchoredPosition = animPanelShowPos;

            mAnimPanel.localScale = Vector3.zero;



            if (bannerCanvasGroup != null)

                bannerCanvasGroup.alpha = 1f;



            if (mLine1 != null)

                mLine1.gameObject.SetActive(true);

            if (mLine2 != null)

                mLine2.gameObject.SetActive(true);

            if (mArrow != null)

                mArrow.gameObject.SetActive(true);

        }



        private void PlayBannerPopIn(Action onComplete)

        {

            UIManager.Instance.SetGraphicRaycaster(false);

            bannerTween?.Kill();

            mAnimPanel.localScale = Vector3.zero;



            float duration = GameDefines.ShowAnimTime * 1.4f;

            bannerTween = mAnimPanel

                .DOScale(Vector3.one, duration)

                .SetEase(Ease.OutBack)

                .OnComplete(() => onComplete?.Invoke());

        }



        private void PreparePayOutPanelHidden()

        {

            if (mPayOutPanel == null) return;



            payOutPanelSlideTween?.Kill();

            mPayOutPanel.localScale = Vector3.one;

            var pos = mPayOutPanel.anchoredPosition;

            mPayOutPanel.anchoredPosition = new Vector2(pos.x, payOutPanelHiddenY);

        }



        private float CalcBannerEndY(float panelShowY)

        {

            var panelParent = mPayOutPanel.parent as RectTransform;

            if (panelParent == null)

                return animPanelShowPos.y + (panelShowY - payOutPanelHiddenY);



            float parentHalfH = panelParent.rect.height * 0.5f;

            float panelTopY = -parentHalfH + panelShowY + mPayOutPanel.rect.height;

            float bannerHalfH = mAnimPanel.rect.height * 0.5f;

            return panelTopY + bannerHalfH + BannerTopMargin;

        }



        private void PlayPanelSlideWithBannerPush()

        {

            if (mPayOutPanel == null)

            {

                UIManager.Instance.SetGraphicRaycaster(true);

                return;

            }



            UIManager.Instance.SetGraphicRaycaster(false);

            payOutPanelSlideTween?.Kill();



            float fromPanelY = payOutPanelHiddenY;

            float toPanelY = PayOutPanelShowY;

            float fromBannerY = animPanelShowPos.y;

            float toBannerY = CalcBannerEndY(toPanelY);

            float panelX = mPayOutPanel.anchoredPosition.x;

            float bannerX = animPanelShowPos.x;

            float duration = GameDefines.ShowAnimTime;



            payOutPanelSlideTween = DOTween.To(

                    () => fromPanelY,

                    panelY =>

                    {

                        mPayOutPanel.anchoredPosition = new Vector2(panelX, panelY);



                        if (mAnimPanel == null) return;



                        float t = Mathf.Approximately(toPanelY, fromPanelY)

                            ? 1f

                            : (panelY - fromPanelY) / (toPanelY - fromPanelY);

                        float bannerY = Mathf.Lerp(fromBannerY, toBannerY, t);

                        mAnimPanel.anchoredPosition = new Vector2(bannerX, bannerY);

                    },

                    toPanelY,

                    duration)

                .SetEase(Ease.OutBack)

                .OnComplete(FadeOutBanner);

        }



        private void FadeOutBanner()

        {

            if (mAnimPanel == null || bannerCanvasGroup == null)

            {

                UIManager.Instance.SetGraphicRaycaster(true);

                return;

            }



            bannerFadeTween?.Kill();

            bannerFadeTween = bannerCanvasGroup

                .DOFade(0f, BannerFadeSeconds)

                .OnComplete(() =>

                {

                    mAnimPanel.gameObject.SetActive(false);

                    bannerCanvasGroup.alpha = 1f;

                    UIManager.Instance.SetGraphicRaycaster(true);

                });

        }



        private void RefreshView()

        {

            var display = FacadePayout.GetStepDisplay(entryKey);

            var entry = FacadePayout.GetEntry?.Invoke(entryKey);

            bool showFinishText = entry != null && entry.curStepSn > 1;



            if (mPrevioursStepFinish != null)

            {

                mPrevioursStepFinish.gameObject.SetActive(showFinishText);

                if (showFinishText)

                {

                    mPrevioursStepFinish.languageId = FinishBannerLangId;

                    mPrevioursStepFinish.UpdateLanguage();

                }

            }



            if (mLabTitle != null)

            {

                mLabTitle.languageId = string.Empty;

                mLabTitle.text = display.Title ?? string.Empty;

            }



            if (mPreviousCondition != null)

            {

                mPreviousCondition.languageId = string.Empty;

                mPreviousCondition.text = display.PrevTask ?? string.Empty;

            }



            if (mNextCondition != null)

            {

                mNextCondition.languageId = string.Empty;

                mNextCondition.text = display.CurTask ?? string.Empty;

            }



            if (mRuleExplain != null)

            {

                mRuleExplain.languageId = string.Empty;

                mRuleExplain.text = display.Explain ?? string.Empty;

            }



            if (mLineConnect != null)

                mLineConnect.gameObject.SetActive(true);



            ApplyStepState(mPreviousState, display.PrevStepDone, false);

            ApplyStepState(mNextState, display.CurStepDone, true);

        }



        private void ApplyStepState(Image stateImage, bool done, bool allowRotate)

        {

            if (stateImage == null) return;



            if (done)

            {

                if (allowRotate)

                    StopWaitingRotate();

                if (checkSprite != null)

                    stateImage.sprite = checkSprite;

                stateImage.transform.localRotation = Quaternion.identity;

            }

            else

            {

                if (waitingSprite != null)

                    stateImage.sprite = waitingSprite;

                if (allowRotate)

                {

                    if (waitingRotateTween == null || !waitingRotateTween.IsActive())

                        StartWaitingRotate(stateImage.transform);

                }

                else

                {

                    stateImage.transform.localRotation = Quaternion.identity;

                }

            }

        }



        private void StartWaitingRotate(Transform target)

        {

            StopWaitingRotate();

            waitingRotateTween = target

                .DORotate(new Vector3(0f, 0f, -360f), 2f, RotateMode.FastBeyond360)

                .SetEase(Ease.Linear)

                .SetLoops(-1, LoopType.Restart);

        }



        private void StopWaitingRotate()

        {

            waitingRotateTween?.Kill();

            waitingRotateTween = null;

        }



        private void StopAllPanelTweens()

        {

            StopWaitingRotate();

            bannerTween?.Kill();

            bannerTween = null;

            bannerFadeTween?.Kill();

            bannerFadeTween = null;

            payOutPanelSlideTween?.Kill();

            payOutPanelSlideTween = null;

            mPayOutPanel?.DOKill();

            mAnimPanel?.DOKill();

            bannerCanvasGroup?.DOKill();

        }



        private void ResetPanelTransforms()

        {

            if (mPayOutPanel != null)

            {

                mPayOutPanel.localScale = Vector3.one;

                var pos = mPayOutPanel.anchoredPosition;

                mPayOutPanel.anchoredPosition = new Vector2(pos.x, payOutPanelHiddenY);

            }



            if (mAnimPanel != null)

            {

                mAnimPanel.localScale = Vector3.one;

                mAnimPanel.anchoredPosition = animPanelShowPos;

            }



            if (bannerCanvasGroup != null)

                bannerCanvasGroup.alpha = 1f;

        }



        private void OnBtnContinueClickHandle()

        {

            FacadePayout.MarkPanelAcknowledged?.Invoke(entryKey);

            UIManager.Instance.CloseUI(EUIType.EUIProgressPanel);

        }



        protected override void OnDispose()

        {

            StopAllPanelTweens();

        }



    }

}

