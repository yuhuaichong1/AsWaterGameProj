
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class ReStartView : BaseView
    {
        protected RectTransform mPlane;
        protected Button mExitBtn;
        protected Button mReStartBtn;
        protected Button ContinueBtn;

        private bool mIsEventsBound = false;

        public void Awake()
        {
            mPlane = transform.Find("Plane").GetComponent<RectTransform>();
            mExitBtn = transform.Find("Plane/ExitBtn").GetComponent<Button>();
            ContinueBtn = transform.Find("Plane/ContinueBtn").GetComponent<Button>();
            mReStartBtn = transform.Find("Plane/ReStartBtn").GetComponent<Button>();
        }

        public override void InitView()
        {
            BindButtonEvent();
        }

        public override void Start()
        {

        }

        public override void Update()
        {

        }

        protected void BindButtonEvent()
        {
            if (mIsEventsBound)
            {
                UnBindButtonEvent();
            }

            mExitBtn.onClick.AddListener(OnExitBtnClickHandle);
            ContinueBtn.onClick.AddListener(OnExitBtnClickHandle);
            mReStartBtn.onClick.AddListener(OnReStartBtnClickHandle);
            mIsEventsBound = true;
        }

        private void OnExitBtnClickHandle()
        {
            UIManager.instance.CloseView(EUIType.ReStartView);
            SoundManager.Instance?.PlayUIClickSFX();
        }

        private void OnReStartBtnClickHandle()
        {
           // LevelManager.Instance.LoadCurrentLevel();
            // GameManagerWZ.instance.ReplayGame();
            UIManager.instance.CloseView(EUIType.ReStartView);
            SoundManager.Instance?.PlayUIClickSFX();
        }

        protected void UnBindButtonEvent()
        {
            mExitBtn.onClick.RemoveAllListeners();
            mReStartBtn.onClick.RemoveAllListeners();
            mIsEventsBound = false;
        }

        private void OnDestroy()
        {
            if (mIsEventsBound)
            {
                UnBindButtonEvent();
            }
        }
    }
}
