package ws

import (
	"encoding/json"
	"log"
	"sync"
)

type Hub struct {
	clients    map[string]*Client
	rooms      map[string]map[string]*Client
	register   chan *Client
	unregister chan *Client
	broadcast  chan *Message
	mu         sync.RWMutex
}

func NewHub() *Hub {
	return &Hub{
		clients:    make(map[string]*Client),
		rooms:      make(map[string]map[string]*Client),
		register:   make(chan *Client),
		unregister: make(chan *Client),
		broadcast:  make(chan *Message, 256),
	}
}

func (h *Hub) Run() {
	for {
		select {
		case client := <-h.register:
			h.mu.Lock()
			h.clients[client.ID] = client
			h.mu.Unlock()
			log.Printf("Client connected: %s", client.ID)

		case client := <-h.unregister:
			h.mu.Lock()
			if _, ok := h.clients[client.ID]; ok {
				delete(h.clients, client.ID)
				close(client.send)
				
				for roomID, roomClients := range h.rooms {
					delete(roomClients, client.ID)
					if len(roomClients) == 0 {
						delete(h.rooms, roomID)
					}
				}
			}
			h.mu.Unlock()
			log.Printf("Client disconnected: %s", client.ID)

		case message := <-h.broadcast:
			h.broadcastMessage(message)
		}
	}
}

func (h *Hub) broadcastMessage(msg *Message) {
	data, err := json.Marshal(msg)
	if err != nil {
		log.Printf("Error marshaling message: %v", err)
		return
	}

	h.mu.RLock()
	defer h.mu.RUnlock()

	if msg.RoomID != "" {
		if roomClients, ok := h.rooms[msg.RoomID]; ok {
			for _, client := range roomClients {
				select {
				case client.send <- data:
				default:
					close(client.send)
					delete(h.clients, client.ID)
				}
			}
		}
	} else {
		for _, client := range h.clients {
			select {
			case client.send <- data:
			default:
				close(client.send)
				delete(h.clients, client.ID)
			}
		}
	}
}

func (h *Hub) Register(client *Client) {
	h.register <- client
}

func (h *Hub) Unregister(client *Client) {
	h.unregister <- client
}

func (h *Hub) Broadcast(msg *Message) {
	h.broadcast <- msg
}

func (h *Hub) SendToClient(clientID string, msg *Message) error {
	h.mu.RLock()
	client, ok := h.clients[clientID]
	h.mu.RUnlock()

	if !ok {
		return nil
	}

	data, err := json.Marshal(msg)
	if err != nil {
		return err
	}

	select {
	case client.send <- data:
	default:
		return nil
	}

	return nil
}

func (h *Hub) SendToRoom(roomID string, msg *Message) {
	msg.RoomID = roomID
	h.broadcast <- msg
}

func (h *Hub) JoinRoom(roomID string, client *Client) {
	h.mu.Lock()
	defer h.mu.Unlock()

	if _, ok := h.rooms[roomID]; !ok {
		h.rooms[roomID] = make(map[string]*Client)
	}
	h.rooms[roomID][client.ID] = client
	client.RoomID = roomID
}

func (h *Hub) LeaveRoom(roomID string, client *Client) {
	h.mu.Lock()
	defer h.mu.Unlock()

	if roomClients, ok := h.rooms[roomID]; ok {
		delete(roomClients, client.ID)
		if len(roomClients) == 0 {
			delete(h.rooms, roomID)
		}
	}
	client.RoomID = ""
}

func (h *Hub) GetClient(clientID string) *Client {
	h.mu.RLock()
	defer h.mu.RUnlock()
	return h.clients[clientID]
}

func (h *Hub) GetRoomClients(roomID string) []*Client {
	h.mu.RLock()
	defer h.mu.RUnlock()

	clients := make([]*Client, 0)
	if roomClients, ok := h.rooms[roomID]; ok {
		for _, client := range roomClients {
			clients = append(clients, client)
		}
	}
	return clients
}

func (h *Hub) GetClientCount() int {
	h.mu.RLock()
	defer h.mu.RUnlock()
	return len(h.clients)
}

func (h *Hub) GetRoomCount() int {
	h.mu.RLock()
	defer h.mu.RUnlock()
	return len(h.rooms)
}
