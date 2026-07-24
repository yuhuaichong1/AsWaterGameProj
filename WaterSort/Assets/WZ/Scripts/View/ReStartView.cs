
using UnityEngine;
using UnityEngine.UI;
using XrCode;

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
                UnBindButtonEvent();

            mExitBtn.onClick.AddListener(OnExitBtnClickHandle);
            ContinueBtn.onClick.AddListener(OnExitBtnClickHandle);
            mReStartBtn.onClick.AddListener(OnReStartBtnClickHandle);
            mIsEventsBound = true;
        }

        private void OnExitBtnClickHandle()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            UIManager.instance.CloseView(EUIType.ReStartView);
        }

        private void OnReStartBtnClickHandle()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            UIManager.instance.CloseView(EUIType.ReStartView);
            // 对齐 UIReStart：关闭后重开本关。
            GameManagerWZ.instance?.RestartLevelTimer();
            FacadeGamePlay.RePlay?.Invoke();
        }

        protected void UnBindButtonEvent()
        {
            mExitBtn.onClick.RemoveAllListeners();
            ContinueBtn.onClick.RemoveAllListeners();
            mReStartBtn.onClick.RemoveAllListeners();
            mIsEventsBound = false;
        }

        private void OnDestroy()
        {
            if (mIsEventsBound)
                UnBindButtonEvent();
        }
    }
}
