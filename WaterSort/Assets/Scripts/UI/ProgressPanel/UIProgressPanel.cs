
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
            RefreshView();
            ShowAnim(mPlane);
        }

        protected override void OnDisable()
        {
            FacadeEvent.RemoveEventListener(PayoutEventTypes.ENTRY_UPDATED, OnEntryUpdated);
        }

        private void OnEntryUpdated(EventStruct evt)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            var display = FacadePayout.GetStepDisplay(entryKey);
            if (mLabTitle != null)
                mLabTitle.text = display.Title;

            mPreviousCondition.text = display.PrevTask;
            mNextCondition.text = display.CurTask;
            mRuleExplain.text = display.Explain;

            bool canContinue = display.CanContinue && !display.IsTerminal;
            mBtnContinue.interactable = canContinue;
            if (!canContinue && !display.IsTerminal)
                mLabContinue.text = FacadeLanguage.GetText("10234");
            else
                mLabContinue.text = FacadeLanguage.GetText("10235");
        }

        private void OnBtnContinueClickHandle()
        {
            if (!FacadePayout.CanContinue(entryKey)) return;
            FacadePayout.ContinueStep(entryKey);
            RefreshView();
        }

        protected override void OnDispose() { }

    }
}
