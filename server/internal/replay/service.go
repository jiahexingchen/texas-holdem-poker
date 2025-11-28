package replay

import (
	"encoding/json"
	"sync"
	"time"

	"texas-holdem-server/internal/game"
)

type ActionRecord struct {
	PlayerID   string          `json:"playerId"`
	PlayerName string          `json:"playerName"`
	Action     game.ActionType `json:"action"`
	Amount     int64           `json:"amount"`
	Timestamp  int64           `json:"timestamp"`
}

type PlayerSnapshot struct {
	PlayerID  string      `json:"playerId"`
	Name      string      `json:"name"`
	SeatIndex int         `json:"seatIndex"`
	Chips     int64       `json:"chips"`
	HoleCards []game.Card `json:"holeCards,omitempty"`
	IsDealer  bool        `json:"isDealer"`
}

type PhaseSnapshot struct {
	Phase          game.Phase     `json:"phase"`
	CommunityCards []game.Card    `json:"communityCards"`
	Pot            int64          `json:"pot"`
	Actions        []ActionRecord `json:"actions"`
}

type HandHistory struct {
	ID             string           `json:"id"`
	RoomID         string           `json:"roomId"`
	HandNumber     int              `json:"handNumber"`
	StartTime      time.Time        `json:"startTime"`
	EndTime        time.Time        `json:"endTime"`
	SmallBlind     int64            `json:"smallBlind"`
	BigBlind       int64            `json:"bigBlind"`
	Players        []PlayerSnapshot `json:"players"`
	Phases         []PhaseSnapshot  `json:"phases"`
	CommunityCards []game.Card      `json:"communityCards"`
	Winners        []WinnerInfo     `json:"winners"`
	FinalPot       int64            `json:"finalPot"`
}

type WinnerInfo struct {
	PlayerID string        `json:"playerId"`
	Amount   int64         `json:"amount"`
	HandType string        `json:"handType"`
	Cards    []game.Card   `json:"cards"`
}

type Service struct {
	histories    map[string][]*HandHistory // roomID -> histories
	userHistories map[string][]string       // userID -> handIDs
	allHistories map[string]*HandHistory   // handID -> history
	maxHistories int
	mu           sync.RWMutex
}

func NewService(maxHistories int) *Service {
	if maxHistories <= 0 {
		maxHistories = 100
	}
	return &Service{
		histories:     make(map[string][]*HandHistory),
		userHistories: make(map[string][]string),
		allHistories:  make(map[string]*HandHistory),
		maxHistories:  maxHistories,
	}
}

func (s *Service) StartRecording(roomID string, handNumber int, players []*game.Player, smallBlind, bigBlind int64) *HandHistory {
	s.mu.Lock()
	defer s.mu.Unlock()

	handID := generateHandID(roomID, handNumber)
	
	playerSnapshots := make([]PlayerSnapshot, 0, len(players))
	for _, p := range players {
		snapshot := PlayerSnapshot{
			PlayerID:  p.ID,
			Name:      p.Name,
			SeatIndex: p.SeatIndex,
			Chips:     p.Chips,
			IsDealer:  p.IsDealer,
		}
		if len(p.HoleCards) > 0 {
			snapshot.HoleCards = make([]game.Card, len(p.HoleCards))
			copy(snapshot.HoleCards, p.HoleCards)
		}
		playerSnapshots = append(playerSnapshots, snapshot)
	}

	history := &HandHistory{
		ID:         handID,
		RoomID:     roomID,
		HandNumber: handNumber,
		StartTime:  time.Now(),
		SmallBlind: smallBlind,
		BigBlind:   bigBlind,
		Players:    playerSnapshots,
		Phases:     make([]PhaseSnapshot, 0),
	}

	s.allHistories[handID] = history
	
	return history
}

func (s *Service) RecordPhase(handID string, phase game.Phase, communityCards []game.Card, pot int64) {
	s.mu.Lock()
	defer s.mu.Unlock()

	history := s.allHistories[handID]
	if history == nil {
		return
	}

	cards := make([]game.Card, len(communityCards))
	copy(cards, communityCards)

	phaseSnapshot := PhaseSnapshot{
		Phase:          phase,
		CommunityCards: cards,
		Pot:            pot,
		Actions:        make([]ActionRecord, 0),
	}

	history.Phases = append(history.Phases, phaseSnapshot)
}

func (s *Service) RecordAction(handID string, playerID, playerName string, action game.ActionType, amount int64) {
	s.mu.Lock()
	defer s.mu.Unlock()

	history := s.allHistories[handID]
	if history == nil || len(history.Phases) == 0 {
		return
	}

	record := ActionRecord{
		PlayerID:   playerID,
		PlayerName: playerName,
		Action:     action,
		Amount:     amount,
		Timestamp:  time.Now().UnixMilli(),
	}

	lastPhase := &history.Phases[len(history.Phases)-1]
	lastPhase.Actions = append(lastPhase.Actions, record)
}

func (s *Service) FinishRecording(handID string, communityCards []game.Card, winners map[string]int64, handTypes map[string]string, winningCards map[string][]game.Card, totalPot int64) {
	s.mu.Lock()
	defer s.mu.Unlock()

	history := s.allHistories[handID]
	if history == nil {
		return
	}

	history.EndTime = time.Now()
	history.CommunityCards = make([]game.Card, len(communityCards))
	copy(history.CommunityCards, communityCards)
	history.FinalPot = totalPot

	history.Winners = make([]WinnerInfo, 0, len(winners))
	for playerID, amount := range winners {
		info := WinnerInfo{
			PlayerID: playerID,
			Amount:   amount,
			HandType: handTypes[playerID],
		}
		if cards, ok := winningCards[playerID]; ok {
			info.Cards = make([]game.Card, len(cards))
			copy(info.Cards, cards)
		}
		history.Winners = append(history.Winners, info)
	}

	// Store in room history
	if s.histories[history.RoomID] == nil {
		s.histories[history.RoomID] = make([]*HandHistory, 0)
	}
	s.histories[history.RoomID] = append(s.histories[history.RoomID], history)

	// Trim if too many
	if len(s.histories[history.RoomID]) > s.maxHistories {
		s.histories[history.RoomID] = s.histories[history.RoomID][1:]
	}

	// Store for each player
	for _, player := range history.Players {
		if s.userHistories[player.PlayerID] == nil {
			s.userHistories[player.PlayerID] = make([]string, 0)
		}
		s.userHistories[player.PlayerID] = append(s.userHistories[player.PlayerID], handID)
		
		// Trim if too many
		if len(s.userHistories[player.PlayerID]) > s.maxHistories {
			s.userHistories[player.PlayerID] = s.userHistories[player.PlayerID][1:]
		}
	}
}

func (s *Service) GetHandHistory(handID string) *HandHistory {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.allHistories[handID]
}

func (s *Service) GetRoomHistories(roomID string, limit int) []*HandHistory {
	s.mu.RLock()
	defer s.mu.RUnlock()

	histories := s.histories[roomID]
	if histories == nil {
		return []*HandHistory{}
	}

	if limit <= 0 || limit > len(histories) {
		limit = len(histories)
	}

	start := len(histories) - limit
	result := make([]*HandHistory, limit)
	copy(result, histories[start:])

	return result
}

func (s *Service) GetUserHistories(userID string, limit int) []*HandHistory {
	s.mu.RLock()
	defer s.mu.RUnlock()

	handIDs := s.userHistories[userID]
	if handIDs == nil {
		return []*HandHistory{}
	}

	if limit <= 0 || limit > len(handIDs) {
		limit = len(handIDs)
	}

	start := len(handIDs) - limit
	result := make([]*HandHistory, 0, limit)

	for i := start; i < len(handIDs); i++ {
		if history := s.allHistories[handIDs[i]]; history != nil {
			result = append(result, history)
		}
	}

	return result
}

func (s *Service) ToJSON(history *HandHistory) ([]byte, error) {
	return json.Marshal(history)
}

func (s *Service) FromJSON(data []byte) (*HandHistory, error) {
	var history HandHistory
	if err := json.Unmarshal(data, &history); err != nil {
		return nil, err
	}
	return &history, nil
}

func generateHandID(roomID string, handNumber int) string {
	return roomID + "_" + time.Now().Format("20060102150405")
}

// ReplayState for client-side replay
type ReplayState struct {
	History      *HandHistory `json:"history"`
	CurrentPhase int          `json:"currentPhase"`
	CurrentAction int         `json:"currentAction"`
	IsPlaying    bool         `json:"isPlaying"`
	Speed        float64      `json:"speed"` // 1.0 = normal, 2.0 = 2x speed
}

func NewReplayState(history *HandHistory) *ReplayState {
	return &ReplayState{
		History:       history,
		CurrentPhase:  0,
		CurrentAction: 0,
		IsPlaying:     false,
		Speed:         1.0,
	}
}

func (r *ReplayState) NextAction() (ActionRecord, bool) {
	if r.CurrentPhase >= len(r.History.Phases) {
		return ActionRecord{}, false
	}

	phase := r.History.Phases[r.CurrentPhase]
	if r.CurrentAction >= len(phase.Actions) {
		r.CurrentPhase++
		r.CurrentAction = 0
		return r.NextAction()
	}

	action := phase.Actions[r.CurrentAction]
	r.CurrentAction++
	return action, true
}

func (r *ReplayState) Reset() {
	r.CurrentPhase = 0
	r.CurrentAction = 0
	r.IsPlaying = false
}
