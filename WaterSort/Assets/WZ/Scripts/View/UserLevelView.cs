using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace WZSDK
{
    public class UserLevelView : BaseView
    {
        protected RectTransform mPlane;
        protected Button mExitBtn;
        protected Text mCurLevelText;
        protected Button mKeepPlayingBtn;
        protected Slider mLevelProgress;
        protected Text mLevelProgressText;

        private bool mIsEventsBound = false;

        private void Awake()
        {
            mPlane = transform.Find("Plane").GetComponent<RectTransform>();
            mExitBtn = transform.Find("Plane/ExitBtn").GetComponent<Button>();
            mCurLevelText = transform.Find("Plane/CurLevelText").GetComponent<Text>();
            mKeepPlayingBtn = transform.Find("Plane/KeepPlayingBtn").GetComponent<Button>();
            mLevelProgress = transform.Find("Plane/LevelProgress").GetComponent<Slider>();
            mLevelProgressText = transform.Find("Plane/LevelProgress/LevelProgressText").GetComponent<Text>();
        }

        public override void InitView()
        {
            BindButtonEvent();
            RefreshLevelData();
        }



        /// <summary>
        /// 刷新等级数据
        /// </summary>
        private void RefreshLevelData()
        {
            if (GameManagerWZ.instance == null)
            {
                Debug.LogWarning("GameManager.instance is null");
                return;
            }
            int curUserLevel = GameManagerWZ.instance.userLevel;

            if (mCurLevelText != null)
            {
                mCurLevelText.text = string.Format(LocalizationManager.Instance.GetText("2128"), curUserLevel + 1);
            }

            int currentExp = GameManagerWZ.instance.userExp;
            int nextLevelExp = GetNextLevelNeedExp(curUserLevel);
            float progress = 0f;

            if (nextLevelExp > 0)
            {
                progress = currentExp * 1f / nextLevelExp;
                progress = Mathf.Clamp01(progress);
            }
            else
            {
                progress = 1.0f;
            }

            if (mLevelProgress != null)
            {
                mLevelProgress.value = progress;
            }

            if (mLevelProgressText != null)
            {
                mLevelProgressText.text = $"{(int)(progress * 100)}%";
            }

            Debug.Log($"刷新等级数据: Lv.{curUserLevel + 1}, Exp:{currentExp}/{nextLevelExp}, 进度:{progress:P0}");
        }

        /// <summary>
        /// 获取下一级所需经验
        /// </summary>
        private int GetNextLevelNeedExp(int currentLevel)
        {
            if (UserDataManager.Instance != null)
            {
                return UserDataManager.Instance.GetNextLevelNeedExp();
            }
            return 100;
        }

        protected void BindButtonEvent()
        {
            if (mIsEventsBound)
            {
                UnBindButtonEvent();
            }

            mExitBtn.onClick.AddListener(OnExitBtnClickHandle);
            mKeepPlayingBtn.onClick.AddListener(OnKeepPlayingBtnClickHandle);

            mIsEventsBound = true;
        }

        protected void UnBindButtonEvent()
        {
            mExitBtn.onClick.RemoveAllListeners();
            mKeepPlayingBtn.onClick.RemoveAllListeners();
        }

        private void OnExitBtnClickHandle()
        {
            UIManager.instance.CloseView(EUIType.UserLevelView);
        }

        private void OnKeepPlayingBtnClickHandle()
        {
            UIManager.instance.CloseView(EUIType.UserLevelView);
        }

        private void OnDestroy()
        {
            if (mIsEventsBound)
            {
                UnBindButtonEvent();
            }
        }

        public override void Start()
        {

        }

        public override void Update()
        {

        }
    }
}