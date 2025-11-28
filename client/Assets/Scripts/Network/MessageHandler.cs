using System;
using System.Collections.Generic;
using UnityEngine;
using TexasHoldem.Core;
using TexasHoldem.Game;

namespace TexasHoldem.Network
{
    public class MessageHandler : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        
        private GameManager _gameManager;
        private Dictionary<string, Action<MessageData>> _handlers;

        public event Action<RoomInfo> OnRoomJoined;
        public event Action<string> OnRoomLeft;
        public event Action<PlayerInfo> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<GameStateData> OnGameStateUpdate;
        public event Action<PlayerActionData> OnPlayerActionReceived;
        public event Action<CardData[]> OnCardsDealt;
        public event Action<WinnerData[]> OnHandResult;
        public event Action<ChatMessage> OnChatReceived;
        public event Action<string> OnErrorReceived;

        private void Start()
        {
            if (networkManager == null)
                networkManager = NetworkManager.Instance;

            networkManager.OnMessageReceived += HandleMessage;
            InitializeHandlers();
        }

        public void SetGameManager(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        private void InitializeHandlers()
        {
            _handlers = new Dictionary<string, Action<MessageData>>
            {
                { "room_joined", HandleRoomJoined },
                { "room_left", HandleRoomLeft },
                { "player_joined", HandlePlayerJoined },
                { "player_left", HandlePlayerLeft },
                { "game_state", HandleGameState },
                { "player_action", HandlePlayerAction },
                { "deal_cards", HandleDealCards },
                { "community_cards", HandleCommunityCards },
                { "hand_result", HandleHandResult },
                { "your_turn", HandleYourTurn },
                { "chat", HandleChat },
                { "error", HandleError }
            };
        }

        private void HandleMessage(GameMessage message)
        {
            if (_handlers.TryGetValue(message.type, out var handler))
            {
                try
                {
                    var data = string.IsNullOrEmpty(message.data) 
                        ? new MessageData() 
                        : JsonUtility.FromJson<MessageData>(message.data);
                    data.roomId = message.roomId;
                    data.playerId = message.playerId;
                    handler(data);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error handling message {message.type}: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Unknown message type: {message.type}");
            }
        }

        private void HandleRoomJoined(MessageData data)
        {
            var roomInfo = JsonUtility.FromJson<RoomInfo>(data.json);
            OnRoomJoined?.Invoke(roomInfo);
        }

        private void HandleRoomLeft(MessageData data)
        {
            OnRoomLeft?.Invoke(data.roomId);
        }

        private void HandlePlayerJoined(MessageData data)
        {
            var playerInfo = JsonUtility.FromJson<PlayerInfo>(data.json);
            OnPlayerJoined?.Invoke(playerInfo);
        }

        private void HandlePlayerLeft(MessageData data)
        {
            OnPlayerLeft?.Invoke(data.playerId);
        }

        private void HandleGameState(MessageData data)
        {
            var gameState = JsonUtility.FromJson<GameStateData>(data.json);
            OnGameStateUpdate?.Invoke(gameState);
        }

        private void HandlePlayerAction(MessageData data)
        {
            var actionData = JsonUtility.FromJson<PlayerActionData>(data.json);
            OnPlayerActionReceived?.Invoke(actionData);
        }

        private void HandleDealCards(MessageData data)
        {
            var cards = JsonUtility.FromJson<CardArrayWrapper>(data.json).cards;
            OnCardsDealt?.Invoke(cards);
        }

        private void HandleCommunityCards(MessageData data)
        {
            var cards = JsonUtility.FromJson<CardArrayWrapper>(data.json).cards;
            OnCardsDealt?.Invoke(cards);
        }

        private void HandleHandResult(MessageData data)
        {
            var winners = JsonUtility.FromJson<WinnerArrayWrapper>(data.json).winners;
            OnHandResult?.Invoke(winners);
        }

        private void HandleYourTurn(MessageData data)
        {
            var turnData = JsonUtility.FromJson<TurnData>(data.json);
            Debug.Log($"Your turn! Call: {turnData.callAmount}, Min Raise: {turnData.minRaise}");
        }

        private void HandleChat(MessageData data)
        {
            var chat = JsonUtility.FromJson<ChatMessage>(data.json);
            OnChatReceived?.Invoke(chat);
        }

        private void HandleError(MessageData data)
        {
            OnErrorReceived?.Invoke(data.message);
            Debug.LogError($"Server error: {data.message}");
        }

        #region Send Methods

        public void SendJoinRoom(string roomId)
        {
            networkManager.SendAction("join_room", new Dictionary<string, object>
            {
                { "roomId", roomId }
            });
        }

        public void SendLeaveRoom()
        {
            networkManager.SendAction("leave_room");
        }

        public void SendCreateRoom(RoomConfig config)
        {
            networkManager.SendAction("create_room", new Dictionary<string, object>
            {
                { "smallBlind", config.smallBlind },
                { "bigBlind", config.bigBlind },
                { "maxPlayers", config.maxPlayers },
                { "isPrivate", config.isPrivate }
            });
        }

        public void SendPlayerAction(PlayerAction action, long amount = 0)
        {
            networkManager.SendAction("player_action", new Dictionary<string, object>
            {
                { "action", action.ToString().ToLower() },
                { "amount", amount }
            });
        }

        public void SendChat(string message)
        {
            networkManager.SendAction("chat", new Dictionary<string, object>
            {
                { "message", message }
            });
        }

        public void SendQuickMatch(int blindLevel = 0)
        {
            networkManager.SendAction("quick_match", new Dictionary<string, object>
            {
                { "blindLevel", blindLevel }
            });
        }

        public void SendCancelMatch()
        {
            networkManager.SendAction("cancel_match");
        }

        public void SendSitOut()
        {
            networkManager.SendAction("sit_out");
        }

        public void SendSitIn()
        {
            networkManager.SendAction("sit_in");
        }

        public void SendBuyIn(long amount)
        {
            networkManager.SendAction("buy_in", new Dictionary<string, object>
            {
                { "amount", amount }
            });
        }

        #endregion

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnMessageReceived -= HandleMessage;
            }
        }
    }

    #region Data Classes

    [Serializable]
    public class MessageData
    {
        public string roomId;
        public string playerId;
        public string message;
        public string json;
    }

    [Serializable]
    public class RoomInfo
    {
        public string roomId;
        public string roomName;
        public long smallBlind;
        public long bigBlind;
        public int maxPlayers;
        public int currentPlayers;
        public bool isPrivate;
        public PlayerInfo[] players;
    }

    [Serializable]
    public class RoomConfig
    {
        public long smallBlind = 10;
        public long bigBlind = 20;
        public int maxPlayers = 9;
        public bool isPrivate = false;
        public string password;
    }

    [Serializable]
    public class PlayerInfo
    {
        public string playerId;
        public string name;
        public string avatar;
        public int seatIndex;
        public long chips;
        public bool isBot;
    }

    [Serializable]
    public class GameStateData
    {
        public string phase;
        public int dealerSeat;
        public int currentPlayerSeat;
        public long currentBet;
        public long pot;
        public long minRaise;
        public PlayerStateData[] players;
        public CardData[] communityCards;
    }

    [Serializable]
    public class PlayerStateData
    {
        public string playerId;
        public int seatIndex;
        public long chips;
        public long currentBet;
        public string state;
        public string lastAction;
    }

    [Serializable]
    public class PlayerActionData
    {
        public string playerId;
        public string action;
        public long amount;
    }

    [Serializable]
    public class CardData
    {
        public int suit;
        public int rank;

        public Card ToCard()
        {
            return new Card((Suit)suit, (Rank)rank);
        }
    }

    [Serializable]
    public class CardArrayWrapper
    {
        public CardData[] cards;
    }

    [Serializable]
    public class WinnerData
    {
        public string playerId;
        public long amount;
        public string handType;
        public CardData[] bestHand;
    }

    [Serializable]
    public class WinnerArrayWrapper
    {
        public WinnerData[] winners;
    }

    [Serializable]
    public class TurnData
    {
        public long callAmount;
        public long minRaise;
        public long maxRaise;
        public float timeRemaining;
    }

    [Serializable]
    public class ChatMessage
    {
        public string playerId;
        public string playerName;
        public string message;
        public long timestamp;
    }

    #endregion
}
