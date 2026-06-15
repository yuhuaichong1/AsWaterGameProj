using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIDateShow : BaseUI
    {
        private STimer STimer;

        protected override void OnAwake()
        {

        }
        protected override void OnEnable()
        {
            ShowAnim(mPlane);

            mContentText.text = string.Format(FacadeLanguage.GetText("10125"), GameDefines.DataShowText1, GameDefines.DataShowText2, FacadeWithdraw.GetWTarget());
            mTimeText.text = string.Format(FacadeLanguage.GetText("10126"),15);

            STimer = STimerManager.Instance.CreateSDelay(2, OnContinueBtnClickHandle);
        }
        	    private void OnContinueBtnClickHandle()
        {
            if(STimer != null)
            {
                STimer.Stop();
            }

            HideAnim(mPlane, () => 
            {
                UIManager.Instance.CloseUI(EUIType.EUIDateShow);
                FacadeGuide.SetIfTutorial(false);
                FacadeGamePlay.StartLevel();
            });
        }

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}