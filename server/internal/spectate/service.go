package spectate

import (
	"sync"
	"time"
)

type Spectator struct {
	UserID    string    `json:"userId"`
	Name      string    `json:"name"`
	RoomID    string    `json:"roomId"`
	JoinedAt  time.Time `json:"joinedAt"`
}

type SpectatorSettings struct {
	AllowSpectators bool `json:"allowSpectators"`
	DelaySeconds    int  `json:"delaySeconds"` // Delay to prevent cheating
	MaxSpectators   int  `json:"maxSpectators"`
	ShowHoleCards   bool `json:"showHoleCards"` // Show all hole cards (for delayed view)
}

type DelayedGameState struct {
	State     map[string]interface{} `json:"state"`
	Timestamp time.Time              `json:"timestamp"`
}

type Service struct {
	spectators    map[string]map[string]*Spectator // roomID -> userID -> Spectator
	roomSettings  map[string]*SpectatorSettings
	delayedStates map[string][]*DelayedGameState // roomID -> delayed states
	defaultDelay  int
	mu            sync.RWMutex
}

func NewService(defaultDelay int) *Service {
	if defaultDelay <= 0 {
		defaultDelay = 30 // 30 seconds default delay
	}
	
	return &Service{
		spectators:    make(map[string]map[string]*Spectator),
		roomSettings:  make(map[string]*SpectatorSettings),
		delayedStates: make(map[string][]*DelayedGameState),
		defaultDelay:  defaultDelay,
	}
}

func (s *Service) SetRoomSettings(roomID string, settings *SpectatorSettings) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.roomSettings[roomID] = settings
}

func (s *Service) GetRoomSettings(roomID string) *SpectatorSettings {
	s.mu.RLock()
	defer s.mu.RUnlock()

	settings := s.roomSettings[roomID]
	if settings == nil {
		return &SpectatorSettings{
			AllowSpectators: true,
			DelaySeconds:    s.defaultDelay,
			MaxSpectators:   50,
			ShowHoleCards:   true,
		}
	}
	return settings
}

func (s *Service) JoinAsSpectator(roomID, userID, name string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	settings := s.roomSettings[roomID]
	if settings != nil && !settings.AllowSpectators {
		return &SpectatorError{Message: "spectating not allowed"}
	}

	if s.spectators[roomID] == nil {
		s.spectators[roomID] = make(map[string]*Spectator)
	}

	if settings != nil && len(s.spectators[roomID]) >= settings.MaxSpectators {
		return &SpectatorError{Message: "room is full"}
	}

	s.spectators[roomID][userID] = &Spectator{
		UserID:   userID,
		Name:     name,
		RoomID:   roomID,
		JoinedAt: time.Now(),
	}

	return nil
}

func (s *Service) LeaveSpectating(roomID, userID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.spectators[roomID] != nil {
		delete(s.spectators[roomID], userID)
		if len(s.spectators[roomID]) == 0 {
			delete(s.spectators, roomID)
		}
	}
}

func (s *Service) GetSpectators(roomID string) []*Spectator {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.spectators[roomID] == nil {
		return []*Spectator{}
	}

	result := make([]*Spectator, 0, len(s.spectators[roomID]))
	for _, spec := range s.spectators[roomID] {
		result = append(result, spec)
	}
	return result
}

func (s *Service) GetSpectatorCount(roomID string) int {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return len(s.spectators[roomID])
}

func (s *Service) IsSpectating(roomID, userID string) bool {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.spectators[roomID] == nil {
		return false
	}
	_, exists := s.spectators[roomID][userID]
	return exists
}

func (s *Service) RecordGameState(roomID string, state map[string]interface{}) {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.delayedStates[roomID] == nil {
		s.delayedStates[roomID] = make([]*DelayedGameState, 0)
	}

	s.delayedStates[roomID] = append(s.delayedStates[roomID], &DelayedGameState{
		State:     state,
		Timestamp: time.Now(),
	})

	// Keep only last 5 minutes of states
	cutoff := time.Now().Add(-5 * time.Minute)
	for len(s.delayedStates[roomID]) > 0 && s.delayedStates[roomID][0].Timestamp.Before(cutoff) {
		s.delayedStates[roomID] = s.delayedStates[roomID][1:]
	}
}

func (s *Service) GetDelayedGameState(roomID string) map[string]interface{} {
	s.mu.RLock()
	defer s.mu.RUnlock()

	settings := s.roomSettings[roomID]
	delay := s.defaultDelay
	if settings != nil {
		delay = settings.DelaySeconds
	}

	cutoff := time.Now().Add(-time.Duration(delay) * time.Second)

	states := s.delayedStates[roomID]
	if states == nil || len(states) == 0 {
		return nil
	}

	// Find the latest state before the cutoff
	var result *DelayedGameState
	for i := len(states) - 1; i >= 0; i-- {
		if states[i].Timestamp.Before(cutoff) {
			result = states[i]
			break
		}
	}

	if result == nil {
		return nil
	}

	return result.State
}

func (s *Service) BroadcastToSpectators(roomID string, broadcaster func(userID string)) {
	s.mu.RLock()
	spectators := s.spectators[roomID]
	s.mu.RUnlock()

	for userID := range spectators {
		broadcaster(userID)
	}
}

func (s *Service) CleanupRoom(roomID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	delete(s.spectators, roomID)
	delete(s.roomSettings, roomID)
	delete(s.delayedStates, roomID)
}

type SpectatorError struct {
	Message string
}

func (e *SpectatorError) Error() string {
	return e.Message
}
