using System;
using System.Collections.Generic;
using UnityEngine;
using TexasHoldem.Core;
using TexasHoldem.Game;

namespace TexasHoldem.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject gameTablePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject profilePanel;

        [Header("Game Table UI")]
        [SerializeField] private Transform[] playerSlots;
        [SerializeField] private Transform communityCardsContainer;
        [SerializeField] private Transform potDisplay;

        [Header("Action Buttons")]
        [SerializeField] private GameObject actionPanel;
        [SerializeField] private UnityEngine.UI.Button foldButton;
        [SerializeField] private UnityEngine.UI.Button checkButton;
        [SerializeField] private UnityEngine.UI.Button callButton;
        [SerializeField] private UnityEngine.UI.Button raiseButton;
        [SerializeField] private UnityEngine.UI.Button allInButton;
        [SerializeField] private UnityEngine.UI.Slider raiseSlider;
        [SerializeField] private TMPro.TMP_Text raiseAmountText;
        [SerializeField] private TMPro.TMP_Text callAmountText;
        [SerializeField] private TMPro.TMP_Text potText;

        [Header("Prefabs")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private GameObject chipPrefab;
        [SerializeField] private GameObject playerHUDPrefab;

        private Dictionary<int, PlayerHUD> _playerHUDs;
        private List<GameObject> _communityCardObjects;
        private GameManager _gameManager;

        public event Action<PlayerAction, long> OnPlayerAction;

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
            }

            _playerHUDs = new Dictionary<int, PlayerHUD>();
            _communityCardObjects = new List<GameObject>();
        }

        private void Start()
        {
            SetupButtons();
            ShowMainMenu();
        }

        private void SetupButtons()
        {
            foldButton?.onClick.AddListener(() => OnPlayerAction?.Invoke(PlayerAction.Fold, 0));
            checkButton?.onClick.AddListener(() => OnPlayerAction?.Invoke(PlayerAction.Check, 0));
            callButton?.onClick.AddListener(() => OnPlayerAction?.Invoke(PlayerAction.Call, 0));
            allInButton?.onClick.AddListener(() => OnPlayerAction?.Invoke(PlayerAction.AllIn, 0));
            
            raiseButton?.onClick.AddListener(() =>
            {
                long amount = (long)raiseSlider.value;
                OnPlayerAction?.Invoke(PlayerAction.Raise, amount);
            });

            raiseSlider?.onValueChanged.AddListener(value =>
            {
                if (raiseAmountText != null)
                    raiseAmountText.text = $"${value:N0}";
            });
        }

        public void Initialize(GameManager gameManager)
        {
            _gameManager = gameManager;
            
            _gameManager.OnPhaseChanged += HandlePhaseChanged;
            _gameManager.OnPlayerAction += HandlePlayerAction;
            _gameManager.OnCardsDealt += HandleCardsDealt;
            _gameManager.OnHandComplete += HandleHandComplete;
        }

        #region Panel Management

        public void ShowMainMenu()
        {
            HideAllPanels();
            mainMenuPanel?.SetActive(true);
        }

        public void ShowLobby()
        {
            HideAllPanels();
            lobbyPanel?.SetActive(true);
        }

        public void ShowGameTable()
        {
            HideAllPanels();
            gameTablePanel?.SetActive(true);
        }

        public void ShowSettings()
        {
            settingsPanel?.SetActive(true);
        }

        public void ShowProfile()
        {
            profilePanel?.SetActive(true);
        }

        private void HideAllPanels()
        {
            mainMenuPanel?.SetActive(false);
            lobbyPanel?.SetActive(false);
            gameTablePanel?.SetActive(false);
            settingsPanel?.SetActive(false);
            profilePanel?.SetActive(false);
        }

        #endregion

        #region Game Table UI

        public void UpdatePlayerHUD(Player player)
        {
            if (!_playerHUDs.TryGetValue(player.SeatIndex, out var hud))
            {
                hud = CreatePlayerHUD(player.SeatIndex);
                _playerHUDs[player.SeatIndex] = hud;
            }

            hud.UpdatePlayer(player);
        }

        public void RemovePlayerHUD(int seatIndex)
        {
            if (_playerHUDs.TryGetValue(seatIndex, out var hud))
            {
                Destroy(hud.gameObject);
                _playerHUDs.Remove(seatIndex);
            }
        }

        private PlayerHUD CreatePlayerHUD(int seatIndex)
        {
            if (playerSlots == null || seatIndex >= playerSlots.Length)
                return null;

            var hudObj = Instantiate(playerHUDPrefab, playerSlots[seatIndex]);
            return hudObj.GetComponent<PlayerHUD>();
        }

        public void ShowCommunityCards(Card[] cards)
        {
            ClearCommunityCards();

            foreach (var card in cards)
            {
                var cardObj = CreateCardObject(card, communityCardsContainer);
                _communityCardObjects.Add(cardObj);
            }
        }

        public void ClearCommunityCards()
        {
            foreach (var cardObj in _communityCardObjects)
            {
                Destroy(cardObj);
            }
            _communityCardObjects.Clear();
        }

        private GameObject CreateCardObject(Card card, Transform parent)
        {
            var cardObj = Instantiate(cardPrefab, parent);
            var cardUI = cardObj.GetComponent<CardUI>();
            cardUI?.SetCard(card);
            return cardObj;
        }

        public void UpdatePot(long amount)
        {
            if (potText != null)
                potText.text = $"Pot: ${amount:N0}";
        }

        public void UpdateActionButtons(Player player, long currentBet, long minRaise, long maxRaise)
        {
            if (player == null || !player.CanAct)
            {
                actionPanel?.SetActive(false);
                return;
            }

            actionPanel?.SetActive(true);

            long callAmount = currentBet - player.CurrentBet;
            bool canCheck = callAmount <= 0;

            checkButton?.gameObject.SetActive(canCheck);
            callButton?.gameObject.SetActive(!canCheck);

            if (callAmountText != null && !canCheck)
                callAmountText.text = $"Call ${callAmount:N0}";

            if (raiseSlider != null)
            {
                raiseSlider.minValue = minRaise;
                raiseSlider.maxValue = maxRaise;
                raiseSlider.value = minRaise;
            }

            bool canRaise = maxRaise > minRaise;
            raiseButton?.gameObject.SetActive(canRaise);
            raiseSlider?.gameObject.SetActive(canRaise);
        }

        public void HideActionButtons()
        {
            actionPanel?.SetActive(false);
        }

        public void HighlightCurrentPlayer(int seatIndex)
        {
            foreach (var kvp in _playerHUDs)
            {
                kvp.Value.SetHighlight(kvp.Key == seatIndex);
            }
        }

        #endregion

        #region Event Handlers

        private void HandlePhaseChanged(object sender, GameEventArgs e)
        {
            Debug.Log($"Phase changed to: {e.Phase}");
            
            if (_gameManager != null)
            {
                HighlightCurrentPlayer(_gameManager.State.CurrentPlayerSeat);
            }
        }

        private void HandlePlayerAction(object sender, GameEventArgs e)
        {
            Debug.Log($"{e.Player.Name} {e.Action} {e.Amount}");
            UpdatePlayerHUD(e.Player);
            UpdatePot(_gameManager.TotalPot);
        }

        private void HandleCardsDealt(object sender, GameEventArgs e)
        {
            if (e.Message == "Flop" || e.Message == "Turn" || e.Message == "River")
            {
                ShowCommunityCards(e.Cards);
            }
        }

        private void HandleHandComplete(object sender, GameEventArgs e)
        {
            Debug.Log(e.Message);
            HideActionButtons();
        }

        #endregion

        #region Animations

        public void PlayDealCardAnimation(int seatIndex, Card card, bool faceUp = false)
        {
            // TODO: Implement card dealing animation
        }

        public void PlayChipAnimation(int fromSeat, int toSeat, long amount)
        {
            // TODO: Implement chip movement animation
        }

        public void PlayWinAnimation(int seatIndex, long amount)
        {
            // TODO: Implement win celebration animation
        }

        #endregion

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnPhaseChanged -= HandlePhaseChanged;
                _gameManager.OnPlayerAction -= HandlePlayerAction;
                _gameManager.OnCardsDealt -= HandleCardsDealt;
                _gameManager.OnHandComplete -= HandleHandComplete;
            }
        }
    }
}
