package ai

import (
	"math/rand"
	"time"

	"texas-holdem-server/internal/game"
)

type Difficulty int

const (
	Easy Difficulty = iota
	Medium
	Hard
	Expert
)

type Decision struct {
	Action  game.ActionType
	Amount  int64
	Reason  string
}

type Bot struct {
	PlayerID   string
	Difficulty Difficulty
	rng        *rand.Rand
}

func NewBot(playerID string, difficulty Difficulty) *Bot {
	return &Bot{
		PlayerID:   playerID,
		Difficulty: difficulty,
		rng:        rand.New(rand.NewSource(time.Now().UnixNano())),
	}
}

func (b *Bot) MakeDecision(g *game.Game, player *game.Player) Decision {
	switch b.Difficulty {
	case Easy:
		return b.makeEasyDecision(g, player)
	case Medium:
		return b.makeMediumDecision(g, player)
	case Hard:
		return b.makeHardDecision(g, player)
	case Expert:
		return b.makeExpertDecision(g, player)
	default:
		return b.makeEasyDecision(g, player)
	}
}

func (b *Bot) makeEasyDecision(g *game.Game, player *game.Player) Decision {
	roll := b.rng.Float64()

	callAmount := g.CurrentBet - player.CurrentBet
	if callAmount <= 0 {
		if roll < 0.7 {
			return Decision{Action: game.ActionCheck, Reason: "random check"}
		}
		minRaise, _ := g.GetRaiseLimits(player.ID)
		return Decision{Action: game.ActionRaise, Amount: minRaise, Reason: "random raise"}
	}

	if roll < 0.2 {
		return Decision{Action: game.ActionFold, Reason: "random fold"}
	} else if roll < 0.8 {
		return Decision{Action: game.ActionCall, Reason: "random call"}
	} else {
		minRaise, _ := g.GetRaiseLimits(player.ID)
		return Decision{Action: game.ActionRaise, Amount: minRaise, Reason: "random raise"}
	}
}

func (b *Bot) makeMediumDecision(g *game.Game, player *game.Player) Decision {
	handStrength := b.evaluateHandStrength(player.HoleCards, g.CommunityCards)
	callAmount := g.CurrentBet - player.CurrentBet
	potOdds := b.calculatePotOdds(g, callAmount)

	if callAmount <= 0 {
		if handStrength > 0.7 {
			minRaise, maxRaise := g.GetRaiseLimits(player.ID)
			raiseAmount := minRaise + int64(float64(maxRaise-minRaise)*handStrength*0.5)
			return Decision{Action: game.ActionRaise, Amount: raiseAmount, Reason: "value raise"}
		}
		return Decision{Action: game.ActionCheck, Reason: "check back"}
	}

	if handStrength > potOdds+0.1 {
		if handStrength > 0.8 && b.rng.Float64() < 0.5 {
			minRaise, maxRaise := g.GetRaiseLimits(player.ID)
			raiseAmount := minRaise + int64(float64(maxRaise-minRaise)*0.4)
			return Decision{Action: game.ActionRaise, Amount: raiseAmount, Reason: "value raise"}
		}
		return Decision{Action: game.ActionCall, Reason: "good odds call"}
	}

	if handStrength > potOdds-0.1 && b.rng.Float64() < 0.3 {
		return Decision{Action: game.ActionCall, Reason: "borderline call"}
	}

	return Decision{Action: game.ActionFold, Reason: "bad odds fold"}
}

func (b *Bot) makeHardDecision(g *game.Game, player *game.Player) Decision {
	handStrength := b.evaluateHandStrength(player.HoleCards, g.CommunityCards)
	positionValue := b.getPositionValue(g, player)
	adjustedStrength := handStrength * (0.8 + positionValue*0.4)

	callAmount := g.CurrentBet - player.CurrentBet
	potOdds := b.calculatePotOdds(g, callAmount)

	shouldBluff := b.rng.Float64() < 0.15*positionValue

	if callAmount <= 0 {
		if adjustedStrength > 0.65 || shouldBluff {
			minRaise, maxRaise := g.GetRaiseLimits(player.ID)
			betSizing := 0.4
			if adjustedStrength > 0.8 {
				betSizing = 0.7
			}
			raiseAmount := minRaise + int64(float64(maxRaise-minRaise)*betSizing)
			reason := "value bet"
			if shouldBluff {
				reason = "positional bluff"
			}
			return Decision{Action: game.ActionRaise, Amount: raiseAmount, Reason: reason}
		}
		return Decision{Action: game.ActionCheck, Reason: "check"}
	}

	effectiveOdds := potOdds * (1 + positionValue*0.2)
	if adjustedStrength > effectiveOdds+0.15 {
		if adjustedStrength > 0.75 {
			minRaise, maxRaise := g.GetRaiseLimits(player.ID)
			raiseAmount := minRaise + int64(float64(maxRaise-minRaise)*adjustedStrength*0.5)
			return Decision{Action: game.ActionRaise, Amount: raiseAmount, Reason: "value raise"}
		}
		return Decision{Action: game.ActionCall, Reason: "profitable call"}
	}

	if adjustedStrength > effectiveOdds && b.rng.Float64() < 0.4 {
		return Decision{Action: game.ActionCall, Reason: "marginal call"}
	}

	return Decision{Action: game.ActionFold, Reason: "fold to aggression"}
}

func (b *Bot) makeExpertDecision(g *game.Game, player *game.Player) Decision {
	decision := b.makeHardDecision(g, player)

	if decision.Action == game.ActionCall && b.rng.Float64() < 0.15 {
		minRaise, _ := g.GetRaiseLimits(player.ID)
		return Decision{Action: game.ActionRaise, Amount: minRaise, Reason: "balanced raise"}
	}

	if decision.Action == game.ActionRaise && b.rng.Float64() < 0.1 {
		callAmount := g.CurrentBet - player.CurrentBet
		if callAmount > 0 {
			return Decision{Action: game.ActionCall, Reason: "trap call"}
		}
	}

	return decision
}

func (b *Bot) evaluateHandStrength(holeCards, communityCards []game.Card) float64 {
	if len(holeCards) < 2 {
		return 0.2
	}

	if len(communityCards) == 0 {
		return b.evaluatePreflopStrength(holeCards)
	}

	return b.evaluatePostflopStrength(holeCards, communityCards)
}

func (b *Bot) evaluatePreflopStrength(holeCards []game.Card) float64 {
	rank1 := int(holeCards[0].Rank)
	rank2 := int(holeCards[1].Rank)
	isPair := rank1 == rank2
	isSuited := holeCards[0].Suit == holeCards[1].Suit

	highRank := rank1
	lowRank := rank2
	if rank2 > rank1 {
		highRank = rank2
		lowRank = rank1
	}

	var strength float64

	if isPair {
		strength = 0.5 + float64(highRank-2)/24.0
		if highRank >= 10 {
			strength += 0.1
		}
		if highRank == int(game.Ace) {
			strength = 0.95
		}
	} else {
		strength = (float64(highRank-2) + float64(lowRank-2)*0.5) / 30.0

		if highRank == int(game.Ace) && lowRank >= 10 {
			strength = 0.75
		}
		if highRank == int(game.Ace) && lowRank == int(game.King) {
			strength = 0.85
		}

		if isSuited {
			strength += 0.08
		}

		gap := highRank - lowRank
		if gap == 1 {
			strength += 0.05
		}
	}

	if strength < 0 {
		strength = 0
	}
	if strength > 1 {
		strength = 1
	}

	return strength
}

func (b *Bot) evaluatePostflopStrength(holeCards, communityCards []game.Card) float64 {
	handRank := game.EvaluateHand(holeCards, communityCards)

	var baseStrength float64
	switch handRank.Type {
	case game.RoyalFlush, game.StraightFlush:
		baseStrength = 0.98
	case game.FourOfAKind:
		baseStrength = 0.95
	case game.FullHouse:
		baseStrength = 0.90
	case game.Flush:
		baseStrength = 0.80
	case game.Straight:
		baseStrength = 0.70
	case game.ThreeOfAKind:
		baseStrength = 0.60
	case game.TwoPair:
		baseStrength = 0.50
	case game.OnePair:
		baseStrength = 0.35
	default:
		baseStrength = 0.15
	}

	if len(handRank.Kickers) > 0 {
		baseStrength += float64(handRank.Kickers[0]-2) / 100.0
	}

	if baseStrength > 1 {
		baseStrength = 1
	}

	return baseStrength
}

func (b *Bot) getPositionValue(g *game.Game, player *game.Player) float64 {
	activePlayers := 0
	playerIndex := 0
	dealerIndex := 0

	for i, p := range g.Players {
		if p.State == game.StateActive || p.State == game.StateAllIn {
			if p.ID == player.ID {
				playerIndex = activePlayers
			}
			if p.IsDealer {
				dealerIndex = activePlayers
			}
			activePlayers++
		}
	}

	if activePlayers <= 1 {
		return 0.5
	}

	relativePosition := (playerIndex - dealerIndex + activePlayers) % activePlayers
	return float64(relativePosition) / float64(activePlayers-1)
}

func (b *Bot) calculatePotOdds(g *game.Game, callAmount int64) float64 {
	if callAmount <= 0 {
		return 0
	}

	var totalPot int64
	for _, pot := range g.Pots {
		totalPot += pot.Amount
	}
	totalPot += callAmount

	return float64(callAmount) / float64(totalPot)
}

type BotManager struct {
	bots map[string]*Bot
}

func NewBotManager() *BotManager {
	return &BotManager{
		bots: make(map[string]*Bot),
	}
}

func (bm *BotManager) CreateBot(playerID string, difficulty Difficulty) *Bot {
	bot := NewBot(playerID, difficulty)
	bm.bots[playerID] = bot
	return bot
}

func (bm *BotManager) GetBot(playerID string) *Bot {
	return bm.bots[playerID]
}

func (bm *BotManager) RemoveBot(playerID string) {
	delete(bm.bots, playerID)
}

func (bm *BotManager) ProcessBotTurn(g *game.Game) {
	currentPlayer := g.GetCurrentPlayer()
	if currentPlayer == nil || !currentPlayer.IsBot {
		return
	}

	bot := bm.GetBot(currentPlayer.ID)
	if bot == nil {
		return
	}

	time.Sleep(time.Duration(500+rand.Intn(1500)) * time.Millisecond)

	decision := bot.MakeDecision(g, currentPlayer)
	g.ProcessAction(currentPlayer.ID, decision.Action, decision.Amount)
}
