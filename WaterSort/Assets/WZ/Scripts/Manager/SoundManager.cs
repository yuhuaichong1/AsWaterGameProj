using System;
using UnityEngine;

namespace WZSDK
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance;

        private const int PrefOnValue = 0;
        private const int PrefOffValue = 1;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Legacy Audio Sources")]
        [SerializeField] private AudioSource waterShortFall1;
        [SerializeField] private AudioSource waterShortFall2;
        [SerializeField] private AudioSource waterLongFall1;
        [SerializeField] private AudioSource waterLongFall2;
        [SerializeField] private AudioSource waterFull;
        [SerializeField] private AudioSource bottleSelect;
        [SerializeField] private AudioSource addTube;
        [SerializeField] private AudioSource clickBtn;
        [SerializeField] private AudioSource flyingCoin;
        [SerializeField] private AudioSource cat;
        [SerializeField] private AudioSource gameWin;
        [SerializeField] private AudioSource backgroundMusic;
        [SerializeField] private AudioSource[] soundList;

        [Header("SFX")]
        [SerializeField] private AudioClip uiClickSfxClip;
        [SerializeField] private AudioClip arrowEscapeFailedSfx;
        [SerializeField] private AudioClip arrowEscapeSuccessSfx;
        [SerializeField] private AudioClip levelCompletedSfx;
        [SerializeField] private AudioClip levelFailedSfx;
        [SerializeField] private AudioClip collectCoinSfx;
        [SerializeField] private AudioClip waterFullSfxClip;
        [SerializeField] private AudioClip gameWinSfxClip;

        [Header("Volume")]
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        public int musicState;   // 1=on, 0=off
        public int soundState;   // 1=on, 0=off
        public int hapticState;  // 1=on, 0=off

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ResolveAudioSources();
            RegisterFacadeHandlers();

            musicState = LoadToggleState(FacadePlayerPrefExtend.MusicKey, FacadePlayerPrefExtend.LegacyMusicKey);
            soundState = LoadToggleState(FacadePlayerPrefExtend.SoundKey, FacadePlayerPrefExtend.LegacySoundKey, musicState == 1);
            hapticState = LoadToggleState(FacadePlayerPrefExtend.HapticKey, FacadePlayerPrefExtend.LegacyHapticKey);

            ApplyInitialSettings();
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            UnregisterFacadeHandlers();
            Instance = null;
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicState != 1 || clip == null || musicSource == null)
                return;

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            StopAudioSource(musicSource);

            if (backgroundMusic != musicSource)
                StopAudioSource(backgroundMusic);
        }

        public void PlaySFX(AudioClip clip)
        {
            if (soundState != 1 || clip == null || sfxSource == null)
                return;

            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlayUIClickSFX()
        {
            if (uiClickSfxClip != null)
            {
                PlaySFX(uiClickSfxClip);
                return;
            }

            PlayLegacySFX(clickBtn);
        }

        public void PlayArrowEscapeFailedSFX()
        {
            PlaySFX(arrowEscapeFailedSfx);
            Vibrate();
        }

        public void PlayArrowEscapeSuccessSFX()
        {
            PlaySFX(arrowEscapeSuccessSfx);
        }

        public void PlayLevelCompletedSFX()
        {
            AudioClip clip = levelCompletedSfx != null ? levelCompletedSfx : gameWinSfxClip;

            if (clip != null)
                PlaySFX(clip);
            else
                PlayLegacySFX(gameWin);
        }

        public void PlayCollectCoinSFX()
        {
            if (collectCoinSfx != null)
                PlaySFX(collectCoinSfx);
            else
                PlayLegacySFX(flyingCoin);
        }

        public void PlayLevelFailedSFX()
        {
            if (levelFailedSfx != null)
                PlaySFX(levelFailedSfx);
            else
                PlayLegacySFX(waterFull);

            Vibrate();
        }

        public void PlayWaterFullSFX()
        {
            if (waterFullSfxClip != null)
                PlaySFX(waterFullSfxClip);
            else
                PlayLegacySFX(waterFull);
        }

        public void PlayGameWinSFX()
        {
            AudioClip clip = gameWinSfxClip != null ? gameWinSfxClip : levelCompletedSfx;

            if (clip != null)
                PlaySFX(clip);
            else
                PlayLegacySFX(gameWin);
        }

        public void ToogleMusic(bool toggle)
        {
            musicState = toggle ? 1 : 0;
            SaveToggleState(FacadePlayerPrefExtend.MusicKey, FacadePlayerPrefExtend.LegacyMusicKey, toggle);

            if (toggle)
                PlayConfiguredMusic();
            else
                StopMusic();
        }

        public void ToogleSound(bool toggle)
        {
            soundState = toggle ? 1 : 0;
            SaveToggleState(FacadePlayerPrefExtend.SoundKey, FacadePlayerPrefExtend.LegacySoundKey, toggle);

            ApplySfxVolume();

            if (!toggle)
                StopAllSfxSources();
        }

        public void ToogleAllAudio(bool toggle)
        {
            ToogleMusic(toggle);
            ToogleSound(toggle);
        }

        public void ToogleHaptic(bool toggle)
        {
            hapticState = toggle ? 1 : 0;
            SaveToggleState(FacadePlayerPrefExtend.HapticKey, FacadePlayerPrefExtend.LegacyHapticKey, toggle);
        }

        public void Vibrate()
        {
            if (hapticState != 1)
                return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private void ResolveAudioSources()
        {
            if (musicSource == null)
                musicSource = backgroundMusic;

            if (backgroundMusic == null)
                backgroundMusic = musicSource;

            if (sfxSource == null)
                sfxSource = clickBtn;

            if (soundList == null || soundList.Length == 0)
            {
                soundList = new[]
                {
                    waterShortFall1,
                    waterShortFall2,
                    waterLongFall1,
                    waterLongFall2,
                    waterFull,
                    bottleSelect,
                    addTube,
                    clickBtn,
                    flyingCoin,
                    cat,
                    gameWin
                };
            }
        }

        private void RegisterFacadeHandlers()
        {
            FacadeAudioExtend.PlayBgmHandle += PlayConfiguredMusic;
            FacadeAudioExtend.StopBgmHandle += StopMusic;
            FacadeAudioExtend.PlayEffectHandle += PlayEffect;
            FacadeAudioExtend.PlayVibrateHandle += Vibrate;
            FacadeAudioExtend.SetMusicVolumeHandle += SetMusicVolume;
            FacadeAudioExtend.SetEffectsVolumeHandle += SetEffectsVolume;
            FacadeAudioExtend.SetVibrateHandle += ToogleHaptic;
            FacadeAudioExtend.GetMusicVolumeHandle += GetMusicVolume;
            FacadeAudioExtend.GetEffectsVolumeHandle += GetEffectsVolume;
            FacadeAudioExtend.GetVibrateHandle += GetVibrate;
            FacadeAudioExtend.GetEATypeByStrHandle += GetEATypeByStr;
        }

        private void UnregisterFacadeHandlers()
        {
            FacadeAudioExtend.PlayBgmHandle -= PlayConfiguredMusic;
            FacadeAudioExtend.StopBgmHandle -= StopMusic;
            FacadeAudioExtend.PlayEffectHandle -= PlayEffect;
            FacadeAudioExtend.PlayVibrateHandle -= Vibrate;
            FacadeAudioExtend.SetMusicVolumeHandle -= SetMusicVolume;
            FacadeAudioExtend.SetEffectsVolumeHandle -= SetEffectsVolume;
            FacadeAudioExtend.SetVibrateHandle -= ToogleHaptic;
            FacadeAudioExtend.GetMusicVolumeHandle -= GetMusicVolume;
            FacadeAudioExtend.GetEffectsVolumeHandle -= GetEffectsVolume;
            FacadeAudioExtend.GetVibrateHandle -= GetVibrate;
            FacadeAudioExtend.GetEATypeByStrHandle -= GetEATypeByStr;
        }

        private void PlayConfiguredMusic()
        {
            if (musicState != 1)
                return;

            PlayMusicSource(musicSource);

            if (backgroundMusic != musicSource)
                PlayMusicSource(backgroundMusic);
        }

        private void PlayEffect(AudioStCategory category)
        {
            switch (category)
            {
                case AudioStCategory.EBgm:
                    PlayConfiguredMusic();
                    break;
                case AudioStCategory.EButton:
                case AudioStCategory.EClick2:
                    PlayUIClickSFX();
                    break;
                case AudioStCategory.ESpoolClick:
                    PlayLegacySFX(bottleSelect, uiClickSfxClip);
                    break;
                case AudioStCategory.EUnlock:
                    PlayLegacySFX(addTube, arrowEscapeSuccessSfx);
                    break;
                case AudioStCategory.EChangeTarget:
                    PlayLegacySFX(waterShortFall1, arrowEscapeSuccessSfx);
                    break;
                case AudioStCategory.EChangeTarget2:
                    PlayLegacySFX(waterShortFall2, arrowEscapeSuccessSfx);
                    break;
                case AudioStCategory.ESettlement:
                case AudioStCategory.ELevelComplete:
                case AudioStCategory.EPopRewad:
                    PlayLevelCompletedSFX();
                    break;
                case AudioStCategory.EBoardBreak:
                case AudioStCategory.ELevelFailed:
                    PlayLevelFailedSFX();
                    break;
                case AudioStCategory.EFlyMoneyTip:
                    PlayCollectCoinSFX();
                    break;
                case AudioStCategory.ELuckySpin:
                    PlayLegacySFX(waterLongFall1, arrowEscapeSuccessSfx);
                    break;
                case AudioStCategory.ELuckySpin2:
                    PlayLegacySFX(waterLongFall2, collectCoinSfx);
                    break;
                case AudioStCategory.EMerge_1:
                    PlayLegacySFX(waterShortFall1, arrowEscapeSuccessSfx);
                    break;
                case AudioStCategory.EMerge_2:
                    PlayLegacySFX(waterShortFall2, arrowEscapeSuccessSfx);
                    break;
                case AudioStCategory.EMerge_3:
                    PlayLegacySFX(waterLongFall1, arrowEscapeSuccessSfx);
                    break;
                case AudioStCategory.EMerge_4:
                    PlayLegacySFX(waterLongFall2, arrowEscapeSuccessSfx);
                    break;
                case AudioStCategory.EMerge_5:
                    PlayWaterFullSFX();
                    break;
                case AudioStCategory.ESaoBa:
                    PlayLegacySFX(cat, uiClickSfxClip);
                    break;
            }
        }

        private void PlayMusicSource(AudioSource source)
        {
            if (source == null)
                return;

            source.volume = musicVolume;

            if (!source.isPlaying)
                source.Play();
        }

        private void PlayLegacySFX(AudioSource source, AudioClip fallbackClip = null)
        {
            if (soundState != 1)
                return;

            if (source != null)
            {
                source.volume = sfxVolume;
                source.Play();
                return;
            }

            PlaySFX(fallbackClip);
        }

        private void StopAudioSource(AudioSource source)
        {
            if (source != null)
                source.Stop();
        }

        private void StopAllSfxSources()
        {
            StopAudioSource(sfxSource);

            if (soundList == null)
                return;

            for (int i = 0; i < soundList.Length; i++)
                StopAudioSource(soundList[i]);
        }

        private void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyMusicVolume();
        }

        private void SetEffectsVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplySfxVolume();
        }

        private float GetMusicVolume()
        {
            return musicVolume;
        }

        private float GetEffectsVolume()
        {
            return sfxVolume;
        }

        private bool GetVibrate()
        {
            return hapticState == 1;
        }

        private AudioStCategory GetEATypeByStr(string value)
        {
            if (string.IsNullOrEmpty(value))
                return AudioStCategory.EButton;

            if (Enum.TryParse(value, true, out AudioStCategory category))
                return category;

            return AudioStCategory.EButton;
        }

        private void ApplyInitialSettings()
        {
            ApplyMusicVolume();
            ApplySfxVolume();

            if (musicState == 1)
                PlayConfiguredMusic();
            else
                StopMusic();
        }

        private void ApplyMusicVolume()
        {
            if (musicSource != null)
                musicSource.volume = musicState == 1 ? musicVolume : 0f;

            if (backgroundMusic != null && backgroundMusic != musicSource)
                backgroundMusic.volume = musicState == 1 ? musicVolume : 0f;
        }

        private void ApplySfxVolume()
        {
            float volume = soundState == 1 ? sfxVolume : 0f;

            if (sfxSource != null)
                sfxSource.volume = volume;

            if (soundList == null)
                return;

            for (int i = 0; i < soundList.Length; i++)
            {
                if (soundList[i] != null)
                    soundList[i].volume = volume;
            }
        }

        private int LoadToggleState(string key, string legacyKey, bool defaultOn = true)
        {
            int defaultPrefValue = defaultOn ? PrefOnValue : PrefOffValue;
            int prefValue = defaultPrefValue;

            if (PlayerPrefs.HasKey(key))
            {
                prefValue = PlayerPrefs.GetInt(key, defaultPrefValue);
            }
            else if (!string.IsNullOrEmpty(legacyKey) && legacyKey != key && PlayerPrefs.HasKey(legacyKey))
            {
                prefValue = PlayerPrefs.GetInt(legacyKey, defaultPrefValue);
            }

            bool isOn = prefValue == PrefOnValue;
            SaveToggleState(key, legacyKey, isOn);
            return isOn ? 1 : 0;
        }

        private void SaveToggleState(string key, string legacyKey, bool isOn)
        {
            int prefValue = isOn ? PrefOnValue : PrefOffValue;
            PlayerPrefs.SetInt(key, prefValue);

            if (!string.IsNullOrEmpty(legacyKey) && legacyKey != key)
                PlayerPrefs.SetInt(legacyKey, prefValue);

            PlayerPrefs.Save();
        }
    }
}
