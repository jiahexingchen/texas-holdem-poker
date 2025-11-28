using System.Collections.Generic;
using UnityEngine;

namespace TexasHoldem.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Audio Clips - Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameMusic;

        [Header("Audio Clips - Game SFX")]
        [SerializeField] private AudioClip cardDealClip;
        [SerializeField] private AudioClip cardFlipClip;
        [SerializeField] private AudioClip chipsBetClip;
        [SerializeField] private AudioClip chipsWinClip;
        [SerializeField] private AudioClip foldClip;
        [SerializeField] private AudioClip checkClip;
        [SerializeField] private AudioClip raiseClip;
        [SerializeField] private AudioClip allInClip;
        [SerializeField] private AudioClip timerWarningClip;
        [SerializeField] private AudioClip yourTurnClip;

        [Header("Audio Clips - UI")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip buttonHoverClip;
        [SerializeField] private AudioClip notificationClip;
        [SerializeField] private AudioClip errorClip;
        [SerializeField] private AudioClip successClip;

        [Header("Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.5f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float uiVolume = 0.8f;

        private Dictionary<string, AudioClip> _clipCache;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            LoadSettings();
            
            _clipCache = new Dictionary<string, AudioClip>
            {
                { "card_deal", cardDealClip },
                { "card_flip", cardFlipClip },
                { "chips_bet", chipsBetClip },
                { "chips_win", chipsWinClip },
                { "fold", foldClip },
                { "check", checkClip },
                { "raise", raiseClip },
                { "all_in", allInClip },
                { "timer_warning", timerWarningClip },
                { "your_turn", yourTurnClip },
                { "button_click", buttonClickClip },
                { "button_hover", buttonHoverClip },
                { "notification", notificationClip },
                { "error", errorClip },
                { "success", successClip }
            };
        }

        #region Music

        public void PlayMenuMusic()
        {
            PlayMusic(menuMusic);
        }

        public void PlayGameMusic()
        {
            PlayMusic(gameMusic);
        }

        public void PlayMusic(AudioClip clip)
        {
            if (musicSource == null || clip == null) return;

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (musicSource != null)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null)
            {
                musicSource.UnPause();
            }
        }

        #endregion

        #region SFX

        public void PlaySFX(string clipName)
        {
            if (_clipCache.TryGetValue(clipName, out var clip))
            {
                PlaySFX(clip);
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }

        public void PlayCardDeal()
        {
            PlaySFX(cardDealClip);
        }

        public void PlayCardFlip()
        {
            PlaySFX(cardFlipClip);
        }

        public void PlayChipsBet()
        {
            PlaySFX(chipsBetClip);
        }

        public void PlayChipsWin()
        {
            PlaySFX(chipsWinClip);
        }

        public void PlayFold()
        {
            PlaySFX(foldClip);
        }

        public void PlayCheck()
        {
            PlaySFX(checkClip);
        }

        public void PlayRaise()
        {
            PlaySFX(raiseClip);
        }

        public void PlayAllIn()
        {
            PlaySFX(allInClip);
        }

        public void PlayTimerWarning()
        {
            PlaySFX(timerWarningClip);
        }

        public void PlayYourTurn()
        {
            PlaySFX(yourTurnClip);
        }

        #endregion

        #region UI Sounds

        public void PlayButtonClick()
        {
            if (uiSource == null || buttonClickClip == null) return;
            uiSource.PlayOneShot(buttonClickClip, uiVolume * masterVolume);
        }

        public void PlayButtonHover()
        {
            if (uiSource == null || buttonHoverClip == null) return;
            uiSource.PlayOneShot(buttonHoverClip, uiVolume * masterVolume * 0.5f);
        }

        public void PlayNotification()
        {
            if (uiSource == null || notificationClip == null) return;
            uiSource.PlayOneShot(notificationClip, uiVolume * masterVolume);
        }

        public void PlayError()
        {
            if (uiSource == null || errorClip == null) return;
            uiSource.PlayOneShot(errorClip, uiVolume * masterVolume);
        }

        public void PlaySuccess()
        {
            if (uiSource == null || successClip == null) return;
            uiSource.PlayOneShot(successClip, uiVolume * masterVolume);
        }

        #endregion

        #region Volume Settings

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
            SaveSettings();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
            SaveSettings();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveSettings();
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            SaveSettings();
        }

        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
        public float GetUIVolume() => uiVolume;

        private void UpdateVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("UIVolume", uiVolume);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            uiVolume = PlayerPrefs.GetFloat("UIVolume", 0.8f);
            UpdateVolumes();
        }

        #endregion

        public void MuteAll()
        {
            SetMasterVolume(0);
        }

        public void UnmuteAll()
        {
            SetMasterVolume(1);
        }
    }
}
