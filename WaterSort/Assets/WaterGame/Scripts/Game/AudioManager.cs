using UnityEngine;

namespace AsGame.Water
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource _bgm;
        AudioSource _sfx;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.loop = true;
            _sfx = gameObject.AddComponent<AudioSource>();
        }

        public void PlayBgm()
        {
            if (!AsGame.Data.GameSaveData.Bgm) return;
            var clip = Resources.Load<AudioClip>("Audio/Bgm");
            if (clip == null) return;
            _bgm.clip = clip;
            _bgm.Play();
        }

        public void StopBgm() => _bgm.Stop();

        public void PlaySfx(string name, float volume = 1f)
        {
            if (!AsGame.Data.GameSaveData.Sfx) return;
            var clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) return;
            _sfx.PlayOneShot(clip, volume);
        }
    }
}
