package game

import (
	"testing"
)

func TestNewDeck(t *testing.T) {
	deck := NewDeck()
	
	if deck.RemainingCards() != 52 {
		t.Errorf("Expected 52 cards, got %d", deck.RemainingCards())
	}
}

func TestDeckShuffle(t *testing.T) {
	deck1 := NewDeck()
	deck2 := NewDeck()
	
	deck1.Shuffle()
	deck2.Shuffle()
	
	// Two shuffled decks should be different (with very high probability)
	same := true
	for i := 0; i < 10; i++ {
		card1, _ := deck1.Deal()
		card2, _ := deck2.Deal()
		if card1.Suit != card2.Suit || card1.Rank != card2.Rank {
			same = false
			break
		}
	}
	
	if same {
		t.Error("Two shuffled decks should not be identical")
	}
}

func TestDeckDeal(t *testing.T) {
	deck := NewDeck()
	deck.Shuffle()
	
	// Deal all 52 cards
	for i := 0; i < 52; i++ {
		_, err := deck.Deal()
		if err != nil {
			t.Errorf("Failed to deal card %d: %v", i, err)
		}
	}
	
	// Should fail on 53rd card
	_, err := deck.Deal()
	if err == nil {
		t.Error("Should fail when deck is empty")
	}
}

func TestCardToString(t *testing.T) {
	tests := []struct {
		card     Card
		expected string
	}{
		{Card{Suit: Hearts, Rank: Ace}, "Ah"},
		{Card{Suit: Spades, Rank: King}, "Ks"},
		{Card{Suit: Diamonds, Rank: Ten}, "Td"},
		{Card{Suit: Clubs, Rank: Two}, "2c"},
	}
	
	for _, tt := range tests {
		result := tt.card.String()
		if result != tt.expected {
			t.Errorf("Card.String() = %s, expected %s", result, tt.expected)
		}
	}
}

func TestEvaluateHandRoyalFlush(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: Ace},
		{Suit: Hearts, Rank: King},
	}
	communityCards := []Card{
		{Suit: Hearts, Rank: Queen},
		{Suit: Hearts, Rank: Jack},
		{Suit: Hearts, Rank: Ten},
		{Suit: Clubs, Rank: Two},
		{Suit: Diamonds, Rank: Three},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != RoyalFlush {
		t.Errorf("Expected RoyalFlush, got %v", result.Type)
	}
}

func TestEvaluateHandStraightFlush(t *testing.T) {
	holeCards := []Card{
		{Suit: Spades, Rank: Nine},
		{Suit: Spades, Rank: Eight},
	}
	communityCards := []Card{
		{Suit: Spades, Rank: Seven},
		{Suit: Spades, Rank: Six},
		{Suit: Spades, Rank: Five},
		{Suit: Hearts, Rank: Ace},
		{Suit: Diamonds, Rank: King},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != StraightFlush {
		t.Errorf("Expected StraightFlush, got %v", result.Type)
	}
}

func TestEvaluateHandFourOfAKind(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: Ace},
		{Suit: Spades, Rank: Ace},
	}
	communityCards := []Card{
		{Suit: Diamonds, Rank: Ace},
		{Suit: Clubs, Rank: Ace},
		{Suit: Hearts, Rank: King},
		{Suit: Spades, Rank: Queen},
		{Suit: Diamonds, Rank: Jack},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != FourOfAKind {
		t.Errorf("Expected FourOfAKind, got %v", result.Type)
	}
}

func TestEvaluateHandFullHouse(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: King},
		{Suit: Spades, Rank: King},
	}
	communityCards := []Card{
		{Suit: Diamonds, Rank: King},
		{Suit: Clubs, Rank: Queen},
		{Suit: Hearts, Rank: Queen},
		{Suit: Spades, Rank: Two},
		{Suit: Diamonds, Rank: Three},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != FullHouse {
		t.Errorf("Expected FullHouse, got %v", result.Type)
	}
}

func TestEvaluateHandFlush(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: Ace},
		{Suit: Hearts, Rank: Ten},
	}
	communityCards := []Card{
		{Suit: Hearts, Rank: Eight},
		{Suit: Hearts, Rank: Five},
		{Suit: Hearts, Rank: Two},
		{Suit: Spades, Rank: King},
		{Suit: Diamonds, Rank: Queen},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != Flush {
		t.Errorf("Expected Flush, got %v", result.Type)
	}
}

func TestEvaluateHandStraight(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: Nine},
		{Suit: Spades, Rank: Eight},
	}
	communityCards := []Card{
		{Suit: Diamonds, Rank: Seven},
		{Suit: Clubs, Rank: Six},
		{Suit: Hearts, Rank: Five},
		{Suit: Spades, Rank: Ace},
		{Suit: Diamonds, Rank: King},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != Straight {
		t.Errorf("Expected Straight, got %v", result.Type)
	}
}

func TestEvaluateHandWheelStraight(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: Ace},
		{Suit: Spades, Rank: Two},
	}
	communityCards := []Card{
		{Suit: Diamonds, Rank: Three},
		{Suit: Clubs, Rank: Four},
		{Suit: Hearts, Rank: Five},
		{Suit: Spades, Rank: King},
		{Suit: Diamonds, Rank: Queen},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != Straight {
		t.Errorf("Expected Straight (wheel), got %v", result.Type)
	}
	
	if len(result.Kickers) == 0 || result.Kickers[0] != Five {
		t.Error("Wheel straight should have 5 as high card")
	}
}

func TestEvaluateHandThreeOfAKind(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: Jack},
		{Suit: Spades, Rank: Jack},
	}
	communityCards := []Card{
		{Suit: Diamonds, Rank: Jack},
		{Suit: Clubs, Rank: King},
		{Suit: Hearts, Rank: Queen},
		{Suit: Spades, Rank: Two},
		{Suit: Diamonds, Rank: Three},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != ThreeOfAKind {
		t.Errorf("Expected ThreeOfAKind, got %v", result.Type)
	}
}

func TestEvaluateHandTwoPair(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: King},
		{Suit: Spades, Rank: Queen},
	}
	communityCards := []Card{
		{Suit: Diamonds, Rank: King},
		{Suit: Clubs, Rank: Queen},
		{Suit: Hearts, Rank: Two},
		{Suit: Spades, Rank: Five},
		{Suit: Diamonds, Rank: Seven},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != TwoPair {
		t.Errorf("Expected TwoPair, got %v", result.Type)
	}
}

func TestEvaluateHandOnePair(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: Ace},
		{Suit: Spades, Rank: Ace},
	}
	communityCards := []Card{
		{Suit: Diamonds, Rank: King},
		{Suit: Clubs, Rank: Queen},
		{Suit: Hearts, Rank: Jack},
		{Suit: Spades, Rank: Two},
		{Suit: Diamonds, Rank: Three},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != OnePair {
		t.Errorf("Expected OnePair, got %v", result.Type)
	}
}

func TestEvaluateHandHighCard(t *testing.T) {
	holeCards := []Card{
		{Suit: Hearts, Rank: Ace},
		{Suit: Spades, Rank: King},
	}
	communityCards := []Card{
		{Suit: Diamonds, Rank: Ten},
		{Suit: Clubs, Rank: Eight},
		{Suit: Hearts, Rank: Six},
		{Suit: Spades, Rank: Four},
		{Suit: Diamonds, Rank: Two},
	}
	
	result := EvaluateHand(holeCards, communityCards)
	
	if result.Type != HighCard {
		t.Errorf("Expected HighCard, got %v", result.Type)
	}
}

func TestHandRankCompare(t *testing.T) {
	flush := HandRank{Type: Flush, Kickers: []Rank{Ace, King, Queen, Jack, Ten}}
	straight := HandRank{Type: Straight, Kickers: []Rank{King}}
	
	if flush.Compare(straight) <= 0 {
		t.Error("Flush should beat Straight")
	}
	
	if straight.Compare(flush) >= 0 {
		t.Error("Straight should lose to Flush")
	}
}

func TestHandRankCompareKickers(t *testing.T) {
	pair1 := HandRank{Type: OnePair, Kickers: []Rank{Ace, King, Queen, Jack}}
	pair2 := HandRank{Type: OnePair, Kickers: []Rank{Ace, King, Queen, Ten}}
	
	if pair1.Compare(pair2) <= 0 {
		t.Error("Pair with Jack kicker should beat pair with Ten kicker")
	}
}

func TestNewGame(t *testing.T) {
	config := DefaultConfig()
	game := NewGame("test-room", config)
	
	if game.Phase != PhaseWaiting {
		t.Errorf("New game should be in Waiting phase, got %v", game.Phase)
	}
	
	if len(game.Players) != 0 {
		t.Error("New game should have no players")
	}
}

func TestGameAddPlayer(t *testing.T) {
	config := DefaultConfig()
	game := NewGame("test-room", config)
	
	player := NewPlayer("player1", "Test Player", 1000)
	err := game.AddPlayer(player)
	
	if err != nil {
		t.Errorf("Failed to add player: %v", err)
	}
	
	if len(game.Players) != 1 {
		t.Error("Game should have 1 player")
	}
}

func TestGameAddDuplicatePlayer(t *testing.T) {
	config := DefaultConfig()
	game := NewGame("test-room", config)
	
	player := NewPlayer("player1", "Test Player", 1000)
	game.AddPlayer(player)
	
	err := game.AddPlayer(player)
	if err == nil {
		t.Error("Should fail when adding duplicate player")
	}
}

func TestGameCanStartHand(t *testing.T) {
	config := DefaultConfig()
	game := NewGame("test-room", config)
	
	if game.CanStartHand() {
		t.Error("Should not be able to start with no players")
	}
	
	game.AddPlayer(NewPlayer("p1", "Player 1", 1000))
	if game.CanStartHand() {
		t.Error("Should not be able to start with 1 player")
	}
	
	game.AddPlayer(NewPlayer("p2", "Player 2", 1000))
	if !game.CanStartHand() {
		t.Error("Should be able to start with 2 players")
	}
}

func TestGameStartHand(t *testing.T) {
	config := DefaultConfig()
	game := NewGame("test-room", config)
	
	game.AddPlayer(NewPlayer("p1", "Player 1", 1000))
	game.AddPlayer(NewPlayer("p2", "Player 2", 1000))
	
	err := game.StartHand()
	if err != nil {
		t.Errorf("Failed to start hand: %v", err)
	}
	
	if game.Phase != PhasePreflop {
		t.Errorf("Game should be in Preflop phase, got %v", game.Phase)
	}
	
	// Check hole cards dealt
	for _, p := range game.Players {
		if len(p.HoleCards) != 2 {
			t.Errorf("Player should have 2 hole cards, got %d", len(p.HoleCards))
		}
	}
	
	// Check blinds posted
	if game.CurrentBet != config.BigBlind {
		t.Errorf("Current bet should be %d, got %d", config.BigBlind, game.CurrentBet)
	}
}

func TestGameProcessAction(t *testing.T) {
	config := DefaultConfig()
	game := NewGame("test-room", config)
	
	p1 := NewPlayer("p1", "Player 1", 1000)
	p2 := NewPlayer("p2", "Player 2", 1000)
	game.AddPlayer(p1)
	game.AddPlayer(p2)
	
	game.StartHand()
	
	currentPlayer := game.GetCurrentPlayer()
	if currentPlayer == nil {
		t.Fatal("Should have a current player")
	}
	
	// Process a call action
	err := game.ProcessAction(currentPlayer.ID, ActionCall, 0)
	if err != nil {
		t.Errorf("Failed to process action: %v", err)
	}
}
