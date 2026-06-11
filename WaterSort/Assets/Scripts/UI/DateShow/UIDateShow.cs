using System;
using UnityEngine;
using UnityEngine.UI;

namespace XrCode
{
    public partial class UIDateShow : BaseUI
    {
        private Vector3 titlePos;
        private Vector3 contentPos;

        protected override void OnAwake()
        {
            titlePos = mTitle.transform.position;
            contentPos = mContentText.transform.position;
        }
        protected override void OnEnable()
        {
            ShowAnim(mPlane);

            mContentText.text = string.Format(FacadeLanguage.GetText("10125"), GameDefines.DataShowText1, GameDefines.DataShowText2, FacadeWithdraw.GetWTarget());

            STimerManager.Instance.CreateSDelay(2, OnContinueBtnClickHandle);
        }
        	    private void OnContinueBtnClickHandle()
        {
            HideAnim(mPlane);
        }

        protected override void OnDisable() { }
        protected override void OnDispose() { }
    }
}