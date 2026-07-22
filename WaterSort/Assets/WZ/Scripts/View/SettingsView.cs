using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
            if (GameManagerWZ.instance != null)
            {
                mUserNameText.text = $"{GameManagerWZ.instance.userName}";
                mUserIDText.text = $"ID:{GameManagerWZ.instance.userID.Replace("-", "").Substring(0, 16)}";
                mUserLv.text = $"LV.{GameManagerWZ.instance.userLevel + 1}";

               // mWRBtn.gameObject.SetActive(!GameDefines.ifIAA && GameManagerWZ.instance.currentLv > 1);
                mWRBtn.gameObject.SetActive(false);
            }
        }

        private void InitializeToggleStates()
        {
            if (SoundManager.Instance != null)
            {
                if (mM_Toggle != null)
                {
                    bool musicIsOn = SoundManager.Instance.musicState == 1;
                    mM_Toggle.SetIsOnWithoutNotify(musicIsOn);
                }

                if (mS_Toggle != null)
                {
                    bool soundIsOn = HasDedicatedMusicToggle()
                        ? SoundManager.Instance.soundState == 1
                        : SoundManager.Instance.musicState == 1;

                    mS_Toggle.SetIsOnWithoutNotify(soundIsOn);
                    UpdateSoundToggleVisual(soundIsOn);
                }

                if (mV_Toggle != null)
                {
                    bool vibrationIsOn = SoundManager.Instance.hapticState == 1;
                    mV_Toggle.SetIsOnWithoutNotify(vibrationIsOn);
                    UpdateVibrationToggleVisual(vibrationIsOn);
                }
            }
        }

        protected void BindButtonEvent()
        {
            if (mIsEventsBound)
            {
                RemoveButtonEvents();
            }
            mExitBtn.onClick.AddListener(OnExitBtnClickHandle);
            mUserLevelBtn.onClick.AddListener(OnUserLevelBtnClickHandle);
            mWRBtn.onClick.AddListener(OnWRButtonBtnClickHandle);
            mM_Toggle.onValueChanged.AddListener(OnM_ToggleValueChange);
            mS_Toggle.onValueChanged.AddListener(OnS_ToggleValueChange);
            mV_Toggle.onValueChanged.AddListener(OnV_ToggleValueChange);
            mIsEventsBound = true;
        }

        private void RemoveButtonEvents()
        {
            mExitBtn.onClick.RemoveAllListeners();
            mUserLevelBtn.onClick.RemoveAllListeners();
            mWRBtn.onClick.RemoveAllListeners();
            mM_Toggle.onValueChanged.RemoveAllListeners();
            mS_Toggle.onValueChanged.RemoveAllListeners();
            mV_Toggle.onValueChanged.RemoveAllListeners();
        }

        private void OnWRButtonBtnClickHandle()
        {
            int lv = Gm.currentLv;
            double coin = Gm.GetCoin();
            int stage = Gm.currentStage;

            DataModel dataModel = new DataModel();
            var playerName = Gm.getPlayerName();
            var phone = Gm.getPlayerPhone();
            var email = Gm.getPlayerEmail();

            dataModel.Name = playerName;
            dataModel.Phone = phone;
            dataModel.Email = email;
            dataModel.Level = lv - 1;
            dataModel.Money = coin;
            dataModel.CloseType = 1;

            var historyList = SPlayerPrefs.GetListTem<HistoryModel>(FacadePlayerPrefExtend.History);

            if (historyList == null || historyList.Count == 0)
            {
                return;
            }
            GameApp.viewManager.Open(ViewType.WithDrawHistory, dataModel);
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
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToogleMusic(b);
            }
        }

        private void OnS_ToggleValueChange(bool b)
        {
            if (SoundManager.Instance != null)
            {
                if (HasDedicatedMusicToggle())
                {
                    SoundManager.Instance.ToogleSound(b);
                }
                else
                {
                    SoundManager.Instance.ToogleAllAudio(b);
                }
            }

            UpdateSoundToggleVisual(b);
        }

        private void OnV_ToggleValueChange(bool b)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.ToogleHaptic(b);
            }

            UpdateVibrationToggleVisual(b);
        }

        private bool HasDedicatedMusicToggle()
        {
            return mM_Toggle != null && mM_Toggle.gameObject.activeInHierarchy;
        }

        private void UpdateSoundToggleVisual(bool isOn)
        {
            SoundBackground.gameObject.SetActive(!isOn);
            mS_Icon.localPosition = new Vector3(isOn ? 88 : -88, 36, 0);
        }

        private void UpdateVibrationToggleVisual(bool isOn)
        {
            VibrationBackground.gameObject.SetActive(!isOn);
            mV_Icon.localPosition = new Vector3(88 * (isOn ? 1 : -1), 36, 0);
        }

        private void OnDestroy()
        {
            if (mIsEventsBound)
            {
                RemoveButtonEvents();
            }
        }

        public override void Start()
        {

        }
    }
}
