package reconnect

import (
	"sync"
	"time"
)

type SessionState struct {
	UserID       string                 `json:"userId"`
	RoomID       string                 `json:"roomId"`
	SeatIndex    int                    `json:"seatIndex"`
	Chips        int64                  `json:"chips"`
	CurrentBet   int64                  `json:"currentBet"`
	HoleCards    []int                  `json:"holeCards"` // Card indices
	State        string                 `json:"state"`     // active, folded, allIn
	LastAction   string                 `json:"lastAction"`
	GameState    map[string]interface{} `json:"gameState"`
	DisconnectAt time.Time              `json:"disconnectAt"`
	ExpiresAt    time.Time              `json:"expiresAt"`
}

type Service struct {
	sessions       map[string]*SessionState // userID -> session
	roomSessions   map[string][]string      // roomID -> userIDs
	sessionTimeout time.Duration
	mu             sync.RWMutex
}

func NewService(timeout time.Duration) *Service {
	if timeout <= 0 {
		timeout = 5 * time.Minute
	}
	
	s := &Service{
		sessions:       make(map[string]*SessionState),
		roomSessions:   make(map[string][]string),
		sessionTimeout: timeout,
	}

	go s.cleanupLoop()
	
	return s
}

func (s *Service) SaveSession(userID string, state *SessionState) {
	s.mu.Lock()
	defer s.mu.Unlock()

	state.UserID = userID
	state.DisconnectAt = time.Now()
	state.ExpiresAt = time.Now().Add(s.sessionTimeout)

	s.sessions[userID] = state

	// Track by room
	if state.RoomID != "" {
		if s.roomSessions[state.RoomID] == nil {
			s.roomSessions[state.RoomID] = make([]string, 0)
		}
		
		// Check if already tracked
		found := false
		for _, id := range s.roomSessions[state.RoomID] {
			if id == userID {
				found = true
				break
			}
		}
		if !found {
			s.roomSessions[state.RoomID] = append(s.roomSessions[state.RoomID], userID)
		}
	}
}

func (s *Service) GetSession(userID string) *SessionState {
	s.mu.RLock()
	defer s.mu.RUnlock()

	session := s.sessions[userID]
	if session == nil {
		return nil
	}

	// Check if expired
	if time.Now().After(session.ExpiresAt) {
		return nil
	}

	return session
}

func (s *Service) RemoveSession(userID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	session := s.sessions[userID]
	if session != nil && session.RoomID != "" {
		// Remove from room tracking
		if userIDs := s.roomSessions[session.RoomID]; userIDs != nil {
			for i, id := range userIDs {
				if id == userID {
					s.roomSessions[session.RoomID] = append(userIDs[:i], userIDs[i+1:]...)
					break
				}
			}
		}
	}

	delete(s.sessions, userID)
}

func (s *Service) GetRoomDisconnectedPlayers(roomID string) []*SessionState {
	s.mu.RLock()
	defer s.mu.RUnlock()

	userIDs := s.roomSessions[roomID]
	if userIDs == nil {
		return []*SessionState{}
	}

	result := make([]*SessionState, 0)
	now := time.Now()

	for _, userID := range userIDs {
		session := s.sessions[userID]
		if session != nil && !now.After(session.ExpiresAt) {
			result = append(result, session)
		}
	}

	return result
}

func (s *Service) HasPendingSession(userID string) bool {
	s.mu.RLock()
	defer s.mu.RUnlock()

	session := s.sessions[userID]
	if session == nil {
		return false
	}

	return !time.Now().After(session.ExpiresAt)
}

func (s *Service) ExtendSession(userID string, duration time.Duration) {
	s.mu.Lock()
	defer s.mu.Unlock()

	session := s.sessions[userID]
	if session != nil {
		session.ExpiresAt = time.Now().Add(duration)
	}
}

func (s *Service) GetRemainingTime(userID string) time.Duration {
	s.mu.RLock()
	defer s.mu.RUnlock()

	session := s.sessions[userID]
	if session == nil {
		return 0
	}

	remaining := time.Until(session.ExpiresAt)
	if remaining < 0 {
		return 0
	}
	return remaining
}

func (s *Service) cleanupLoop() {
	ticker := time.NewTicker(30 * time.Second)
	defer ticker.Stop()

	for range ticker.C {
		s.cleanup()
	}
}

func (s *Service) cleanup() {
	s.mu.Lock()
	defer s.mu.Unlock()

	now := time.Now()
	expiredUsers := make([]string, 0)

	for userID, session := range s.sessions {
		if now.After(session.ExpiresAt) {
			expiredUsers = append(expiredUsers, userID)
		}
	}

	for _, userID := range expiredUsers {
		session := s.sessions[userID]
		if session != nil && session.RoomID != "" {
			// Remove from room tracking
			if userIDs := s.roomSessions[session.RoomID]; userIDs != nil {
				for i, id := range userIDs {
					if id == userID {
						s.roomSessions[session.RoomID] = append(userIDs[:i], userIDs[i+1:]...)
						break
					}
				}
			}
		}
		delete(s.sessions, userID)
	}

	// Clean up empty room entries
	for roomID, userIDs := range s.roomSessions {
		if len(userIDs) == 0 {
			delete(s.roomSessions, roomID)
		}
	}
}

// ReconnectHandler handles reconnection logic
type ReconnectHandler struct {
	service *Service
}

func NewReconnectHandler(service *Service) *ReconnectHandler {
	return &ReconnectHandler{service: service}
}

func (h *ReconnectHandler) OnDisconnect(userID, roomID string, gameState map[string]interface{}) {
	session := &SessionState{
		UserID:    userID,
		RoomID:    roomID,
		GameState: gameState,
	}
	h.service.SaveSession(userID, session)
}

func (h *ReconnectHandler) OnReconnect(userID string) (*SessionState, bool) {
	session := h.service.GetSession(userID)
	if session == nil {
		return nil, false
	}

	// Remove the session after successful reconnect
	h.service.RemoveSession(userID)
	return session, true
}

func (h *ReconnectHandler) ShouldWaitForReconnect(userID string) bool {
	return h.service.HasPendingSession(userID)
}
