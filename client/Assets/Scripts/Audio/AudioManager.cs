using UnityEngine;
using TexasHoldem.Utils;

namespace TexasHoldem.Audio
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.8f;
        [Range(0f, 1f)]
        [SerializeField] private float uiVolume = 0.7f;

        private const string PREF_MASTER_VOLUME = "MasterVolume";
        private const string PREF_MUSIC_VOLUME = "MusicVolume";
        private const string PREF_SFX_VOLUME = "SFXVolume";
        private const string PREF_UI_VOLUME = "UIVolume";
        private const string PREF_MUTE_ALL = "MuteAll";

        private bool _isMuted = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadVolumeSettings();
        }

        private void InitializeAudioSources()
        {
            if (musicSource == null)
            {
                var musicGo = new GameObject("MusicSource");
                musicGo.transform.SetParent(transform);
                musicSource = musicGo.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                var sfxGo = new GameObject("SFXSource");
                sfxGo.transform.SetParent(transform);
                sfxSource = sfxGo.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (uiSource == null)
            {
                var uiGo = new GameObject("UISource");
                uiGo.transform.SetParent(transform);
                uiSource = uiGo.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1f);
            musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 0.5f);
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.8f);
            uiVolume = PlayerPrefs.GetFloat(PREF_UI_VOLUME, 0.7f);
            _isMuted = PlayerPrefs.GetInt(PREF_MUTE_ALL, 0) == 1;

            ApplyVolumeSettings();
        }

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, masterVolume);
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, musicVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolume);
            PlayerPrefs.SetFloat(PREF_UI_VOLUME, uiVolume);
            PlayerPrefs.SetInt(PREF_MUTE_ALL, _isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyVolumeSettings()
        {
            float master = _isMuted ? 0f : masterVolume;

            if (musicSource != null)
                musicSource.volume = master * musicVolume;

            if (sfxSource != null)
                sfxSource.volume = master * sfxVolume;

            if (uiSource != null)
                uiSource.volume = master * uiVolume;
        }

        // Volume control
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        public void ToggleMute()
        {
            _isMuted = !_isMuted;
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        public void SetMute(bool mute)
        {
            _isMuted = mute;
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        // Music playback
        public void PlayMenuMusic()
        {
            var clip = ResourceManager.Instance?.MenuMusic;
            if (clip != null) PlayMusic(clip);
        }

        public void PlayGameMusic()
        {
            var clip = ResourceManager.Instance?.GameMusic;
            if (clip != null) PlayMusic(clip);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null) return;

            if (musicSource.clip != clip)
            {
                musicSource.clip = clip;
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            musicSource?.Stop();
        }

        public void PauseMusic()
        {
            musicSource?.Pause();
        }

        public void ResumeMusic()
        {
            musicSource?.UnPause();
        }

        // SFX playback
        public void PlaySFX(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void PlayUI(AudioClip clip)
        {
            if (uiSource == null || clip == null) return;
            uiSource.PlayOneShot(clip);
        }

        // Convenience methods
        public void PlayCardDeal()
        {
            PlaySFX(ResourceManager.Instance?.CardDealSound);
        }

        public void PlayCardFlip()
        {
            PlaySFX(ResourceManager.Instance?.CardFlipSound);
        }

        public void PlayChipMove()
        {
            PlaySFX(ResourceManager.Instance?.ChipMoveSound);
        }

        public void PlayCheck()
        {
            PlaySFX(ResourceManager.Instance?.CheckSound);
        }

        public void PlayCall()
        {
            PlaySFX(ResourceManager.Instance?.CallSound);
        }

        public void PlayRaise()
        {
            PlaySFX(ResourceManager.Instance?.RaiseSound);
        }

        public void PlayAllIn()
        {
            PlaySFX(ResourceManager.Instance?.AllInSound);
        }

        public void PlayFold()
        {
            PlaySFX(ResourceManager.Instance?.FoldSound);
        }

        public void PlayWin()
        {
            PlaySFX(ResourceManager.Instance?.WinSound);
        }

        public void PlayLose()
        {
            PlaySFX(ResourceManager.Instance?.LoseSound);
        }

        public void PlayButtonClick()
        {
            PlayUI(ResourceManager.Instance?.ButtonClickSound);
        }

        public void PlayTimerTick()
        {
            PlayUI(ResourceManager.Instance?.TimerTickSound);
        }

        // Getters
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public float UIVolume => uiVolume;
        public bool IsMuted => _isMuted;
    }
}
