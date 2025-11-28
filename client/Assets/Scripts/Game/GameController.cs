using System;
using System.Collections;
using UnityEngine;
using TexasHoldem.Core;
using TexasHoldem.AI;
using TexasHoldem.Network;
using TexasHoldem.UI;

namespace TexasHoldem.Game
{
    public enum GameMode
    {
        Offline,    // 单机模式
        Online      // 联机模式
    }

    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private GameMode gameMode = GameMode.Offline;
        [SerializeField] private GameConfig gameConfig;

        [Header("References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private NetworkManager networkManager;

        private GameManager _gameManager;
        private AIManager _aiManager;
        private MessageHandler _messageHandler;
        private Player _localPlayer;
        private bool _isMyTurn;
        private Coroutine _aiTurnCoroutine;

        public GameManager GameManager => _gameManager;
        public Player LocalPlayer => _localPlayer;
        public bool IsMyTurn => _isMyTurn;
        public GameMode Mode => gameMode;

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

            InitializeGame();
        }

        private void InitializeGame()
        {
            if (gameConfig == null)
            {
                gameConfig = GameConfig.Default;
            }

            _gameManager = new GameManager(gameConfig);
            _aiManager = new AIManager(_gameManager);

            _gameManager.OnPhaseChanged += HandlePhaseChanged;
            _gameManager.OnPlayerAction += HandlePlayerAction;
            _gameManager.OnCardsDealt += HandleCardsDealt;
            _gameManager.OnHandComplete += HandleHandComplete;

            if (uiManager != null)
            {
                uiManager.Initialize(_gameManager);
                uiManager.OnPlayerAction += HandleLocalPlayerAction;
            }
        }

        public void StartOfflineGame(string playerName, int aiCount, AIDifficulty difficulty)
        {
            gameMode = GameMode.Offline;

            _localPlayer = new Player(Guid.NewGuid().ToString(), playerName, 1000);
            _gameManager.AddPlayer(_localPlayer);

            for (int i = 0; i < aiCount; i++)
            {
                var aiPlayer = _aiManager.CreateAIPlayer($"Bot_{i + 1}", 1000, difficulty);
                _gameManager.AddPlayer(aiPlayer);
            }

            uiManager?.ShowGameTable();
            StartNewHand();
        }

        public async void StartOnlineGame(string serverUrl, string token)
        {
            gameMode = GameMode.Online;

            if (networkManager == null)
            {
                networkManager = NetworkManager.Instance;
            }

            _messageHandler = GetComponent<MessageHandler>();
            if (_messageHandler == null)
            {
                _messageHandler = gameObject.AddComponent<MessageHandler>();
            }

            _messageHandler.SetGameManager(_gameManager);
            SetupNetworkCallbacks();

            bool connected = await networkManager.ConnectAsync(token);
            if (connected)
            {
                uiManager?.ShowLobby();
            }
            else
            {
                Debug.LogError("Failed to connect to server");
            }
        }

        private void SetupNetworkCallbacks()
        {
            _messageHandler.OnRoomJoined += HandleRoomJoined;
            _messageHandler.OnGameStateUpdate += HandleGameStateUpdate;
            _messageHandler.OnPlayerActionReceived += HandleNetworkPlayerAction;
            _messageHandler.OnCardsDealt += HandleNetworkCardsDealt;
            _messageHandler.OnHandResult += HandleHandResult;
        }

        public void StartNewHand()
        {
            if (!_gameManager.CanStartHand())
            {
                Debug.LogWarning("Cannot start hand");
                return;
            }

            _gameManager.StartNewHand();
            UpdateUI();
            CheckForAITurn();
        }

        private void HandlePhaseChanged(object sender, GameEventArgs e)
        {
            Debug.Log($"Phase changed to: {e.Phase}");
            UpdateUI();
            CheckForAITurn();
        }

        private void HandlePlayerAction(object sender, GameEventArgs e)
        {
            Debug.Log($"{e.Player.Name}: {e.Action} ${e.Amount}");
            UpdateUI();

            if (_gameManager.State.Phase != GamePhase.Finished)
            {
                CheckForAITurn();
            }
        }

        private void HandleCardsDealt(object sender, GameEventArgs e)
        {
            if (e.Cards != null)
            {
                string cardsStr = string.Join(", ", Array.ConvertAll(e.Cards, c => c?.ToString() ?? "?"));
                Debug.Log($"Cards dealt: {cardsStr}");
            }
            UpdateUI();
        }

        private void HandleHandComplete(object sender, GameEventArgs e)
        {
            Debug.Log($"Hand complete: {e.Message}");
            UpdateUI();

            StartCoroutine(StartNextHandAfterDelay(3f));
        }

        private IEnumerator StartNextHandAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_gameManager.CanStartHand())
            {
                StartNewHand();
            }
        }

        private void HandleLocalPlayerAction(PlayerAction action, long amount)
        {
            if (!_isMyTurn || _localPlayer == null)
                return;

            if (gameMode == GameMode.Online)
            {
                _messageHandler.SendPlayerAction(action, amount);
            }
            else
            {
                _gameManager.ProcessAction(_localPlayer.Id, action, amount);
                _isMyTurn = false;
            }
        }

        private void CheckForAITurn()
        {
            if (gameMode == GameMode.Online)
                return;

            var currentPlayer = _gameManager.GetCurrentPlayer();
            if (currentPlayer == null)
                return;

            if (currentPlayer.Id == _localPlayer?.Id)
            {
                _isMyTurn = true;
                ShowActionButtons();
            }
            else if (currentPlayer.IsBot)
            {
                _isMyTurn = false;
                HideActionButtons();

                if (_aiTurnCoroutine != null)
                {
                    StopCoroutine(_aiTurnCoroutine);
                }
                _aiTurnCoroutine = StartCoroutine(ProcessAITurnWithDelay());
            }
        }

        private IEnumerator ProcessAITurnWithDelay()
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));

            if (_gameManager.State.Phase != GamePhase.Finished)
            {
                _aiManager.ProcessAITurn();
            }
        }

        private void ShowActionButtons()
        {
            if (_localPlayer == null || uiManager == null)
                return;

            var (minRaise, maxRaise) = _gameManager.GetRaiseLimits(_localPlayer.Id);
            uiManager.UpdateActionButtons(
                _localPlayer,
                _gameManager.State.CurrentBet,
                minRaise,
                maxRaise
            );
        }

        private void HideActionButtons()
        {
            uiManager?.HideActionButtons();
        }

        private void UpdateUI()
        {
            if (uiManager == null)
                return;

            foreach (var player in _gameManager.Players)
            {
                uiManager.UpdatePlayerHUD(player);
            }

            uiManager.UpdatePot(_gameManager.TotalPot);
            uiManager.HighlightCurrentPlayer(_gameManager.State.CurrentPlayerSeat);

            if (_gameManager.State.CommunityCardCount > 0)
            {
                uiManager.ShowCommunityCards(_gameManager.State.GetVisibleCommunityCards());
            }
        }

        #region Network Handlers

        private void HandleRoomJoined(RoomInfo room)
        {
            Debug.Log($"Joined room: {room.roomId}");
            uiManager?.ShowGameTable();
        }

        private void HandleGameStateUpdate(GameStateData state)
        {
            UpdateUI();
            
            if (state.currentPlayerSeat == GetLocalPlayerSeat())
            {
                _isMyTurn = true;
                ShowActionButtons();
            }
            else
            {
                _isMyTurn = false;
                HideActionButtons();
            }
        }

        private void HandleNetworkPlayerAction(PlayerActionData action)
        {
            Debug.Log($"Network action: {action.playerId} {action.action} ${action.amount}");
        }

        private void HandleNetworkCardsDealt(CardData[] cards)
        {
            var convertedCards = Array.ConvertAll(cards, c => c.ToCard());
            uiManager?.ShowCommunityCards(convertedCards);
        }

        private void HandleHandResult(WinnerData[] winners)
        {
            foreach (var winner in winners)
            {
                Debug.Log($"Winner: {winner.playerId} wins ${winner.amount} with {winner.handType}");
            }
        }

        private int GetLocalPlayerSeat()
        {
            return _localPlayer?.SeatIndex ?? -1;
        }

        #endregion

        public void LeaveGame()
        {
            if (gameMode == GameMode.Online)
            {
                _messageHandler?.SendLeaveRoom();
                networkManager?.Disconnect();
            }

            if (_aiTurnCoroutine != null)
            {
                StopCoroutine(_aiTurnCoroutine);
            }

            _gameManager = new GameManager(gameConfig);
            _aiManager = new AIManager(_gameManager);
            _localPlayer = null;

            uiManager?.ShowMainMenu();
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnPhaseChanged -= HandlePhaseChanged;
                _gameManager.OnPlayerAction -= HandlePlayerAction;
                _gameManager.OnCardsDealt -= HandleCardsDealt;
                _gameManager.OnHandComplete -= HandleHandComplete;
            }

            if (uiManager != null)
            {
                uiManager.OnPlayerAction -= HandleLocalPlayerAction;
            }
        }
    }
}
