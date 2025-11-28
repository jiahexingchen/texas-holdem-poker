package ws

import (
	"log"
	"net/http"

	"github.com/google/uuid"
	"github.com/gorilla/websocket"
	"texas-holdem-server/internal/room"
)

var upgrader = websocket.Upgrader{
	ReadBufferSize:  1024,
	WriteBufferSize: 1024,
	CheckOrigin: func(r *http.Request) bool {
		return true
	},
}

type Handler struct {
	hub         *Hub
	roomManager *room.Manager
}

func NewHandler(hub *Hub, roomManager *room.Manager) *Handler {
	return &Handler{
		hub:         hub,
		roomManager: roomManager,
	}
}

func (h *Handler) HandleWebSocket(w http.ResponseWriter, r *http.Request) {
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("WebSocket upgrade error: %v", err)
		return
	}

	clientID := uuid.New().String()
	client := NewClient(clientID, conn, h.hub)

	token := r.URL.Query().Get("token")
	if token != "" {
		client.PlayerID = token
	} else {
		client.PlayerID = "guest_" + clientID[:8]
	}
	client.Name = "Player_" + clientID[:6]

	h.hub.Register(client)

	go client.WritePump()
	go client.ReadPump(h.handleMessage)

	client.Send(NewMessage("connected", map[string]string{
		"clientId": clientID,
		"playerId": client.PlayerID,
	}))
}

func (h *Handler) handleMessage(client *Client, msg *Message) {
	switch msg.Type {
	case "ping":
		client.Send(NewMessage("pong", nil))

	case "auth":
		h.handleAuth(client, msg)

	case "create_room":
		h.handleCreateRoom(client, msg)

	case "join_room":
		h.handleJoinRoom(client, msg)

	case "leave_room":
		h.handleLeaveRoom(client, msg)

	case "quick_match":
		h.handleQuickMatch(client, msg)

	case "cancel_match":
		h.handleCancelMatch(client, msg)

	case "player_action":
		h.handlePlayerAction(client, msg)

	case "chat":
		h.handleChat(client, msg)

	case "sit_out":
		h.handleSitOut(client, msg)

	case "sit_in":
		h.handleSitIn(client, msg)

	case "buy_in":
		h.handleBuyIn(client, msg)

	default:
		log.Printf("Unknown message type: %s", msg.Type)
	}
}

func (h *Handler) handleAuth(client *Client, msg *Message) {
	var data struct {
		Token string `json:"token"`
	}
	if err := msg.ParseData(&data); err != nil {
		client.Send(NewMessage("auth_failed", map[string]string{"error": "invalid token"}))
		return
	}

	client.PlayerID = data.Token
	client.Send(NewMessage("auth_success", map[string]string{
		"playerId": client.PlayerID,
	}))
}

func (h *Handler) handleCreateRoom(client *Client, msg *Message) {
	var config room.RoomConfig
	if err := msg.ParseData(&config); err != nil {
		config = room.DefaultRoomConfig()
	}

	r, err := h.roomManager.CreateRoom(config)
	if err != nil {
		client.Send(NewMessage("error", map[string]string{"message": err.Error()}))
		return
	}

	err = h.roomManager.JoinRoom(r.ID, client.PlayerID, client.Name, 1000)
	if err != nil {
		client.Send(NewMessage("error", map[string]string{"message": err.Error()}))
		return
	}

	h.hub.JoinRoom(r.ID, client)

	client.Send(NewMessage("room_joined", r.ToInfo()))
}

func (h *Handler) handleJoinRoom(client *Client, msg *Message) {
	var data struct {
		RoomID   string `json:"roomId"`
		Password string `json:"password,omitempty"`
	}
	if err := msg.ParseData(&data); err != nil {
		client.Send(NewMessage("error", map[string]string{"message": "invalid request"}))
		return
	}

	err := h.roomManager.JoinRoom(data.RoomID, client.PlayerID, client.Name, 1000)
	if err != nil {
		client.Send(NewMessage("error", map[string]string{"message": err.Error()}))
		return
	}

	h.hub.JoinRoom(data.RoomID, client)

	r := h.roomManager.GetRoom(data.RoomID)
	if r != nil {
		client.Send(NewMessage("room_joined", r.ToInfo()))

		h.hub.SendToRoom(data.RoomID, NewMessage("player_joined", map[string]interface{}{
			"playerId": client.PlayerID,
			"name":     client.Name,
		}))
	}
}

func (h *Handler) handleLeaveRoom(client *Client, msg *Message) {
	if client.RoomID == "" {
		return
	}

	roomID := client.RoomID
	h.roomManager.LeaveRoom(roomID, client.PlayerID)
	h.hub.LeaveRoom(roomID, client)

	client.Send(NewMessage("room_left", nil))

	h.hub.SendToRoom(roomID, NewMessage("player_left", map[string]string{
		"playerId": client.PlayerID,
	}))
}

func (h *Handler) handleQuickMatch(client *Client, msg *Message) {
	var data struct {
		BlindLevel int `json:"blindLevel"`
	}
	msg.ParseData(&data)

	roomID, err := h.roomManager.QuickMatch(client.PlayerID, client.Name, data.BlindLevel)
	if err != nil {
		client.Send(NewMessage("error", map[string]string{"message": err.Error()}))
		return
	}

	h.hub.JoinRoom(roomID, client)

	r := h.roomManager.GetRoom(roomID)
	if r != nil {
		client.Send(NewMessage("room_joined", r.ToInfo()))
	}
}

func (h *Handler) handleCancelMatch(client *Client, msg *Message) {
	h.roomManager.CancelMatch(client.PlayerID)
	client.Send(NewMessage("match_cancelled", nil))
}

func (h *Handler) handlePlayerAction(client *Client, msg *Message) {
	if client.RoomID == "" {
		client.Send(NewMessage("error", map[string]string{"message": "not in a room"}))
		return
	}

	var data struct {
		Action string `json:"action"`
		Amount int64  `json:"amount"`
	}
	if err := msg.ParseData(&data); err != nil {
		client.Send(NewMessage("error", map[string]string{"message": "invalid action"}))
		return
	}

	err := h.roomManager.ProcessAction(client.RoomID, client.PlayerID, data.Action, data.Amount)
	if err != nil {
		client.Send(NewMessage("error", map[string]string{"message": err.Error()}))
		return
	}

	r := h.roomManager.GetRoom(client.RoomID)
	if r != nil {
		h.hub.SendToRoom(client.RoomID, NewMessage("player_action", map[string]interface{}{
			"playerId": client.PlayerID,
			"action":   data.Action,
			"amount":   data.Amount,
		}))

		h.hub.SendToRoom(client.RoomID, NewMessage("game_state", r.GetGameState()))
	}
}

func (h *Handler) handleChat(client *Client, msg *Message) {
	if client.RoomID == "" {
		return
	}

	var data struct {
		Message string `json:"message"`
	}
	if err := msg.ParseData(&data); err != nil {
		return
	}

	h.hub.SendToRoom(client.RoomID, NewMessage("chat", map[string]interface{}{
		"playerId":   client.PlayerID,
		"playerName": client.Name,
		"message":    data.Message,
	}))
}

func (h *Handler) handleSitOut(client *Client, msg *Message) {
	if client.RoomID == "" {
		return
	}

	h.roomManager.SitOut(client.RoomID, client.PlayerID)
	h.hub.SendToRoom(client.RoomID, NewMessage("player_sit_out", map[string]string{
		"playerId": client.PlayerID,
	}))
}

func (h *Handler) handleSitIn(client *Client, msg *Message) {
	if client.RoomID == "" {
		return
	}

	h.roomManager.SitIn(client.RoomID, client.PlayerID)
	h.hub.SendToRoom(client.RoomID, NewMessage("player_sit_in", map[string]string{
		"playerId": client.PlayerID,
	}))
}

func (h *Handler) handleBuyIn(client *Client, msg *Message) {
	if client.RoomID == "" {
		return
	}

	var data struct {
		Amount int64 `json:"amount"`
	}
	if err := msg.ParseData(&data); err != nil {
		return
	}

	h.roomManager.BuyIn(client.RoomID, client.PlayerID, data.Amount)
}
