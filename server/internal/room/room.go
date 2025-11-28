package room

import (
	"encoding/json"
	"fmt"
	"sync"
	"time"

	"github.com/google/uuid"
	"texas-holdem-server/internal/game"
)

type RoomConfig struct {
	SmallBlind int64  `json:"smallBlind"`
	BigBlind   int64  `json:"bigBlind"`
	MaxPlayers int    `json:"maxPlayers"`
	MinPlayers int    `json:"minPlayers"`
	IsPrivate  bool   `json:"isPrivate"`
	Password   string `json:"password,omitempty"`
	AutoStart  bool   `json:"autoStart"`
}

func DefaultRoomConfig() RoomConfig {
	return RoomConfig{
		SmallBlind: 10,
		BigBlind:   20,
		MaxPlayers: 9,
		MinPlayers: 2,
		IsPrivate:  false,
		AutoStart:  true,
	}
}

type RoomInfo struct {
	ID             string       `json:"id"`
	Name           string       `json:"name"`
	SmallBlind     int64        `json:"smallBlind"`
	BigBlind       int64        `json:"bigBlind"`
	MaxPlayers     int          `json:"maxPlayers"`
	CurrentPlayers int          `json:"currentPlayers"`
	IsPrivate      bool         `json:"isPrivate"`
	Players        []PlayerInfo `json:"players"`
}

type PlayerInfo struct {
	PlayerID  string `json:"playerId"`
	Name      string `json:"name"`
	SeatIndex int    `json:"seatIndex"`
	Chips     int64  `json:"chips"`
	IsBot     bool   `json:"isBot"`
}

type Room struct {
	ID        string
	Name      string
	Config    RoomConfig
	Game      *game.Game
	CreatedAt time.Time
	mu        sync.RWMutex

	onGameEvent func(eventType string, data interface{})
}

func NewRoom(config RoomConfig) *Room {
	id := uuid.New().String()[:8]
	
	gameConfig := game.GameConfig{
		SmallBlind:    config.SmallBlind,
		BigBlind:      config.BigBlind,
		MaxPlayers:    config.MaxPlayers,
		MinPlayers:    config.MinPlayers,
		ActionTimeout: 30,
	}

	r := &Room{
		ID:        id,
		Name:      fmt.Sprintf("Room_%s", id),
		Config:    config,
		Game:      game.NewGame(id, gameConfig),
		CreatedAt: time.Now(),
	}

	r.setupGameCallbacks()
	return r
}

func (r *Room) setupGameCallbacks() {
	r.Game.OnPhaseChange = func(phase game.Phase) {
		if r.onGameEvent != nil {
			r.onGameEvent("phase_change", map[string]interface{}{
				"phase": phase.String(),
			})
		}
	}

	r.Game.OnPlayerAction = func(player *game.Player, action game.ActionType, amount int64) {
		if r.onGameEvent != nil {
			r.onGameEvent("player_action", map[string]interface{}{
				"playerId": player.ID,
				"action":   action.String(),
				"amount":   amount,
			})
		}
	}

	r.Game.OnCardsDealt = func(phase game.Phase, cards []game.Card) {
		if r.onGameEvent != nil {
			r.onGameEvent("cards_dealt", map[string]interface{}{
				"phase": phase.String(),
				"cards": cards,
			})
		}
	}

	r.Game.OnHandComplete = func(winners map[string]int64) {
		if r.onGameEvent != nil {
			r.onGameEvent("hand_complete", map[string]interface{}{
				"winners": winners,
			})
		}

		if r.Config.AutoStart && r.Game.CanStartHand() {
			go func() {
				time.Sleep(3 * time.Second)
				r.Game.StartHand()
			}()
		}
	}
}

func (r *Room) SetEventHandler(handler func(eventType string, data interface{})) {
	r.onGameEvent = handler
}

func (r *Room) AddPlayer(playerID, name string, chips int64) error {
	r.mu.Lock()
	defer r.mu.Unlock()

	player := game.NewPlayer(playerID, name, chips)
	if err := r.Game.AddPlayer(player); err != nil {
		return err
	}

	if r.Config.AutoStart && r.Game.CanStartHand() {
		go func() {
			time.Sleep(2 * time.Second)
			r.mu.Lock()
			defer r.mu.Unlock()
			if r.Game.CanStartHand() {
				r.Game.StartHand()
			}
		}()
	}

	return nil
}

func (r *Room) RemovePlayer(playerID string) error {
	r.mu.Lock()
	defer r.mu.Unlock()
	return r.Game.RemovePlayer(playerID)
}

func (r *Room) ProcessAction(playerID, action string, amount int64) error {
	r.mu.Lock()
	defer r.mu.Unlock()

	actionType := parseAction(action)
	return r.Game.ProcessAction(playerID, actionType, amount)
}

func parseAction(action string) game.ActionType {
	switch action {
	case "fold":
		return game.ActionFold
	case "check":
		return game.ActionCheck
	case "call":
		return game.ActionCall
	case "raise":
		return game.ActionRaise
	case "all_in", "allin":
		return game.ActionAllIn
	default:
		return game.ActionNone
	}
}

func (r *Room) SitOut(playerID string) {
	r.mu.Lock()
	defer r.mu.Unlock()

	for _, p := range r.Game.Players {
		if p.ID == playerID {
			p.State = game.StateSittingOut
			break
		}
	}
}

func (r *Room) SitIn(playerID string) {
	r.mu.Lock()
	defer r.mu.Unlock()

	for _, p := range r.Game.Players {
		if p.ID == playerID && p.State == game.StateSittingOut {
			p.State = game.StateWaiting
			break
		}
	}
}

func (r *Room) BuyIn(playerID string, amount int64) {
	r.mu.Lock()
	defer r.mu.Unlock()

	for _, p := range r.Game.Players {
		if p.ID == playerID {
			p.Chips += amount
			break
		}
	}
}

func (r *Room) GetPlayerCount() int {
	r.mu.RLock()
	defer r.mu.RUnlock()
	return len(r.Game.Players)
}

func (r *Room) IsEmpty() bool {
	return r.GetPlayerCount() == 0
}

func (r *Room) ToInfo() RoomInfo {
	r.mu.RLock()
	defer r.mu.RUnlock()

	players := make([]PlayerInfo, 0, len(r.Game.Players))
	for _, p := range r.Game.Players {
		players = append(players, PlayerInfo{
			PlayerID:  p.ID,
			Name:      p.Name,
			SeatIndex: p.SeatIndex,
			Chips:     p.Chips,
			IsBot:     p.IsBot,
		})
	}

	return RoomInfo{
		ID:             r.ID,
		Name:           r.Name,
		SmallBlind:     r.Config.SmallBlind,
		BigBlind:       r.Config.BigBlind,
		MaxPlayers:     r.Config.MaxPlayers,
		CurrentPlayers: len(r.Game.Players),
		IsPrivate:      r.Config.IsPrivate,
		Players:        players,
	}
}

func (r *Room) ToJSON() string {
	info := r.ToInfo()
	data, _ := json.Marshal(info)
	return string(data)
}

func (r *Room) GetGameState() map[string]interface{} {
	r.mu.RLock()
	defer r.mu.RUnlock()

	players := make([]map[string]interface{}, 0)
	for _, p := range r.Game.Players {
		playerData := map[string]interface{}{
			"playerId":   p.ID,
			"name":       p.Name,
			"seatIndex":  p.SeatIndex,
			"chips":      p.Chips,
			"currentBet": p.CurrentBet,
			"state":      p.State,
			"lastAction": p.LastAction.String(),
			"isDealer":   p.IsDealer,
		}
		players = append(players, playerData)
	}

	var totalPot int64
	for _, pot := range r.Game.Pots {
		totalPot += pot.Amount
	}

	return map[string]interface{}{
		"phase":             r.Game.Phase.String(),
		"dealerSeat":        r.Game.DealerSeat,
		"currentPlayerSeat": r.Game.CurrentPlayerSeat,
		"currentBet":        r.Game.CurrentBet,
		"minRaise":          r.Game.MinRaise,
		"pot":               totalPot,
		"communityCards":    r.Game.CommunityCards,
		"players":           players,
	}
}
