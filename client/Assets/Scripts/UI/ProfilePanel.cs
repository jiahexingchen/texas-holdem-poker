using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.UI
{
    public class ProfilePanel : MonoBehaviour
    {
        [Header("Basic Info")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image avatarFrameImage;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text userIdText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Slider expSlider;
        [SerializeField] private TMP_Text expText;

        [Header("Currency")]
        [SerializeField] private TMP_Text chipsText;
        [SerializeField] private TMP_Text diamondsText;

        [Header("Stats")]
        [SerializeField] private TMP_Text handsPlayedText;
        [SerializeField] private TMP_Text handsWonText;
        [SerializeField] private TMP_Text winRateText;
        [SerializeField] private TMP_Text biggestPotText;
        [SerializeField] private TMP_Text totalWinningsText;
        [SerializeField] private TMP_Text playTimeText;

        [Header("Achievements")]
        [SerializeField] private TMP_Text achievementCountText;
        [SerializeField] private Transform recentAchievementsContainer;
        [SerializeField] private GameObject achievementIconPrefab;

        [Header("Edit Profile")]
        [SerializeField] private Button editNicknameButton;
        [SerializeField] private Button changeAvatarButton;
        [SerializeField] private GameObject editNicknamePanel;
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button saveNicknameButton;
        [SerializeField] private Button cancelEditButton;

        [Header("Actions")]
        [SerializeField] private Button addFriendButton;
        [SerializeField] private Button inviteGameButton;
        [SerializeField] private Button blockButton;
        [SerializeField] private Button closeButton;

        private ProfileData _profileData;
        private bool _isOwnProfile;

        public event Action<string> OnNicknameChanged;
        public event Action OnChangeAvatar;
        public event Action<string> OnAddFriend;
        public event Action<string> OnInviteToGame;
        public event Action<string> OnBlockUser;

        private void Start()
        {
            SetupButtons();
        }

        private void SetupButtons()
        {
            editNicknameButton?.onClick.AddListener(ShowEditNickname);
            saveNicknameButton?.onClick.AddListener(SaveNickname);
            cancelEditButton?.onClick.AddListener(HideEditNickname);
            changeAvatarButton?.onClick.AddListener(() => OnChangeAvatar?.Invoke());
            
            addFriendButton?.onClick.AddListener(() => OnAddFriend?.Invoke(_profileData?.userId));
            inviteGameButton?.onClick.AddListener(() => OnInviteToGame?.Invoke(_profileData?.userId));
            blockButton?.onClick.AddListener(() => OnBlockUser?.Invoke(_profileData?.userId));
            
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void SetProfile(ProfileData data, bool isOwnProfile = false)
        {
            _profileData = data;
            _isOwnProfile = isOwnProfile;

            UpdateDisplay();
            UpdateButtonVisibility();
        }

        private void UpdateDisplay()
        {
            if (_profileData == null) return;

            // Basic info
            if (nicknameText != null) nicknameText.text = _profileData.nickname;
            if (userIdText != null) userIdText.text = $"ID: {_profileData.userId}";
            if (levelText != null) levelText.text = $"Lv.{_profileData.level}";

            // Exp progress
            if (expSlider != null)
            {
                long expRequired = _profileData.level * 1000;
                expSlider.value = (float)_profileData.exp / expRequired;
            }
            if (expText != null)
            {
                long expRequired = _profileData.level * 1000;
                expText.text = $"{_profileData.exp}/{expRequired}";
            }

            // Currency
            if (chipsText != null) chipsText.text = FormatNumber(_profileData.chips);
            if (diamondsText != null) diamondsText.text = FormatNumber(_profileData.diamonds);

            // Stats
            if (handsPlayedText != null) handsPlayedText.text = _profileData.handsPlayed.ToString("N0");
            if (handsWonText != null) handsWonText.text = _profileData.handsWon.ToString("N0");
            if (winRateText != null) winRateText.text = $"{_profileData.winRate:P1}";
            if (biggestPotText != null) biggestPotText.text = FormatNumber(_profileData.biggestPot);
            if (totalWinningsText != null) totalWinningsText.text = FormatNumber(_profileData.totalWinnings);
            if (playTimeText != null) playTimeText.text = FormatPlayTime(_profileData.playTimeMinutes);

            // Achievements
            if (achievementCountText != null)
            {
                achievementCountText.text = $"{_profileData.achievementsUnlocked}/{_profileData.achievementsTotal}";
            }

            // Avatar
            if (avatarImage != null)
            {
                var sprite = Resources.Load<Sprite>($"Avatars/{_profileData.avatar}");
                if (sprite != null) avatarImage.sprite = sprite;
            }

            if (avatarFrameImage != null && !string.IsNullOrEmpty(_profileData.avatarFrame))
            {
                var frameSprite = Resources.Load<Sprite>($"Frames/{_profileData.avatarFrame}");
                if (frameSprite != null)
                {
                    avatarFrameImage.sprite = frameSprite;
                    avatarFrameImage.gameObject.SetActive(true);
                }
            }
        }

        private void UpdateButtonVisibility()
        {
            // Show edit buttons only for own profile
            editNicknameButton?.gameObject.SetActive(_isOwnProfile);
            changeAvatarButton?.gameObject.SetActive(_isOwnProfile);

            // Show social buttons only for other profiles
            addFriendButton?.gameObject.SetActive(!_isOwnProfile);
            inviteGameButton?.gameObject.SetActive(!_isOwnProfile);
            blockButton?.gameObject.SetActive(!_isOwnProfile);
        }

        private void ShowEditNickname()
        {
            if (nicknameInput != null)
            {
                nicknameInput.text = _profileData?.nickname ?? "";
            }
            editNicknamePanel?.SetActive(true);
        }

        private void HideEditNickname()
        {
            editNicknamePanel?.SetActive(false);
        }

        private void SaveNickname()
        {
            if (nicknameInput == null) return;

            string newNickname = nicknameInput.text?.Trim();
            if (string.IsNullOrEmpty(newNickname)) return;

            if (newNickname.Length < 2 || newNickname.Length > 16)
            {
                Debug.LogWarning("Nickname must be 2-16 characters");
                return;
            }

            OnNicknameChanged?.Invoke(newNickname);
            
            if (_profileData != null)
            {
                _profileData.nickname = newNickname;
                UpdateDisplay();
            }

            HideEditNickname();
        }

        private string FormatNumber(long number)
        {
            if (number >= 1000000000) return $"{number / 1000000000.0:F1}B";
            if (number >= 1000000) return $"{number / 1000000.0:F1}M";
            if (number >= 1000) return $"{number / 1000.0:F1}K";
            return number.ToString("N0");
        }

        private string FormatPlayTime(long minutes)
        {
            if (minutes < 60) return $"{minutes}分钟";
            if (minutes < 1440) return $"{minutes / 60}小时{minutes % 60}分钟";
            return $"{minutes / 1440}天{(minutes % 1440) / 60}小时";
        }
    }

    [Serializable]
    public class ProfileData
    {
        public string userId;
        public string nickname;
        public string avatar;
        public string avatarFrame;
        public int level;
        public long exp;
        public long chips;
        public long diamonds;
        public int vipLevel;
        public long handsPlayed;
        public long handsWon;
        public float winRate;
        public long biggestPot;
        public long totalWinnings;
        public long playTimeMinutes;
        public int achievementsUnlocked;
        public int achievementsTotal;
        public bool isFriend;
        public bool isBlocked;
    }
}
