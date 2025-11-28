using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TexasHoldem.Network
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting
    }

    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Server Settings")]
        [SerializeField] private string serverUrl = "ws://localhost:8080/ws";
        [SerializeField] private float heartbeatInterval = 30f;
        [SerializeField] private float reconnectDelay = 3f;
        [SerializeField] private int maxReconnectAttempts = 5;

        private WebSocketClient _webSocket;
        private ConnectionState _connectionState;
        private int _reconnectAttempts;
        private float _lastHeartbeatTime;
        private string _authToken;
        private string _playerId;

        public ConnectionState State => _connectionState;
        public bool IsConnected => _connectionState == ConnectionState.Connected;
        public string PlayerId => _playerId;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<GameMessage> OnMessageReceived;

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
        }

        private void Update()
        {
            if (IsConnected && Time.time - _lastHeartbeatTime >= heartbeatInterval)
            {
                SendHeartbeat();
                _lastHeartbeatTime = Time.time;
            }
        }

        public async Task<bool> ConnectAsync(string token = null)
        {
            if (_connectionState == ConnectionState.Connected)
                return true;

            _authToken = token;
            _connectionState = ConnectionState.Connecting;

            try
            {
                string url = serverUrl;
                if (!string.IsNullOrEmpty(token))
                {
                    url += $"?token={token}";
                }

                _webSocket = new WebSocketClient(url);
                _webSocket.OnOpen += HandleOpen;
                _webSocket.OnClose += HandleClose;
                _webSocket.OnError += HandleError;
                _webSocket.OnMessage += HandleMessage;

                await _webSocket.ConnectAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");
                _connectionState = ConnectionState.Disconnected;
                OnError?.Invoke(ex.Message);
                return false;
            }
        }

        public void Disconnect()
        {
            _reconnectAttempts = maxReconnectAttempts;
            _webSocket?.Close();
            _connectionState = ConnectionState.Disconnected;
        }

        public void SendMessage(GameMessage message)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Cannot send message: not connected");
                return;
            }

            string json = JsonUtility.ToJson(message);
            _webSocket?.Send(json);
        }

        public void SendAction(string action, Dictionary<string, object> data = null)
        {
            var message = new GameMessage
            {
                type = action,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            if (data != null)
            {
                message.data = JsonUtility.ToJson(data);
            }

            SendMessage(message);
        }

        private void SendHeartbeat()
        {
            SendAction("ping");
        }

        private void HandleOpen()
        {
            Debug.Log("WebSocket connected");
            _connectionState = ConnectionState.Connected;
            _reconnectAttempts = 0;
            _lastHeartbeatTime = Time.time;
            
            OnConnected?.Invoke();

            if (!string.IsNullOrEmpty(_authToken))
            {
                SendAction("auth", new Dictionary<string, object> { { "token", _authToken } });
            }
        }

        private void HandleClose()
        {
            Debug.Log("WebSocket disconnected");
            _connectionState = ConnectionState.Disconnected;
            OnDisconnected?.Invoke();

            if (_reconnectAttempts < maxReconnectAttempts)
            {
                StartCoroutine(ReconnectCoroutine());
            }
        }

        private void HandleError(string error)
        {
            Debug.LogError($"WebSocket error: {error}");
            OnError?.Invoke(error);
        }

        private void HandleMessage(string data)
        {
            try
            {
                var message = JsonUtility.FromJson<GameMessage>(data);
                ProcessMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse message: {ex.Message}");
            }
        }

        private void ProcessMessage(GameMessage message)
        {
            switch (message.type)
            {
                case "pong":
                    break;
                    
                case "auth_success":
                    _playerId = message.playerId;
                    Debug.Log($"Authenticated as {_playerId}");
                    break;
                    
                case "auth_failed":
                    Debug.LogError("Authentication failed");
                    Disconnect();
                    break;
                    
                default:
                    OnMessageReceived?.Invoke(message);
                    break;
            }
        }

        private System.Collections.IEnumerator ReconnectCoroutine()
        {
            _connectionState = ConnectionState.Reconnecting;
            _reconnectAttempts++;
            
            Debug.Log($"Reconnecting... attempt {_reconnectAttempts}/{maxReconnectAttempts}");
            
            yield return new WaitForSeconds(reconnectDelay * _reconnectAttempts);
            
            _ = ConnectAsync(_authToken);
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }

    [Serializable]
    public class GameMessage
    {
        public string type;
        public string roomId;
        public string playerId;
        public string data;
        public long timestamp;
    }

    public class WebSocketClient
    {
        private readonly string _url;
        private System.Net.WebSockets.ClientWebSocket _ws;
        private bool _isConnected;

        public event Action OnOpen;
        public event Action OnClose;
        public event Action<string> OnError;
        public event Action<string> OnMessage;

        public WebSocketClient(string url)
        {
            _url = url;
        }

        public async Task ConnectAsync()
        {
            _ws = new System.Net.WebSockets.ClientWebSocket();
            
            try
            {
                await _ws.ConnectAsync(new Uri(_url), System.Threading.CancellationToken.None);
                _isConnected = true;
                OnOpen?.Invoke();
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        public void Send(string message)
        {
            if (!_isConnected) return;

            var bytes = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(bytes);
            _ = _ws.SendAsync(segment, System.Net.WebSockets.WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }

        public void Close()
        {
            _isConnected = false;
            _ws?.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closing", System.Threading.CancellationToken.None);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            var segment = new ArraySegment<byte>(buffer);

            try
            {
                while (_isConnected && _ws.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var result = await _ws.ReceiveAsync(segment, System.Threading.CancellationToken.None);
                    
                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessage?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
            finally
            {
                _isConnected = false;
                OnClose?.Invoke();
            }
        }
    }
}
