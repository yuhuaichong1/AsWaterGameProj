
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
            RefreshView();
            ShowAnim(mPayOutPanel);
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
        }

        private void OnBtnContinueClickHandle()
        {
            FacadePayout.MarkPanelAcknowledged?.Invoke(entryKey);
            UIManager.Instance.CloseUI(EUIType.EUIProgressPanel);
        }

        protected override void OnDispose() { }

    }
}
