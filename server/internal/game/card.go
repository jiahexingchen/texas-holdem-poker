package game

import (
	"fmt"
	"math/rand"
	"sort"
	"time"
)

type Suit int
type Rank int

const (
	Hearts Suit = iota
	Diamonds
	Clubs
	Spades
)

const (
	Two Rank = iota + 2
	Three
	Four
	Five
	Six
	Seven
	Eight
	Nine
	Ten
	Jack
	Queen
	King
	Ace
)

type Card struct {
	Suit Suit `json:"suit"`
	Rank Rank `json:"rank"`
}

func NewCard(suit Suit, rank Rank) Card {
	return Card{Suit: suit, Rank: rank}
}

func CardFromIndex(index int) Card {
	return Card{
		Suit: Suit(index / 13),
		Rank: Rank(index%13 + 2),
	}
}

func (c Card) ToIndex() int {
	return int(c.Suit)*13 + int(c.Rank) - 2
}

func (c Card) String() string {
	ranks := "23456789TJQKA"
	suits := "hdcs"
	return fmt.Sprintf("%c%c", ranks[c.Rank-2], suits[c.Suit])
}

func (c Card) SuitSymbol() string {
	symbols := []string{"♥", "♦", "♣", "♠"}
	return symbols[c.Suit]
}

func (c Card) RankString() string {
	ranks := []string{"2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A"}
	return ranks[c.Rank-2]
}

type Deck struct {
	cards []Card
	index int
	rng   *rand.Rand
}

func NewDeck() *Deck {
	d := &Deck{
		cards: make([]Card, 52),
		rng:   rand.New(rand.NewSource(time.Now().UnixNano())),
	}
	d.Reset()
	return d
}

func (d *Deck) Reset() {
	for i := 0; i < 52; i++ {
		d.cards[i] = CardFromIndex(i)
	}
	d.index = 0
}

func (d *Deck) Shuffle() {
	d.index = 0
	d.rng.Shuffle(len(d.cards), func(i, j int) {
		d.cards[i], d.cards[j] = d.cards[j], d.cards[i]
	})
}

func (d *Deck) Deal() (Card, error) {
	if d.index >= len(d.cards) {
		return Card{}, fmt.Errorf("no more cards")
	}
	card := d.cards[d.index]
	d.index++
	return card, nil
}

func (d *Deck) DealN(n int) ([]Card, error) {
	if d.index+n > len(d.cards) {
		return nil, fmt.Errorf("not enough cards")
	}
	cards := make([]Card, n)
	for i := 0; i < n; i++ {
		cards[i] = d.cards[d.index]
		d.index++
	}
	return cards, nil
}

func (d *Deck) Burn() error {
	if d.index >= len(d.cards) {
		return fmt.Errorf("no more cards")
	}
	d.index++
	return nil
}

func (d *Deck) RemainingCards() int {
	return len(d.cards) - d.index
}

type HandRankType int

const (
	HighCard HandRankType = iota
	OnePair
	TwoPair
	ThreeOfAKind
	Straight
	Flush
	FullHouse
	FourOfAKind
	StraightFlush
	RoyalFlush
)

func (h HandRankType) String() string {
	names := []string{
		"High Card", "One Pair", "Two Pair", "Three of a Kind",
		"Straight", "Flush", "Full House", "Four of a Kind",
		"Straight Flush", "Royal Flush",
	}
	return names[h]
}

type HandRank struct {
	Type    HandRankType `json:"type"`
	Kickers []Rank       `json:"kickers"`
	Cards   []Card       `json:"cards"`
}

func (h HandRank) Compare(other HandRank) int {
	if h.Type != other.Type {
		if h.Type > other.Type {
			return 1
		}
		return -1
	}

	for i := 0; i < len(h.Kickers) && i < len(other.Kickers); i++ {
		if h.Kickers[i] > other.Kickers[i] {
			return 1
		}
		if h.Kickers[i] < other.Kickers[i] {
			return -1
		}
	}

	return 0
}

func EvaluateHand(holeCards, communityCards []Card) HandRank {
	allCards := append([]Card{}, holeCards...)
	allCards = append(allCards, communityCards...)

	if len(allCards) < 5 {
		return HandRank{Type: HighCard}
	}

	var bestHand HandRank
	combinations := getCombinations(allCards, 5)

	for _, combo := range combinations {
		hand := evaluateFiveCards(combo)
		if hand.Compare(bestHand) > 0 {
			bestHand = hand
		}
	}

	return bestHand
}

func evaluateFiveCards(cards []Card) HandRank {
	sort.Slice(cards, func(i, j int) bool {
		return cards[i].Rank > cards[j].Rank
	})

	isFlush := checkFlush(cards)
	isStraight, highCard := checkStraight(cards)
	groups := getRankGroups(cards)

	if isFlush && isStraight {
		if highCard == Ace {
			return HandRank{Type: RoyalFlush, Kickers: []Rank{Ace}, Cards: cards}
		}
		return HandRank{Type: StraightFlush, Kickers: []Rank{highCard}, Cards: cards}
	}

	if groups[0].count == 4 {
		return HandRank{
			Type:    FourOfAKind,
			Kickers: []Rank{groups[0].rank, groups[1].rank},
			Cards:   cards,
		}
	}

	if groups[0].count == 3 && groups[1].count == 2 {
		return HandRank{
			Type:    FullHouse,
			Kickers: []Rank{groups[0].rank, groups[1].rank},
			Cards:   cards,
		}
	}

	if isFlush {
		kickers := make([]Rank, 5)
		for i, c := range cards {
			kickers[i] = c.Rank
		}
		return HandRank{Type: Flush, Kickers: kickers, Cards: cards}
	}

	if isStraight {
		return HandRank{Type: Straight, Kickers: []Rank{highCard}, Cards: cards}
	}

	if groups[0].count == 3 {
		return HandRank{
			Type:    ThreeOfAKind,
			Kickers: []Rank{groups[0].rank, groups[1].rank, groups[2].rank},
			Cards:   cards,
		}
	}

	if groups[0].count == 2 && groups[1].count == 2 {
		return HandRank{
			Type:    TwoPair,
			Kickers: []Rank{groups[0].rank, groups[1].rank, groups[2].rank},
			Cards:   cards,
		}
	}

	if groups[0].count == 2 {
		kickers := []Rank{groups[0].rank}
		for i := 1; i < len(groups) && len(kickers) < 4; i++ {
			kickers = append(kickers, groups[i].rank)
		}
		return HandRank{Type: OnePair, Kickers: kickers, Cards: cards}
	}

	kickers := make([]Rank, 5)
	for i, c := range cards {
		kickers[i] = c.Rank
	}
	return HandRank{Type: HighCard, Kickers: kickers, Cards: cards}
}

func checkFlush(cards []Card) bool {
	suit := cards[0].Suit
	for _, c := range cards[1:] {
		if c.Suit != suit {
			return false
		}
	}
	return true
}

func checkStraight(cards []Card) (bool, Rank) {
	ranks := make([]int, len(cards))
	for i, c := range cards {
		ranks[i] = int(c.Rank)
	}
	sort.Sort(sort.Reverse(sort.IntSlice(ranks)))

	// Check A-2-3-4-5
	if ranks[0] == int(Ace) && ranks[1] == 5 && ranks[2] == 4 && ranks[3] == 3 && ranks[4] == 2 {
		return true, Five
	}

	for i := 0; i < 4; i++ {
		if ranks[i]-ranks[i+1] != 1 {
			return false, 0
		}
	}

	return true, Rank(ranks[0])
}

type rankGroup struct {
	rank  Rank
	count int
}

func getRankGroups(cards []Card) []rankGroup {
	counts := make(map[Rank]int)
	for _, c := range cards {
		counts[c.Rank]++
	}

	groups := make([]rankGroup, 0, len(counts))
	for rank, count := range counts {
		groups = append(groups, rankGroup{rank: rank, count: count})
	}

	sort.Slice(groups, func(i, j int) bool {
		if groups[i].count != groups[j].count {
			return groups[i].count > groups[j].count
		}
		return groups[i].rank > groups[j].rank
	})

	return groups
}

func getCombinations(cards []Card, k int) [][]Card {
	n := len(cards)
	if k > n {
		return nil
	}

	var result [][]Card
	indices := make([]int, k)
	for i := range indices {
		indices[i] = i
	}

	for {
		combo := make([]Card, k)
		for i, idx := range indices {
			combo[i] = cards[idx]
		}
		result = append(result, combo)

		i := k - 1
		for i >= 0 && indices[i] == n-k+i {
			i--
		}

		if i < 0 {
			break
		}

		indices[i]++
		for j := i + 1; j < k; j++ {
			indices[j] = indices[j-1] + 1
		}
	}

	return result
}
