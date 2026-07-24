using UnityEngine;
using UnityEngine.UI;
using XrCode;

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
        /// 刷新等级数据（对齐 UIUserLevel：读宿主 FacadePlayer + TBUserLevel）。
        /// </summary>
        private void RefreshLevelData()
        {
            int curUserLevel = FacadePlayer.GetPlayerLevel?.Invoke() ?? 0;
            int currentExp = FacadePlayer.GetPlayerExp?.Invoke() ?? 0;

            if (GameManagerWZ.instance != null)
            {
                GameManagerWZ.instance.userLevel = curUserLevel;
                GameManagerWZ.instance.userExp = currentExp;
            }

            int displayLevel = curUserLevel + 1;
            if (mCurLevelText != null)
            {
                string fmt = FacadeLanguage.GetText?.Invoke("10048");
                if (string.IsNullOrEmpty(fmt) || fmt == "10048")
                    fmt = LocalizationManager.Instance != null
                        ? LocalizationManager.Instance.GetText("2128")
                        : "当前级别：{0}";
                mCurLevelText.text = string.Format(fmt, displayLevel);
            }

            int nextLevelExp = GetNextLevelNeedExp(curUserLevel);
            float progress = nextLevelExp > 0
                ? Mathf.Clamp01(currentExp * 1f / nextLevelExp)
                : 1f;

            if (mLevelProgress != null)
                mLevelProgress.value = progress;

            if (mLevelProgressText != null)
                mLevelProgressText.text = $"{(int)(progress * 100)}%";
        }

        private int GetNextLevelNeedExp(int currentLevel)
        {
            // 对齐 UIUserLevel / PlayerModule：TBUserLevel 以 Sn（0 基等级）取值。
            var tables = ConfigModule.Instance != null ? ConfigModule.Instance.Tables : null;
            if (tables != null && tables.TBUserLevel != null)
            {
                var conf = tables.TBUserLevel.GetOrDefault(currentLevel);
                if (conf != null)
                    return Mathf.Max(conf.NextLvNeedExp, 1);
            }

            if (UserDataManager.Instance != null)
            {
                int need = UserDataManager.Instance.GetNextLevelNeedExp();
                if (need > 0)
                    return need;
            }

            return 100;
        }

        protected void BindButtonEvent()
        {
            if (mIsEventsBound)
                UnBindButtonEvent();

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
            SoundManager.Instance?.PlayUIClickSFX();
            UIManager.instance.CloseView(EUIType.UserLevelView);
        }

        private void OnKeepPlayingBtnClickHandle()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            UIManager.instance.CloseView(EUIType.UserLevelView);
        }

        private void OnDestroy()
        {
            if (mIsEventsBound)
                UnBindButtonEvent();
        }

        public override void Start()
        {
        }

        public override void Update()
        {
        }
    }
}
