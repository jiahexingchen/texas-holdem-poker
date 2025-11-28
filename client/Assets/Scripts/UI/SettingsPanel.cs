using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TexasHoldem.Audio;

namespace TexasHoldem.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle muteToggle;

        [Header("Game Settings")]
        [SerializeField] private Toggle autoMuckToggle;
        [SerializeField] private Toggle showHandStrengthToggle;
        [SerializeField] private Toggle fourColorDeckToggle;
        [SerializeField] private TMP_Dropdown tableThemeDropdown;
        [SerializeField] private TMP_Dropdown cardBackDropdown;

        [Header("Notification Settings")]
        [SerializeField] private Toggle pushNotificationToggle;
        [SerializeField] private Toggle soundNotificationToggle;
        [SerializeField] private Toggle vibrationToggle;

        [Header("Language")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button resetButton;

        private SettingsData _currentSettings;
        private SettingsData _originalSettings;

        private void Start()
        {
            LoadSettings();
            SetupUI();
            SetupListeners();
        }

        private void SetupUI()
        {
            if (tableThemeDropdown != null)
            {
                tableThemeDropdown.ClearOptions();
                tableThemeDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "经典绿色", "深蓝色", "红色", "紫色"
                });
            }

            if (cardBackDropdown != null)
            {
                cardBackDropdown.ClearOptions();
                cardBackDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "经典红色", "经典蓝色", "金色", "黑色"
                });
            }

            if (languageDropdown != null)
            {
                languageDropdown.ClearOptions();
                languageDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "简体中文", "繁體中文", "English"
                });
            }
        }

        private void SetupListeners()
        {
            masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
            muteToggle?.onValueChanged.AddListener(OnMuteChanged);

            autoMuckToggle?.onValueChanged.AddListener(v => _currentSettings.autoMuck = v);
            showHandStrengthToggle?.onValueChanged.AddListener(v => _currentSettings.showHandStrength = v);
            fourColorDeckToggle?.onValueChanged.AddListener(v => _currentSettings.fourColorDeck = v);
            tableThemeDropdown?.onValueChanged.AddListener(v => _currentSettings.tableTheme = v);
            cardBackDropdown?.onValueChanged.AddListener(v => _currentSettings.cardBack = v);

            pushNotificationToggle?.onValueChanged.AddListener(v => _currentSettings.pushNotification = v);
            soundNotificationToggle?.onValueChanged.AddListener(v => _currentSettings.soundNotification = v);
            vibrationToggle?.onValueChanged.AddListener(v => _currentSettings.vibration = v);

            languageDropdown?.onValueChanged.AddListener(v => _currentSettings.language = v);

            saveButton?.onClick.AddListener(SaveSettings);
            cancelButton?.onClick.AddListener(CancelChanges);
            resetButton?.onClick.AddListener(ResetToDefaults);
        }

        private void OnMasterVolumeChanged(float value)
        {
            _currentSettings.masterVolume = value;
            AudioManager.Instance?.SetMasterVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            _currentSettings.musicVolume = value;
            AudioManager.Instance?.SetMusicVolume(value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            _currentSettings.sfxVolume = value;
            AudioManager.Instance?.SetSFXVolume(value);
        }

        private void OnMuteChanged(bool muted)
        {
            _currentSettings.muted = muted;
            if (muted)
                AudioManager.Instance?.MuteAll();
            else
                AudioManager.Instance?.UnmuteAll();
        }

        private void LoadSettings()
        {
            _currentSettings = new SettingsData
            {
                masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f),
                musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f),
                sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f),
                muted = PlayerPrefs.GetInt("Muted", 0) == 1,
                autoMuck = PlayerPrefs.GetInt("AutoMuck", 1) == 1,
                showHandStrength = PlayerPrefs.GetInt("ShowHandStrength", 1) == 1,
                fourColorDeck = PlayerPrefs.GetInt("FourColorDeck", 0) == 1,
                tableTheme = PlayerPrefs.GetInt("TableTheme", 0),
                cardBack = PlayerPrefs.GetInt("CardBack", 0),
                pushNotification = PlayerPrefs.GetInt("PushNotification", 1) == 1,
                soundNotification = PlayerPrefs.GetInt("SoundNotification", 1) == 1,
                vibration = PlayerPrefs.GetInt("Vibration", 1) == 1,
                language = PlayerPrefs.GetInt("Language", 0)
            };

            _originalSettings = _currentSettings.Clone();
            ApplySettingsToUI();
        }

        private void ApplySettingsToUI()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.value = _currentSettings.masterVolume;
            if (musicVolumeSlider != null) musicVolumeSlider.value = _currentSettings.musicVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = _currentSettings.sfxVolume;
            if (muteToggle != null) muteToggle.isOn = _currentSettings.muted;

            if (autoMuckToggle != null) autoMuckToggle.isOn = _currentSettings.autoMuck;
            if (showHandStrengthToggle != null) showHandStrengthToggle.isOn = _currentSettings.showHandStrength;
            if (fourColorDeckToggle != null) fourColorDeckToggle.isOn = _currentSettings.fourColorDeck;
            if (tableThemeDropdown != null) tableThemeDropdown.value = _currentSettings.tableTheme;
            if (cardBackDropdown != null) cardBackDropdown.value = _currentSettings.cardBack;

            if (pushNotificationToggle != null) pushNotificationToggle.isOn = _currentSettings.pushNotification;
            if (soundNotificationToggle != null) soundNotificationToggle.isOn = _currentSettings.soundNotification;
            if (vibrationToggle != null) vibrationToggle.isOn = _currentSettings.vibration;

            if (languageDropdown != null) languageDropdown.value = _currentSettings.language;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", _currentSettings.masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", _currentSettings.musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", _currentSettings.sfxVolume);
            PlayerPrefs.SetInt("Muted", _currentSettings.muted ? 1 : 0);
            PlayerPrefs.SetInt("AutoMuck", _currentSettings.autoMuck ? 1 : 0);
            PlayerPrefs.SetInt("ShowHandStrength", _currentSettings.showHandStrength ? 1 : 0);
            PlayerPrefs.SetInt("FourColorDeck", _currentSettings.fourColorDeck ? 1 : 0);
            PlayerPrefs.SetInt("TableTheme", _currentSettings.tableTheme);
            PlayerPrefs.SetInt("CardBack", _currentSettings.cardBack);
            PlayerPrefs.SetInt("PushNotification", _currentSettings.pushNotification ? 1 : 0);
            PlayerPrefs.SetInt("SoundNotification", _currentSettings.soundNotification ? 1 : 0);
            PlayerPrefs.SetInt("Vibration", _currentSettings.vibration ? 1 : 0);
            PlayerPrefs.SetInt("Language", _currentSettings.language);
            PlayerPrefs.Save();

            _originalSettings = _currentSettings.Clone();

            AudioManager.Instance?.PlaySuccess();
            gameObject.SetActive(false);
        }

        private void CancelChanges()
        {
            _currentSettings = _originalSettings.Clone();
            ApplySettingsToUI();

            AudioManager.Instance?.SetMasterVolume(_currentSettings.masterVolume);
            AudioManager.Instance?.SetMusicVolume(_currentSettings.musicVolume);
            AudioManager.Instance?.SetSFXVolume(_currentSettings.sfxVolume);

            gameObject.SetActive(false);
        }

        private void ResetToDefaults()
        {
            _currentSettings = SettingsData.Default();
            ApplySettingsToUI();

            AudioManager.Instance?.SetMasterVolume(_currentSettings.masterVolume);
            AudioManager.Instance?.SetMusicVolume(_currentSettings.musicVolume);
            AudioManager.Instance?.SetSFXVolume(_currentSettings.sfxVolume);
        }

        public static SettingsData GetCurrentSettings()
        {
            return new SettingsData
            {
                masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f),
                musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f),
                sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f),
                muted = PlayerPrefs.GetInt("Muted", 0) == 1,
                autoMuck = PlayerPrefs.GetInt("AutoMuck", 1) == 1,
                showHandStrength = PlayerPrefs.GetInt("ShowHandStrength", 1) == 1,
                fourColorDeck = PlayerPrefs.GetInt("FourColorDeck", 0) == 1,
                tableTheme = PlayerPrefs.GetInt("TableTheme", 0),
                cardBack = PlayerPrefs.GetInt("CardBack", 0),
                pushNotification = PlayerPrefs.GetInt("PushNotification", 1) == 1,
                soundNotification = PlayerPrefs.GetInt("SoundNotification", 1) == 1,
                vibration = PlayerPrefs.GetInt("Vibration", 1) == 1,
                language = PlayerPrefs.GetInt("Language", 0)
            };
        }
    }

    [System.Serializable]
    public class SettingsData
    {
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public bool muted;
        public bool autoMuck;
        public bool showHandStrength;
        public bool fourColorDeck;
        public int tableTheme;
        public int cardBack;
        public bool pushNotification;
        public bool soundNotification;
        public bool vibration;
        public int language;

        public SettingsData Clone()
        {
            return new SettingsData
            {
                masterVolume = this.masterVolume,
                musicVolume = this.musicVolume,
                sfxVolume = this.sfxVolume,
                muted = this.muted,
                autoMuck = this.autoMuck,
                showHandStrength = this.showHandStrength,
                fourColorDeck = this.fourColorDeck,
                tableTheme = this.tableTheme,
                cardBack = this.cardBack,
                pushNotification = this.pushNotification,
                soundNotification = this.soundNotification,
                vibration = this.vibration,
                language = this.language
            };
        }

        public static SettingsData Default()
        {
            return new SettingsData
            {
                masterVolume = 1f,
                musicVolume = 0.5f,
                sfxVolume = 1f,
                muted = false,
                autoMuck = true,
                showHandStrength = true,
                fourColorDeck = false,
                tableTheme = 0,
                cardBack = 0,
                pushNotification = true,
                soundNotification = true,
                vibration = true,
                language = 0
            };
        }
    }
}
