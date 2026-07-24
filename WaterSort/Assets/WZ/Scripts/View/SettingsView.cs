using UnityEngine;
using UnityEngine.UI;
using XrCode;

namespace WZSDK
{
    public class SettingsView : BaseView
    {
        protected RectTransform mPlane;
        protected Button mExitBtn;
        protected Image mPlayerIcon;
        protected Text mUserNameText;
        protected Text mUserIDText;
        protected Text mUserLv;
        protected Button mUserLevelBtn;
        protected Toggle mM_Toggle;
        protected Text mMON;
        protected Text mMOFF;
        protected Toggle mS_Toggle;
        protected RectTransform SoundBackground;
        protected RectTransform VibrationBackground;
        protected Toggle mV_Toggle;
        protected Button mWRBtn;
        protected RectTransform mS_Icon;
        protected RectTransform mV_Icon;

        private bool mIsEventsBound = false;
        GameManagerWZ Gm => GameManagerWZ.instance;

        public void Awake()
        {
            mPlane = transform.Find("Plane").GetComponent<RectTransform>();
            mExitBtn = transform.Find("Plane/ExitBtn").GetComponent<Button>();
            mPlayerIcon = transform.Find("Plane/Bg2/PlayerIcon").GetComponent<Image>();
            mUserNameText = transform.Find("Plane/Bg2/UserNameText").GetComponent<Text>();
            mUserIDText = transform.Find("Plane/Bg2/UserIDText").GetComponent<Text>();
            mUserLv = transform.Find("Plane/Bg2/UserLv").GetComponent<Text>();
            mUserLevelBtn = transform.Find("Plane/Bg2/UserLevelBtn").GetComponent<Button>();
            mM_Toggle = transform.Find("Plane/Muslc/m_Toggle").GetComponent<Toggle>();
            mMON = transform.Find("Plane/Muslc/m_Toggle/mON").GetComponent<Text>();
            mMOFF = transform.Find("Plane/Muslc/m_Toggle/mOFF").GetComponent<Text>();
            mS_Toggle = transform.Find("Plane/Sound/s_Toggle").GetComponent<Toggle>();
            SoundBackground = transform.Find("Plane/Sound/s_Toggle/Background").GetComponent<RectTransform>();
            mV_Toggle = transform.Find("Plane/Vibration/v_Toggle").GetComponent<Toggle>();
            VibrationBackground = transform.Find("Plane/Vibration/v_Toggle/Background").GetComponent<RectTransform>();
            mWRBtn = transform.Find("Plane/WRBtn").GetComponent<Button>();
            mS_Icon = transform.Find("Plane/Sound/s_Toggle/s_Icon").GetComponent<RectTransform>();
            mV_Icon = transform.Find("Plane/Vibration/v_Toggle/v_Icon").GetComponent<RectTransform>();
        }

        public override void InitView()
        {
            RefreshUserData();
            InitializeToggleStates();
            BindButtonEvent();
        }

        public override void Update()
        {
        }

        private void RefreshUserData()
        {
            string userName = FacadePlayer.GetPlayerName?.Invoke();
            if (string.IsNullOrEmpty(userName))
                userName = Gm != null ? Gm.userName : string.Empty;
            mUserNameText.text = userName ?? string.Empty;

            string userId = FacadePlayer.GetPlayerID?.Invoke();
            if (string.IsNullOrEmpty(userId) && Gm != null)
                userId = Gm.userID;
            userId = userId ?? string.Empty;
            string idPreview = userId.Length > 13 ? userId.Substring(0, 13) + "..." : userId;
            string idFmt = FacadeLanguage.GetText?.Invoke("10040");
            mUserIDText.text = !string.IsNullOrEmpty(idFmt) && idFmt != "10040"
                ? string.Format(idFmt, idPreview)
                : $"ID:{idPreview}";

            int userLevel = FacadePlayer.GetPlayerLevel?.Invoke()
                ?? (Gm != null ? Gm.userLevel : 0);
            string lvFmt = FacadeLanguage.GetText?.Invoke("10041");
            mUserLv.text = !string.IsNullOrEmpty(lvFmt) && lvFmt != "10041"
                ? string.Format(lvFmt, userLevel + 1)
                : $"LV.{userLevel + 1}";

            // 不展示 Withdrawal History。
            if (mWRBtn != null)
                mWRBtn.gameObject.SetActive(false);
        }

        private void InitializeToggleStates()
        {
            // 对齐 UISetting：开关状态以宿主 FacadeAudio 为准（局内音效/BGM/震动都走它）。
            bool soundOn = FacadeAudio.GetEffectsVolume?.Invoke() == 1f
                || FacadeAudio.GetMusicVolume?.Invoke() == 1f;
            bool vibrateOn = FacadeAudio.GetVibrate?.Invoke() == true;

            if (mS_Toggle != null)
            {
                mS_Toggle.SetIsOnWithoutNotify(soundOn);
                UpdateSoundToggleVisual(soundOn);
            }

            if (mV_Toggle != null)
            {
                mV_Toggle.SetIsOnWithoutNotify(vibrateOn);
                UpdateVibrationToggleVisual(vibrateOn);
            }

            // Muslc 预制体默认隐藏；若启用则同步 BGM。
            if (mM_Toggle != null && mM_Toggle.gameObject.activeInHierarchy)
            {
                bool musicOn = FacadeAudio.GetMusicVolume?.Invoke() == 1f;
                mM_Toggle.SetIsOnWithoutNotify(musicOn);
            }
        }

        protected void BindButtonEvent()
        {
            if (mIsEventsBound)
                RemoveButtonEvents();

            mExitBtn.onClick.AddListener(OnExitBtnClickHandle);
            mUserLevelBtn.onClick.AddListener(OnUserLevelBtnClickHandle);
            if (mWRBtn != null)
                mWRBtn.onClick.AddListener(OnWRButtonBtnClickHandle);
            if (mM_Toggle != null)
                mM_Toggle.onValueChanged.AddListener(OnM_ToggleValueChange);
            mS_Toggle.onValueChanged.AddListener(OnS_ToggleValueChange);
            mV_Toggle.onValueChanged.AddListener(OnV_ToggleValueChange);
            mIsEventsBound = true;
        }

        private void RemoveButtonEvents()
        {
            mExitBtn.onClick.RemoveAllListeners();
            mUserLevelBtn.onClick.RemoveAllListeners();
            if (mWRBtn != null)
                mWRBtn.onClick.RemoveAllListeners();
            if (mM_Toggle != null)
                mM_Toggle.onValueChanged.RemoveAllListeners();
            mS_Toggle.onValueChanged.RemoveAllListeners();
            mV_Toggle.onValueChanged.RemoveAllListeners();
        }

        private void OnWRButtonBtnClickHandle()
        {
            // 预留：当前不开放 Withdrawal History。
            SoundManager.Instance?.PlayUIClickSFX();
        }

        private void OnExitBtnClickHandle()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            UIManager.instance.CloseView(EUIType.SettingsView);
        }

        private void OnUserLevelBtnClickHandle()
        {
            SoundManager.Instance?.PlayUIClickSFX();
            UIManager.instance.CloseView(EUIType.SettingsView);
            UIManager.instance.ShowView(EUIType.UserLevelView);
        }

        private void OnM_ToggleValueChange(bool b)
        {
            FacadeAudio.SetMusicVolume?.Invoke(b ? 1f : 0f);
            SoundManager.Instance?.ToogleMusic(b);
        }

        private void OnS_ToggleValueChange(bool b)
        {
            // 对齐 UISetting：Sound 开关同时控制音效 + 音乐。
            FacadeAudio.SetEffectsVolume?.Invoke(b ? 1f : 0f);
            FacadeAudio.SetMusicVolume?.Invoke(b ? 1f : 0f);
            SoundManager.Instance?.ToogleAllAudio(b);
            UpdateSoundToggleVisual(b);
        }

        private void OnV_ToggleValueChange(bool b)
        {
            FacadeAudio.SetVibrate?.Invoke(b);
            SoundManager.Instance?.ToogleHaptic(b);
            UpdateVibrationToggleVisual(b);
        }

        private void UpdateSoundToggleVisual(bool isOn)
        {
            if (SoundBackground != null)
                SoundBackground.gameObject.SetActive(!isOn);
            if (mS_Icon != null)
                mS_Icon.localPosition = new Vector3(isOn ? 88 : -88, 36, 0);
        }

        private void UpdateVibrationToggleVisual(bool isOn)
        {
            if (VibrationBackground != null)
                VibrationBackground.gameObject.SetActive(!isOn);
            if (mV_Icon != null)
                mV_Icon.localPosition = new Vector3(88 * (isOn ? 1 : -1), 36, 0);
        }

        private void OnDestroy()
        {
            if (mIsEventsBound)
                RemoveButtonEvents();
        }

        public override void Start()
        {
        }
    }
}
