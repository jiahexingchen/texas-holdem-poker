package room

import (
	"fmt"
	"sync"
	"time"

	"texas-holdem-server/internal/game"
)

type MatchRequest struct {
	PlayerID   string
	PlayerName string
	BlindLevel int
	Chips      int64
	RequestAt  time.Time
}

type Manager struct {
	rooms        map[string]*Room
	matchQueue   []MatchRequest
	mu           sync.RWMutex
	onRoomEvent  func(roomID, eventType string, data interface{})
}

func NewManager(hub interface{}) *Manager {
	m := &Manager{
		rooms:      make(map[string]*Room),
		matchQueue: make([]MatchRequest, 0),
	}

	go m.cleanupRoutine()
	go m.matchmakingRoutine()

	return m
}

func (m *Manager) SetEventHandler(handler func(roomID, eventType string, data interface{})) {
	m.onRoomEvent = handler
}

func (m *Manager) CreateRoom(config RoomConfig) (*Room, error) {
	m.mu.Lock()
	defer m.mu.Unlock()

	room := NewRoom(config)
	m.rooms[room.ID] = room

	room.SetEventHandler(func(eventType string, data interface{}) {
		if m.onRoomEvent != nil {
			m.onRoomEvent(room.ID, eventType, data)
		}
	})

	return room, nil
}

func (m *Manager) GetRoom(roomID string) *Room {
	m.mu.RLock()
	defer m.mu.RUnlock()
	return m.rooms[roomID]
}

func (m *Manager) DeleteRoom(roomID string) {
	m.mu.Lock()
	defer m.mu.Unlock()
	delete(m.rooms, roomID)
}

func (m *Manager) JoinRoom(roomID, playerID, name string, chips int64) error {
	m.mu.RLock()
	room, exists := m.rooms[roomID]
	m.mu.RUnlock()

	if !exists {
		return fmt.Errorf("room not found")
	}

	return room.AddPlayer(playerID, name, chips)
}

func (m *Manager) LeaveRoom(roomID, playerID string) error {
	m.mu.RLock()
	room, exists := m.rooms[roomID]
	m.mu.RUnlock()

	if !exists {
		return fmt.Errorf("room not found")
	}

	err := room.RemovePlayer(playerID)

	if room.IsEmpty() {
		m.DeleteRoom(roomID)
	}

	return err
}

func (m *Manager) ProcessAction(roomID, playerID, action string, amount int64) error {
	m.mu.RLock()
	room, exists := m.rooms[roomID]
	m.mu.RUnlock()

	if !exists {
		return fmt.Errorf("room not found")
	}

	return room.ProcessAction(playerID, action, amount)
}

func (m *Manager) SitOut(roomID, playerID string) {
	if room := m.GetRoom(roomID); room != nil {
		room.SitOut(playerID)
	}
}

func (m *Manager) SitIn(roomID, playerID string) {
	if room := m.GetRoom(roomID); room != nil {
		room.SitIn(playerID)
	}
}

func (m *Manager) BuyIn(roomID, playerID string, amount int64) {
	if room := m.GetRoom(roomID); room != nil {
		room.BuyIn(playerID, amount)
	}
}

func (m *Manager) QuickMatch(playerID, name string, blindLevel int) (string, error) {
	m.mu.Lock()
	defer m.mu.Unlock()

	blinds := getBlindsByLevel(blindLevel)

	for _, room := range m.rooms {
		if room.Config.IsPrivate {
			continue
		}
		if room.Config.SmallBlind == blinds.small && room.Config.BigBlind == blinds.big {
			if room.GetPlayerCount() < room.Config.MaxPlayers {
				if err := room.AddPlayer(playerID, name, 1000); err == nil {
					return room.ID, nil
				}
			}
		}
	}

	config := RoomConfig{
		SmallBlind: blinds.small,
		BigBlind:   blinds.big,
		MaxPlayers: 9,
		MinPlayers: 2,
		IsPrivate:  false,
		AutoStart:  true,
	}

	room := NewRoom(config)
	m.rooms[room.ID] = room

	room.SetEventHandler(func(eventType string, data interface{}) {
		if m.onRoomEvent != nil {
			m.onRoomEvent(room.ID, eventType, data)
		}
	})

	if err := room.AddPlayer(playerID, name, 1000); err != nil {
		delete(m.rooms, room.ID)
		return "", err
	}

	go m.fillWithAI(room.ID, 3)

	return room.ID, nil
}

func (m *Manager) CancelMatch(playerID string) {
	m.mu.Lock()
	defer m.mu.Unlock()

	for i, req := range m.matchQueue {
		if req.PlayerID == playerID {
			m.matchQueue = append(m.matchQueue[:i], m.matchQueue[i+1:]...)
			break
		}
	}
}

func (m *Manager) GetPublicRooms() []*Room {
	m.mu.RLock()
	defer m.mu.RUnlock()

	rooms := make([]*Room, 0)
	for _, room := range m.rooms {
		if !room.Config.IsPrivate {
			rooms = append(rooms, room)
		}
	}
	return rooms
}

func (m *Manager) GetRoomCount() int {
	m.mu.RLock()
	defer m.mu.RUnlock()
	return len(m.rooms)
}

func (m *Manager) fillWithAI(roomID string, count int) {
	time.Sleep(5 * time.Second)

	room := m.GetRoom(roomID)
	if room == nil {
		return
	}

	aiNames := []string{"Bot_Alice", "Bot_Bob", "Bot_Charlie", "Bot_Diana", "Bot_Eve"}
	added := 0

	for i := 0; i < len(aiNames) && added < count; i++ {
		if room.GetPlayerCount() >= room.Config.MaxPlayers {
			break
		}

		player := game.NewPlayer(
			fmt.Sprintf("ai_%d", time.Now().UnixNano()),
			aiNames[i],
			1000,
		)
		player.IsBot = true

		if err := room.Game.AddPlayer(player); err == nil {
			added++
		}
	}

	if room.Game.CanStartHand() {
		room.Game.StartHand()
	}
}

type blindConfig struct {
	small int64
	big   int64
}

func getBlindsByLevel(level int) blindConfig {
	blinds := []blindConfig{
		{5, 10},
		{10, 20},
		{25, 50},
		{50, 100},
		{100, 200},
		{250, 500},
	}

	if level < 0 || level >= len(blinds) {
		return blinds[0]
	}
	return blinds[level]
}

func (m *Manager) cleanupRoutine() {
	ticker := time.NewTicker(5 * time.Minute)
	defer ticker.Stop()

	for range ticker.C {
		m.mu.Lock()
		for id, room := range m.rooms {
			if room.IsEmpty() && time.Since(room.CreatedAt) > 10*time.Minute {
				delete(m.rooms, id)
			}
		}
		m.mu.Unlock()
	}
}

func (m *Manager) matchmakingRoutine() {
	ticker := time.NewTicker(1 * time.Second)
	defer ticker.Stop()

	for range ticker.C {
		m.processMatchQueue()
	}
}

func (m *Manager) processMatchQueue() {
	m.mu.Lock()
	defer m.mu.Unlock()

	if len(m.matchQueue) < 2 {
		return
	}

	groups := make(map[int][]MatchRequest)
	for _, req := range m.matchQueue {
		groups[req.BlindLevel] = append(groups[req.BlindLevel], req)
	}

	for level, requests := range groups {
		if len(requests) < 2 {
			continue
		}

		blinds := getBlindsByLevel(level)
		config := RoomConfig{
			SmallBlind: blinds.small,
			BigBlind:   blinds.big,
			MaxPlayers: 9,
			MinPlayers: 2,
			IsPrivate:  false,
			AutoStart:  true,
		}

		room := NewRoom(config)
		m.rooms[room.ID] = room

		maxPlayers := min(len(requests), config.MaxPlayers)
		matched := requests[:maxPlayers]

		for _, req := range matched {
			room.AddPlayer(req.PlayerID, req.PlayerName, req.Chips)
		}

		for _, req := range matched {
			m.removeFromQueue(req.PlayerID)
		}
	}
}

func (m *Manager) removeFromQueue(playerID string) {
	for i, req := range m.matchQueue {
		if req.PlayerID == playerID {
			m.matchQueue = append(m.matchQueue[:i], m.matchQueue[i+1:]...)
			return
		}
	}
}

func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}
