
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{

    public partial class UIProgressPanel : BaseUI
    {
        private PayoutEntryKey entryKey;

        protected override void OnAwake()
        {

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
            mPrevioursStepFinish.text = FacadePayout.GetStepDisplay(entryKey).CurTask;
            RefreshView();
            //ShowAnim(mPayOutPanel);
            PlaneShowAnim();
        }

        protected override void OnDisable()
        {
            FacadeEvent.RemoveEventListener(PayoutEventTypes.ENTRY_UPDATED, OnEntryUpdated);
        }

        private void OnEntryUpdated(EventStruct evt)
        {
            RefreshView();
        }

        private void PlaneShowAnim()
        {
            mAnimPanel.alpha = 1;
            mAnimPanel.transform.position = mAnimPlanePos1.position;
            mBox.transform.position = mBoxPos1.transform.position;
            mLine1.transform.position = mLine1Pos1.transform.position;
            mLine2.transform.position = mLine2Pos1.transform.position;
            mArrow.gameObject.transform.position = mArrowPos1.transform.position;
            mPrevioursStepFinish.transform.position = mTextPos1.position;
            mArrow.alpha = 0;
            mPayOutPanel.transform.position = mPOPPlanePos1.transform.position;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(mBox.DOMove(mBoxPos2.position, 0.4f).SetEase(Ease.Linear));
            sequence.Join(mArrow.DOFade(1, 0.25f));
            sequence.Append(mLine2.DOMove(mLine2Pos2.position, 0.4f).SetEase(Ease.Linear));
            sequence.Append(mPrevioursStepFinish.transform.DOMove(mTextPos2.position, 0.7f).SetEase(Ease.OutBack));
            sequence.Append(mArrow.transform.DOMove(mArrowPos2.position, 1.5f));
            sequence.Append(mAnimPanel.transform.DOMove(mAnimPlanePos2.position, 0.5f));
            sequence.Join(mPayOutPanel.transform.DOMove(mPOPPlanePos2.position, 0.6f).SetEase(Ease.OutBack));
            sequence.Append(mAnimPanel.DOFade(0, 0.25f));


            Sequence sequence2 = DOTween.Sequence();
            sequence2.Append(mLine1.DOMove(mLine1Pos3.position, 0.6f).SetEase(Ease.Linear));
            sequence2.AppendCallback(() => { mLine1.position = mLine1Pos1.position; });
            sequence2.Append(mLine1.DOMove(mLine1Pos3.position, 0.6f).SetEase(Ease.Linear));
            sequence2.AppendInterval(0.25f);
            sequence2.Append(mLine1.DOMove(mLine1Pos2.position, 1.3f).SetEase(Ease.OutBack));
            sequence2.AppendInterval(0.25f);
            sequence2.Append(mLine1.DOMove(mLine1Pos4.position, 0.5f));

        }

        private void RefreshView()
        {
            var display = FacadePayout.GetStepDisplay(entryKey);
            if (mLabTitle != null)
                mLabTitle.text = display.Title;

            mPreviousCondition.text = display.PrevTask;
            mNextCondition.text = display.CurTask;
            mRuleExplain.text = display.Explain;
        }

        private void OnBtnContinueClickHandle()
        {
            FacadePayout.MarkPanelAcknowledged?.Invoke(entryKey);
            UIManager.Instance.CloseUI(EUIType.EUIProgressPanel);
        }

        protected override void OnDispose() { }

    }
}
