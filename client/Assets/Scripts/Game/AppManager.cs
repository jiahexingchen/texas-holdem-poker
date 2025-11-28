using System;
using UnityEngine;
using TexasHoldem.UI;
using TexasHoldem.Network;
using TexasHoldem.AI;

namespace TexasHoldem.Game
{
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private LoginPanel loginPanel;
        [SerializeField] private LobbyPanel lobbyPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Settings")]
        [SerializeField] private string serverUrl = "http://localhost:8080";
        [SerializeField] private string wsUrl = "ws://localhost:8080/ws";

        private UserData _currentUser;
        private string _authToken;
        private NetworkManager _networkManager;
        private MessageHandler _messageHandler;
        private GameController _gameController;

        public UserData CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null && !string.IsNullOrEmpty(_authToken);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            Initialize();
        }

        private void Initialize()
        {
            _networkManager = NetworkManager.Instance;
            if (_networkManager == null)
            {
                var go = new GameObject("NetworkManager");
                _networkManager = go.AddComponent<NetworkManager>();
            }

            _messageHandler = GetComponent<MessageHandler>();
            if (_messageHandler == null)
            {
                _messageHandler = gameObject.AddComponent<MessageHandler>();
            }

            _gameController = GetComponent<GameController>();
            if (_gameController == null)
            {
                _gameController = gameObject.AddComponent<GameController>();
            }

            SetupCallbacks();
            ShowLoginPanel();
        }

        private void SetupCallbacks()
        {
            if (loginPanel != null)
            {
                loginPanel.OnLoginSuccess += HandleLoginSuccess;
                loginPanel.OnLoginFailed += HandleLoginFailed;
            }

            if (lobbyPanel != null)
            {
                lobbyPanel.SetMessageHandler(_messageHandler);
                lobbyPanel.OnQuickMatch += HandleQuickMatch;
                lobbyPanel.OnJoinRoom += HandleJoinRoom;
                lobbyPanel.OnCreateRoom += HandleCreateRoom;
            }

            if (_messageHandler != null)
            {
                _messageHandler.OnRoomJoined += HandleRoomJoined;
                _messageHandler.OnRoomLeft += HandleRoomLeft;
            }

            if (_networkManager != null)
            {
                _networkManager.OnConnected += HandleConnected;
                _networkManager.OnDisconnected += HandleDisconnected;
            }
        }

        #region Panel Management

        private void HideAllPanels()
        {
            loginPanel?.gameObject.SetActive(false);
            lobbyPanel?.gameObject.SetActive(false);
            gamePanel?.SetActive(false);
            loadingPanel?.SetActive(false);
        }

        private void ShowLoginPanel()
        {
            HideAllPanels();
            loginPanel?.gameObject.SetActive(true);
        }

        private void ShowLobbyPanel()
        {
            HideAllPanels();
            lobbyPanel?.gameObject.SetActive(true);
            lobbyPanel?.SetUserData(_currentUser);
        }

        private void ShowGamePanel()
        {
            HideAllPanels();
            gamePanel?.SetActive(true);
        }

        private void ShowLoading(bool show)
        {
            loadingPanel?.SetActive(show);
        }

        #endregion

        #region Auth Handlers

        private async void HandleLoginSuccess(string token, UserData user)
        {
            _authToken = token;
            _currentUser = user;

            HttpClient.SetAuthToken(token);

            ShowLoading(true);

            try
            {
                bool connected = await _networkManager.ConnectAsync(token);
                if (connected)
                {
                    ShowLobbyPanel();
                }
                else
                {
                    Debug.LogError("Failed to connect to game server");
                    ShowLoginPanel();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection error: {ex.Message}");
                ShowLoginPanel();
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void HandleLoginFailed(string error)
        {
            Debug.LogError($"Login failed: {error}");
        }

        public void Logout()
        {
            _authToken = null;
            _currentUser = null;
            HttpClient.ClearAuthToken();
            _networkManager?.Disconnect();
            ShowLoginPanel();
        }

        #endregion

        #region Lobby Handlers

        private void HandleQuickMatch(int blindLevel)
        {
            Debug.Log($"Quick match requested for blind level {blindLevel}");
        }

        private void HandleJoinRoom(string roomId)
        {
            Debug.Log($"Joining room: {roomId}");
        }

        private void HandleCreateRoom(RoomConfig config)
        {
            Debug.Log($"Creating room with blinds {config.smallBlind}/{config.bigBlind}");
        }

        #endregion

        #region Network Handlers

        private void HandleConnected()
        {
            Debug.Log("Connected to game server");
        }

        private void HandleDisconnected()
        {
            Debug.Log("Disconnected from game server");
            
            if (gamePanel != null && gamePanel.activeSelf)
            {
                ShowLobbyPanel();
            }
        }

        private void HandleRoomJoined(RoomInfo room)
        {
            Debug.Log($"Joined room: {room.roomId}");
            lobbyPanel?.OnMatchFound(room.roomId);
            ShowGamePanel();
        }

        private void HandleRoomLeft(string roomId)
        {
            Debug.Log($"Left room: {roomId}");
            ShowLobbyPanel();
        }

        #endregion

        #region Game Actions

        public void StartOfflineGame(AIDifficulty difficulty = AIDifficulty.Medium, int aiCount = 3)
        {
            if (_currentUser == null)
            {
                _currentUser = new UserData
                {
                    id = Guid.NewGuid().ToString(),
                    nickname = "Player",
                    chips = 10000,
                    level = 1
                };
            }

            ShowGamePanel();
            _gameController?.StartOfflineGame(_currentUser.nickname, aiCount, difficulty);
        }

        public void LeaveGame()
        {
            _messageHandler?.SendLeaveRoom();
            _gameController?.LeaveGame();
            ShowLobbyPanel();
        }

        #endregion

        #region User Data

        public void UpdateUserChips(long chips)
        {
            if (_currentUser != null)
            {
                _currentUser.chips = chips;
                lobbyPanel?.UpdateUserData(chips, _currentUser.diamonds, _currentUser.level, _currentUser.exp);
            }
        }

        public long GetUserChips()
        {
            return _currentUser?.chips ?? 0;
        }

        #endregion

        private void OnDestroy()
        {
            if (loginPanel != null)
            {
                loginPanel.OnLoginSuccess -= HandleLoginSuccess;
                loginPanel.OnLoginFailed -= HandleLoginFailed;
            }

            if (_messageHandler != null)
            {
                _messageHandler.OnRoomJoined -= HandleRoomJoined;
                _messageHandler.OnRoomLeft -= HandleRoomLeft;
            }

            if (_networkManager != null)
            {
                _networkManager.OnConnected -= HandleConnected;
                _networkManager.OnDisconnected -= HandleDisconnected;
            }
        }
    }
}
