package game

import (
	"fmt"
	"sync"
	"time"

	"github.com/google/uuid"
)

type Phase int

const (
	PhaseWaiting Phase = iota
	PhaseStarting
	PhasePreflop
	PhaseFlop
	PhaseTurn
	PhaseRiver
	PhaseShowdown
	PhaseFinished
)

func (p Phase) String() string {
	names := []string{"waiting", "starting", "preflop", "flop", "turn", "river", "showdown", "finished"}
	return names[p]
}

type PlayerState int

const (
	StateWaiting PlayerState = iota
	StateActive
	StateFolded
	StateAllIn
	StateSittingOut
)

type ActionType int

const (
	ActionNone ActionType = iota
	ActionFold
	ActionCheck
	ActionCall
	ActionRaise
	ActionAllIn
	ActionSmallBlind
	ActionBigBlind
)

func (a ActionType) String() string {
	names := []string{"none", "fold", "check", "call", "raise", "all_in", "small_blind", "big_blind"}
	return names[a]
}

type Player struct {
	ID             string      `json:"id"`
	Name           string      `json:"name"`
	Avatar         string      `json:"avatar"`
	SeatIndex      int         `json:"seatIndex"`
	Chips          int64       `json:"chips"`
	CurrentBet     int64       `json:"currentBet"`
	TotalBetInHand int64       `json:"totalBetInHand"`
	HoleCards      []Card      `json:"holeCards,omitempty"`
	State          PlayerState `json:"state"`
	LastAction     ActionType  `json:"lastAction"`
	IsDealer       bool        `json:"isDealer"`
	IsSmallBlind   bool        `json:"isSmallBlind"`
	IsBigBlind     bool        `json:"isBigBlind"`
	IsBot          bool        `json:"isBot"`
}

func NewPlayer(id, name string, chips int64) *Player {
	return &Player{
		ID:        id,
		Name:      name,
		Chips:     chips,
		State:     StateWaiting,
		HoleCards: make([]Card, 0, 2),
	}
}

func (p *Player) Reset() {
	p.CurrentBet = 0
	p.TotalBetInHand = 0
	p.HoleCards = make([]Card, 0, 2)
	p.State = StateWaiting
	p.LastAction = ActionNone
	p.IsDealer = false
	p.IsSmallBlind = false
	p.IsBigBlind = false
}

func (p *Player) PlaceBet(amount int64) int64 {
	if amount <= 0 {
		return 0
	}
	actual := min(amount, p.Chips)
	p.Chips -= actual
	p.CurrentBet += actual
	p.TotalBetInHand += actual
	if p.Chips == 0 {
		p.State = StateAllIn
	}
	return actual
}

type GameConfig struct {
	SmallBlind    int64 `json:"smallBlind"`
	BigBlind      int64 `json:"bigBlind"`
	Ante          int64 `json:"ante"`
	MaxPlayers    int   `json:"maxPlayers"`
	MinPlayers    int   `json:"minPlayers"`
	ActionTimeout int   `json:"actionTimeout"`
}

func DefaultConfig() GameConfig {
	return GameConfig{
		SmallBlind:    10,
		BigBlind:      20,
		Ante:          0,
		MaxPlayers:    9,
		MinPlayers:    2,
		ActionTimeout: 30,
	}
}

type Pot struct {
	Amount     int64    `json:"amount"`
	PlayerIDs  []string `json:"playerIds"`
	IsSidePot  bool     `json:"isSidePot"`
}

type Game struct {
	ID                string      `json:"id"`
	RoomID            string      `json:"roomId"`
	Config            GameConfig  `json:"config"`
	Phase             Phase       `json:"phase"`
	Players           []*Player   `json:"players"`
	Deck              *Deck       `json:"-"`
	CommunityCards    []Card      `json:"communityCards"`
	Pots              []Pot       `json:"pots"`
	DealerSeat        int         `json:"dealerSeat"`
	SmallBlindSeat    int         `json:"smallBlindSeat"`
	BigBlindSeat      int         `json:"bigBlindSeat"`
	CurrentPlayerSeat int         `json:"currentPlayerSeat"`
	CurrentBet        int64       `json:"currentBet"`
	MinRaise          int64       `json:"minRaise"`
	LastRaiseAmount   int64       `json:"lastRaiseAmount"`
	HandNumber        int         `json:"handNumber"`
	ActionDeadline    time.Time   `json:"actionDeadline"`
	
	mu sync.RWMutex
	
	OnPhaseChange  func(phase Phase)
	OnPlayerAction func(player *Player, action ActionType, amount int64)
	OnCardsDealt   func(phase Phase, cards []Card)
	OnHandComplete func(winners map[string]int64)
}

func NewGame(roomID string, config GameConfig) *Game {
	return &Game{
		ID:             uuid.New().String(),
		RoomID:         roomID,
		Config:         config,
		Phase:          PhaseWaiting,
		Players:        make([]*Player, 0, config.MaxPlayers),
		Deck:           NewDeck(),
		CommunityCards: make([]Card, 0, 5),
		Pots:           make([]Pot, 0),
	}
}

func (g *Game) AddPlayer(player *Player) error {
	g.mu.Lock()
	defer g.mu.Unlock()

	if len(g.Players) >= g.Config.MaxPlayers {
		return fmt.Errorf("room is full")
	}

	for _, p := range g.Players {
		if p.ID == player.ID {
			return fmt.Errorf("player already in game")
		}
	}

	seat := g.findEmptySeat()
	if seat < 0 {
		return fmt.Errorf("no empty seat")
	}

	player.SeatIndex = seat
	g.Players = append(g.Players, player)
	return nil
}

func (g *Game) RemovePlayer(playerID string) error {
	g.mu.Lock()
	defer g.mu.Unlock()

	for i, p := range g.Players {
		if p.ID == playerID {
			if g.Phase != PhaseWaiting && g.Phase != PhaseFinished {
				p.State = StateFolded
			} else {
				g.Players = append(g.Players[:i], g.Players[i+1:]...)
			}
			return nil
		}
	}

	return fmt.Errorf("player not found")
}

func (g *Game) findEmptySeat() int {
	occupied := make(map[int]bool)
	for _, p := range g.Players {
		occupied[p.SeatIndex] = true
	}
	for i := 0; i < g.Config.MaxPlayers; i++ {
		if !occupied[i] {
			return i
		}
	}
	return -1
}

func (g *Game) CanStartHand() bool {
	g.mu.RLock()
	defer g.mu.RUnlock()

	if g.Phase != PhaseWaiting && g.Phase != PhaseFinished {
		return false
	}

	activeCount := 0
	for _, p := range g.Players {
		if p.Chips > 0 && p.State != StateSittingOut {
			activeCount++
		}
	}

	return activeCount >= g.Config.MinPlayers
}

func (g *Game) StartHand() error {
	g.mu.Lock()
	defer g.mu.Unlock()

	if !g.canStartHandLocked() {
		return fmt.Errorf("cannot start hand")
	}

	g.HandNumber++
	g.Phase = PhaseStarting

	for _, p := range g.Players {
		p.Reset()
		if p.Chips > 0 && p.State != StateSittingOut {
			p.State = StateActive
		}
	}

	g.Pots = []Pot{{Amount: 0, PlayerIDs: make([]string, 0)}}
	g.Deck.Reset()
	g.Deck.Shuffle()
	g.CommunityCards = make([]Card, 0, 5)

	g.moveButton()
	g.postBlinds()
	g.dealHoleCards()

	g.Phase = PhasePreflop
	g.setNextPlayer(g.BigBlindSeat)

	if g.OnPhaseChange != nil {
		g.OnPhaseChange(g.Phase)
	}

	return nil
}

func (g *Game) canStartHandLocked() bool {
	if g.Phase != PhaseWaiting && g.Phase != PhaseFinished {
		return false
	}

	activeCount := 0
	for _, p := range g.Players {
		if p.Chips > 0 && p.State != StateSittingOut {
			activeCount++
		}
	}

	return activeCount >= g.Config.MinPlayers
}

func (g *Game) moveButton() {
	activePlayers := g.getActivePlayers()
	if len(activePlayers) < 2 {
		return
	}

	if g.HandNumber == 1 {
		g.DealerSeat = activePlayers[0].SeatIndex
	} else {
		currentIdx := -1
		for i, p := range activePlayers {
			if p.SeatIndex == g.DealerSeat {
				currentIdx = i
				break
			}
		}
		nextIdx := (currentIdx + 1) % len(activePlayers)
		g.DealerSeat = activePlayers[nextIdx].SeatIndex
	}

	for _, p := range g.Players {
		if p.SeatIndex == g.DealerSeat {
			p.IsDealer = true
			break
		}
	}

	if len(activePlayers) == 2 {
		g.SmallBlindSeat = g.DealerSeat
		for _, p := range activePlayers {
			if p.SeatIndex != g.DealerSeat {
				g.BigBlindSeat = p.SeatIndex
				break
			}
		}
	} else {
		sbIdx := g.getNextActiveIndex(g.DealerSeat)
		g.SmallBlindSeat = activePlayers[sbIdx].SeatIndex
		bbIdx := g.getNextActiveIndex(g.SmallBlindSeat)
		g.BigBlindSeat = activePlayers[bbIdx].SeatIndex
	}
}

func (g *Game) postBlinds() {
	for _, p := range g.Players {
		if p.SeatIndex == g.SmallBlindSeat {
			p.PlaceBet(g.Config.SmallBlind)
			p.IsSmallBlind = true
			p.LastAction = ActionSmallBlind
		} else if p.SeatIndex == g.BigBlindSeat {
			p.PlaceBet(g.Config.BigBlind)
			p.IsBigBlind = true
			p.LastAction = ActionBigBlind
		}
	}

	g.CurrentBet = g.Config.BigBlind
	g.MinRaise = g.Config.BigBlind
	g.LastRaiseAmount = g.Config.BigBlind

	if g.Config.Ante > 0 {
		for _, p := range g.getActivePlayers() {
			p.PlaceBet(g.Config.Ante)
		}
	}
}

func (g *Game) dealHoleCards() {
	activePlayers := g.getActivePlayers()
	
	for round := 0; round < 2; round++ {
		for _, p := range activePlayers {
			card, _ := g.Deck.Deal()
			p.HoleCards = append(p.HoleCards, card)
		}
	}

	if g.OnCardsDealt != nil {
		g.OnCardsDealt(PhasePreflop, nil)
	}
}

func (g *Game) ProcessAction(playerID string, action ActionType, amount int64) error {
	g.mu.Lock()
	defer g.mu.Unlock()

	player := g.getPlayerByID(playerID)
	if player == nil {
		return fmt.Errorf("player not found")
	}

	if player.SeatIndex != g.CurrentPlayerSeat {
		return fmt.Errorf("not your turn")
	}

	if player.State != StateActive {
		return fmt.Errorf("player cannot act")
	}

	var err error
	switch action {
	case ActionFold:
		err = g.processFold(player)
	case ActionCheck:
		err = g.processCheck(player)
	case ActionCall:
		err = g.processCall(player)
	case ActionRaise:
		err = g.processRaise(player, amount)
	case ActionAllIn:
		err = g.processAllIn(player)
	default:
		err = fmt.Errorf("invalid action")
	}

	if err != nil {
		return err
	}

	if g.OnPlayerAction != nil {
		g.OnPlayerAction(player, action, amount)
	}

	g.checkRoundComplete()
	return nil
}

func (g *Game) processFold(p *Player) error {
	p.State = StateFolded
	p.LastAction = ActionFold
	return nil
}

func (g *Game) processCheck(p *Player) error {
	if p.CurrentBet < g.CurrentBet {
		return fmt.Errorf("cannot check, must call or fold")
	}
	p.LastAction = ActionCheck
	return nil
}

func (g *Game) processCall(p *Player) error {
	toCall := g.CurrentBet - p.CurrentBet
	if toCall <= 0 {
		p.LastAction = ActionCheck
		return nil
	}

	p.PlaceBet(toCall)
	if p.Chips == 0 {
		p.LastAction = ActionAllIn
	} else {
		p.LastAction = ActionCall
	}
	return nil
}

func (g *Game) processRaise(p *Player, total int64) error {
	minTotal := g.CurrentBet + g.MinRaise
	if total < minTotal && total < p.Chips+p.CurrentBet {
		return fmt.Errorf("raise too small")
	}

	needed := total - p.CurrentBet
	p.PlaceBet(needed)

	raiseAmount := p.CurrentBet - g.CurrentBet
	g.LastRaiseAmount = raiseAmount
	g.MinRaise = raiseAmount
	g.CurrentBet = p.CurrentBet

	if p.Chips == 0 {
		p.LastAction = ActionAllIn
	} else {
		p.LastAction = ActionRaise
	}
	return nil
}

func (g *Game) processAllIn(p *Player) error {
	allIn := p.Chips
	p.PlaceBet(allIn)

	if p.CurrentBet > g.CurrentBet {
		raiseAmount := p.CurrentBet - g.CurrentBet
		if raiseAmount >= g.MinRaise {
			g.MinRaise = raiseAmount
			g.LastRaiseAmount = raiseAmount
		}
		g.CurrentBet = p.CurrentBet
	}

	p.LastAction = ActionAllIn
	return nil
}

func (g *Game) checkRoundComplete() {
	activePlayers := g.getActivePlayers()
	nonFolded := 0
	for _, p := range activePlayers {
		if p.State != StateFolded {
			nonFolded++
		}
	}

	if nonFolded <= 1 {
		g.endHand()
		return
	}

	canAct := make([]*Player, 0)
	for _, p := range activePlayers {
		if p.State == StateActive {
			canAct = append(canAct, p)
		}
	}

	allMatched := true
	for _, p := range activePlayers {
		if p.State == StateFolded {
			continue
		}
		if p.State == StateActive && p.CurrentBet < g.CurrentBet {
			allMatched = false
			break
		}
	}

	allActed := true
	for _, p := range canAct {
		if p.LastAction == ActionNone || p.LastAction == ActionSmallBlind || p.LastAction == ActionBigBlind {
			allActed = false
			break
		}
	}

	if allMatched && allActed {
		g.advanceToNextStreet()
	} else {
		g.setNextPlayer(g.CurrentPlayerSeat)
	}
}

func (g *Game) advanceToNextStreet() {
	g.collectBets()

	for _, p := range g.getActivePlayers() {
		p.CurrentBet = 0
		p.LastAction = ActionNone
	}

	g.CurrentBet = 0
	g.MinRaise = g.Config.BigBlind

	switch g.Phase {
	case PhasePreflop:
		g.dealFlop()
	case PhaseFlop:
		g.dealTurn()
	case PhaseTurn:
		g.dealRiver()
	case PhaseRiver:
		g.endHand()
		return
	}

	g.Phase++

	canAct := 0
	for _, p := range g.getActivePlayers() {
		if p.State == StateActive {
			canAct++
		}
	}

	if canAct <= 1 {
		g.runOutBoard()
	} else {
		g.setFirstPlayerAfterDealer()
		if g.OnPhaseChange != nil {
			g.OnPhaseChange(g.Phase)
		}
	}
}

func (g *Game) dealFlop() {
	g.Deck.Burn()
	cards, _ := g.Deck.DealN(3)
	g.CommunityCards = append(g.CommunityCards, cards...)
	if g.OnCardsDealt != nil {
		g.OnCardsDealt(PhaseFlop, cards)
	}
}

func (g *Game) dealTurn() {
	g.Deck.Burn()
	card, _ := g.Deck.Deal()
	g.CommunityCards = append(g.CommunityCards, card)
	if g.OnCardsDealt != nil {
		g.OnCardsDealt(PhaseTurn, []Card{card})
	}
}

func (g *Game) dealRiver() {
	g.Deck.Burn()
	card, _ := g.Deck.Deal()
	g.CommunityCards = append(g.CommunityCards, card)
	if g.OnCardsDealt != nil {
		g.OnCardsDealt(PhaseRiver, []Card{card})
	}
}

func (g *Game) runOutBoard() {
	for len(g.CommunityCards) < 5 {
		g.Deck.Burn()
		card, _ := g.Deck.Deal()
		g.CommunityCards = append(g.CommunityCards, card)
	}
	g.endHand()
}

func (g *Game) collectBets() {
	// Simplified pot collection
	var totalBets int64
	eligibleIDs := make([]string, 0)

	for _, p := range g.Players {
		if p.TotalBetInHand > 0 {
			totalBets += p.CurrentBet
		}
		if p.State == StateActive || p.State == StateAllIn {
			eligibleIDs = append(eligibleIDs, p.ID)
		}
	}

	if len(g.Pots) == 0 {
		g.Pots = []Pot{{Amount: 0, PlayerIDs: make([]string, 0)}}
	}

	g.Pots[0].Amount += totalBets
	g.Pots[0].PlayerIDs = eligibleIDs
}

func (g *Game) endHand() {
	g.Phase = PhaseShowdown
	g.collectBets()

	activePlayers := make([]*Player, 0)
	for _, p := range g.Players {
		if p.State == StateActive || p.State == StateAllIn {
			activePlayers = append(activePlayers, p)
		}
	}

	winners := make(map[string]int64)

	if len(activePlayers) == 1 {
		winners[activePlayers[0].ID] = g.getTotalPot()
	} else {
		winners = g.determineWinners(activePlayers)
	}

	for id, amount := range winners {
		for _, p := range g.Players {
			if p.ID == id {
				p.Chips += amount
				break
			}
		}
	}

	g.Phase = PhaseFinished

	if g.OnHandComplete != nil {
		g.OnHandComplete(winners)
	}
}

func (g *Game) determineWinners(players []*Player) map[string]int64 {
	winners := make(map[string]int64)
	
	if len(g.CommunityCards) < 5 {
		// Not enough cards, split pot
		pot := g.getTotalPot()
		share := pot / int64(len(players))
		for _, p := range players {
			winners[p.ID] = share
		}
		return winners
	}

	var bestHand HandRank
	var bestPlayers []*Player

	for _, p := range players {
		if len(p.HoleCards) < 2 {
			continue
		}
		hand := EvaluateHand(p.HoleCards, g.CommunityCards)
		cmp := hand.Compare(bestHand)
		if cmp > 0 {
			bestHand = hand
			bestPlayers = []*Player{p}
		} else if cmp == 0 {
			bestPlayers = append(bestPlayers, p)
		}
	}

	pot := g.getTotalPot()
	share := pot / int64(len(bestPlayers))
	remainder := pot % int64(len(bestPlayers))

	for i, p := range bestPlayers {
		amt := share
		if i == 0 {
			amt += remainder
		}
		winners[p.ID] = amt
	}

	return winners
}

func (g *Game) getTotalPot() int64 {
	var total int64
	for _, pot := range g.Pots {
		total += pot.Amount
	}
	return total
}

func (g *Game) getActivePlayers() []*Player {
	result := make([]*Player, 0)
	for _, p := range g.Players {
		if p.State == StateActive || p.State == StateAllIn {
			result = append(result, p)
		}
	}
	return result
}

func (g *Game) getPlayerByID(id string) *Player {
	for _, p := range g.Players {
		if p.ID == id {
			return p
		}
	}
	return nil
}

func (g *Game) getNextActiveIndex(currentSeat int) int {
	activePlayers := g.getActivePlayers()
	if len(activePlayers) == 0 {
		return -1
	}

	currentIdx := -1
	for i, p := range activePlayers {
		if p.SeatIndex == currentSeat {
			currentIdx = i
			break
		}
	}

	if currentIdx < 0 {
		return 0
	}

	return (currentIdx + 1) % len(activePlayers)
}

func (g *Game) setNextPlayer(currentSeat int) {
	canAct := make([]*Player, 0)
	for _, p := range g.getActivePlayers() {
		if p.State == StateActive {
			canAct = append(canAct, p)
		}
	}

	if len(canAct) == 0 {
		return
	}

	currentIdx := -1
	for i, p := range canAct {
		if p.SeatIndex == currentSeat {
			currentIdx = i
			break
		}
	}

	if currentIdx < 0 {
		currentIdx = 0
	} else {
		currentIdx = (currentIdx + 1) % len(canAct)
	}

	g.CurrentPlayerSeat = canAct[currentIdx].SeatIndex
	g.ActionDeadline = time.Now().Add(time.Duration(g.Config.ActionTimeout) * time.Second)
}

func (g *Game) setFirstPlayerAfterDealer() {
	canAct := make([]*Player, 0)
	for _, p := range g.getActivePlayers() {
		if p.State == StateActive {
			canAct = append(canAct, p)
		}
	}

	if len(canAct) == 0 {
		return
	}

	dealerIdx := -1
	for i, p := range canAct {
		if p.SeatIndex == g.DealerSeat {
			dealerIdx = i
			break
		}
	}

	if dealerIdx < 0 {
		dealerIdx = 0
	}

	firstIdx := (dealerIdx + 1) % len(canAct)
	g.CurrentPlayerSeat = canAct[firstIdx].SeatIndex
	g.ActionDeadline = time.Now().Add(time.Duration(g.Config.ActionTimeout) * time.Second)
}

func (g *Game) GetCurrentPlayer() *Player {
	g.mu.RLock()
	defer g.mu.RUnlock()

	for _, p := range g.Players {
		if p.SeatIndex == g.CurrentPlayerSeat {
			return p
		}
	}
	return nil
}

func (g *Game) GetCallAmount(playerID string) int64 {
	g.mu.RLock()
	defer g.mu.RUnlock()

	player := g.getPlayerByID(playerID)
	if player == nil {
		return 0
	}

	call := g.CurrentBet - player.CurrentBet
	if call < 0 {
		return 0
	}
	return min(call, player.Chips)
}

func (g *Game) GetRaiseLimits(playerID string) (minRaise, maxRaise int64) {
	g.mu.RLock()
	defer g.mu.RUnlock()

	player := g.getPlayerByID(playerID)
	if player == nil {
		return 0, 0
	}

	minRaise = g.CurrentBet + g.MinRaise
	maxRaise = player.Chips + player.CurrentBet

	if minRaise > maxRaise {
		minRaise = maxRaise
	}

	return minRaise, maxRaise
}

func min(a, b int64) int64 {
	if a < b {
		return a
	}
	return b
}
