using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TexasHoldem.Network;

namespace TexasHoldem.UI
{
    public class LobbyPanel : MonoBehaviour
    {
        [Header("User Info")]
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text chipsText;
        [SerializeField] private TMP_Text diamondsText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Slider expSlider;

        [Header("Quick Match")]
        [SerializeField] private Button quickMatchButton;
        [SerializeField] private TMP_Dropdown blindLevelDropdown;
        [SerializeField] private GameObject matchingPanel;
        [SerializeField] private Button cancelMatchButton;
        [SerializeField] private TMP_Text matchingStatusText;

        [Header("Room List")]
        [SerializeField] private Transform roomListContainer;
        [SerializeField] private GameObject roomItemPrefab;
        [SerializeField] private Button refreshRoomsButton;
        [SerializeField] private Button createRoomButton;

        [Header("Navigation")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button friendsButton;
        [SerializeField] private Button rankingButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button profileButton;

        [Header("Create Room")]
        [SerializeField] private GameObject createRoomPanel;
        [SerializeField] private TMP_Dropdown roomBlindDropdown;
        [SerializeField] private TMP_Dropdown maxPlayersDropdown;
        [SerializeField] private Toggle privateRoomToggle;
        [SerializeField] private TMP_InputField roomPasswordInput;
        [SerializeField] private Button confirmCreateButton;
        [SerializeField] private Button cancelCreateButton;

        private UserData _currentUser;
        private List<RoomInfo> _rooms;
        private bool _isMatching;
        private MessageHandler _messageHandler;

        public event Action<int> OnQuickMatch;
        public event Action OnCancelMatch;
        public event Action<string> OnJoinRoom;
        public event Action<RoomConfig> OnCreateRoom;
        public event Action OnOpenShop;
        public event Action OnOpenFriends;
        public event Action OnOpenRanking;
        public event Action OnOpenSettings;
        public event Action OnOpenProfile;

        private void Start()
        {
            SetupButtons();
            SetupDropdowns();
            HideMatchingPanel();
            HideCreateRoomPanel();
        }

        private void SetupButtons()
        {
            quickMatchButton?.onClick.AddListener(StartQuickMatch);
            cancelMatchButton?.onClick.AddListener(CancelMatch);
            refreshRoomsButton?.onClick.AddListener(RefreshRooms);
            createRoomButton?.onClick.AddListener(ShowCreateRoomPanel);
            
            confirmCreateButton?.onClick.AddListener(ConfirmCreateRoom);
            cancelCreateButton?.onClick.AddListener(HideCreateRoomPanel);

            playButton?.onClick.AddListener(StartQuickMatch);
            shopButton?.onClick.AddListener(() => OnOpenShop?.Invoke());
            friendsButton?.onClick.AddListener(() => OnOpenFriends?.Invoke());
            rankingButton?.onClick.AddListener(() => OnOpenRanking?.Invoke());
            settingsButton?.onClick.AddListener(() => OnOpenSettings?.Invoke());
            profileButton?.onClick.AddListener(() => OnOpenProfile?.Invoke());

            privateRoomToggle?.onValueChanged.AddListener(isOn => {
                roomPasswordInput?.gameObject.SetActive(isOn);
            });
        }

        private void SetupDropdowns()
        {
            if (blindLevelDropdown != null)
            {
                blindLevelDropdown.ClearOptions();
                blindLevelDropdown.AddOptions(new List<string>
                {
                    "新手场 5/10",
                    "初级场 10/20",
                    "中级场 25/50",
                    "高级场 50/100",
                    "大师场 100/200",
                    "至尊场 250/500"
                });
            }

            if (roomBlindDropdown != null)
            {
                roomBlindDropdown.ClearOptions();
                roomBlindDropdown.AddOptions(new List<string>
                {
                    "5/10", "10/20", "25/50", "50/100", "100/200", "250/500"
                });
            }

            if (maxPlayersDropdown != null)
            {
                maxPlayersDropdown.ClearOptions();
                maxPlayersDropdown.AddOptions(new List<string>
                {
                    "2人桌", "6人桌", "9人桌"
                });
                maxPlayersDropdown.value = 2; // Default 9人桌
            }
        }

        public void SetMessageHandler(MessageHandler handler)
        {
            _messageHandler = handler;
        }

        public void SetUserData(UserData user)
        {
            _currentUser = user;
            UpdateUserDisplay();
        }

        public void UpdateUserData(long chips, long diamonds, int level, long exp)
        {
            if (_currentUser != null)
            {
                _currentUser.chips = chips;
                _currentUser.diamonds = diamonds;
                _currentUser.level = level;
                _currentUser.exp = exp;
                UpdateUserDisplay();
            }
        }

        private void UpdateUserDisplay()
        {
            if (_currentUser == null) return;

            if (nicknameText != null)
                nicknameText.text = _currentUser.nickname;

            if (levelText != null)
                levelText.text = $"Lv.{_currentUser.level}";

            if (chipsText != null)
                chipsText.text = FormatNumber(_currentUser.chips);

            if (diamondsText != null)
                diamondsText.text = FormatNumber(_currentUser.diamonds);

            if (expSlider != null)
            {
                long expRequired = _currentUser.level * 1000;
                expSlider.value = (float)_currentUser.exp / expRequired;
            }
        }

        private string FormatNumber(long number)
        {
            if (number >= 1000000000)
                return $"{number / 1000000000.0:F1}B";
            if (number >= 1000000)
                return $"{number / 1000000.0:F1}M";
            if (number >= 1000)
                return $"{number / 1000.0:F1}K";
            return number.ToString("N0");
        }

        private void StartQuickMatch()
        {
            if (_isMatching) return;

            int blindLevel = blindLevelDropdown != null ? blindLevelDropdown.value : 0;
            
            _isMatching = true;
            ShowMatchingPanel();
            
            OnQuickMatch?.Invoke(blindLevel);
            _messageHandler?.SendQuickMatch(blindLevel);
        }

        private void CancelMatch()
        {
            _isMatching = false;
            HideMatchingPanel();
            
            OnCancelMatch?.Invoke();
            _messageHandler?.SendCancelMatch();
        }

        private void ShowMatchingPanel()
        {
            matchingPanel?.SetActive(true);
            if (matchingStatusText != null)
                matchingStatusText.text = "正在匹配中...";
        }

        private void HideMatchingPanel()
        {
            matchingPanel?.SetActive(false);
        }

        public void OnMatchFound(string roomId)
        {
            _isMatching = false;
            HideMatchingPanel();
        }

        public void OnMatchTimeout()
        {
            _isMatching = false;
            if (matchingStatusText != null)
                matchingStatusText.text = "匹配超时，已加入AI房间";
        }

        private void RefreshRooms()
        {
            // Request room list from server
            Debug.Log("Refreshing room list...");
        }

        public void SetRoomList(List<RoomInfo> rooms)
        {
            _rooms = rooms;
            UpdateRoomListDisplay();
        }

        private void UpdateRoomListDisplay()
        {
            if (roomListContainer == null || roomItemPrefab == null) return;

            // Clear existing items
            foreach (Transform child in roomListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create new items
            foreach (var room in _rooms)
            {
                var item = Instantiate(roomItemPrefab, roomListContainer);
                var roomItem = item.GetComponent<RoomListItem>();
                if (roomItem != null)
                {
                    roomItem.SetRoomInfo(room);
                    roomItem.OnJoinClicked += () => JoinRoom(room.roomId);
                }
            }
        }

        private void JoinRoom(string roomId)
        {
            OnJoinRoom?.Invoke(roomId);
            _messageHandler?.SendJoinRoom(roomId);
        }

        private void ShowCreateRoomPanel()
        {
            createRoomPanel?.SetActive(true);
            roomPasswordInput?.gameObject.SetActive(false);
        }

        private void HideCreateRoomPanel()
        {
            createRoomPanel?.SetActive(false);
        }

        private void ConfirmCreateRoom()
        {
            int blindIndex = roomBlindDropdown != null ? roomBlindDropdown.value : 0;
            int maxPlayersIndex = maxPlayersDropdown != null ? maxPlayersDropdown.value : 2;
            bool isPrivate = privateRoomToggle != null && privateRoomToggle.isOn;
            string password = roomPasswordInput?.text ?? "";

            long[] smallBlinds = { 5, 10, 25, 50, 100, 250 };
            long[] bigBlinds = { 10, 20, 50, 100, 200, 500 };
            int[] maxPlayerOptions = { 2, 6, 9 };

            var config = new RoomConfig
            {
                smallBlind = smallBlinds[blindIndex],
                bigBlind = bigBlinds[blindIndex],
                maxPlayers = maxPlayerOptions[maxPlayersIndex],
                isPrivate = isPrivate,
                password = password
            };

            OnCreateRoom?.Invoke(config);
            _messageHandler?.SendCreateRoom(config);
            
            HideCreateRoomPanel();
        }
    }

    public class RoomListItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text blindsText;
        [SerializeField] private TMP_Text playersText;
        [SerializeField] private Button joinButton;
        [SerializeField] private GameObject privateIcon;

        public event Action OnJoinClicked;

        private void Start()
        {
            joinButton?.onClick.AddListener(() => OnJoinClicked?.Invoke());
        }

        public void SetRoomInfo(RoomInfo room)
        {
            if (roomNameText != null)
                roomNameText.text = room.roomName ?? $"Room {room.roomId}";

            if (blindsText != null)
                blindsText.text = $"{room.smallBlind}/{room.bigBlind}";

            if (playersText != null)
                playersText.text = $"{room.currentPlayers}/{room.maxPlayers}";

            if (privateIcon != null)
                privateIcon.SetActive(room.isPrivate);

            if (joinButton != null)
                joinButton.interactable = room.currentPlayers < room.maxPlayers;
        }
    }
}
