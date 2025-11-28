package stats

import (
	"sync"
	"time"
)

type UserStats struct {
	UserID         string  `json:"userId"`
	HandsPlayed    int64   `json:"handsPlayed"`
	HandsWon       int64   `json:"handsWon"`
	TotalWinnings  int64   `json:"totalWinnings"`
	TotalLosses    int64   `json:"totalLosses"`
	BiggestPot     int64   `json:"biggestPot"`
	BiggestWin     int64   `json:"biggestWin"`
	WinRate        float64 `json:"winRate"`
	VPIP           float64 `json:"vpip"`     // Voluntarily Put In Pot
	PFR            float64 `json:"pfr"`      // Pre-Flop Raise
	AF             float64 `json:"af"`       // Aggression Factor
	CurrentStreak  int     `json:"currentStreak"`
	LongestStreak  int     `json:"longestStreak"`
	PlayTimeMinutes int64  `json:"playTimeMinutes"`
	
	// Hand type counts
	RoyalFlushes    int `json:"royalFlushes"`
	StraightFlushes int `json:"straightFlushes"`
	FourOfAKinds    int `json:"fourOfAKinds"`
	FullHouses      int `json:"fullHouses"`
	Flushes         int `json:"flushes"`
	Straights       int `json:"straights"`
	ThreeOfAKinds   int `json:"threeOfAKinds"`
	TwoPairs        int `json:"twoPairs"`
	Pairs           int `json:"pairs"`

	// Session stats
	SessionStartTime time.Time `json:"sessionStartTime"`
	SessionHands     int       `json:"sessionHands"`
	SessionProfit    int64     `json:"sessionProfit"`

	LastUpdated time.Time `json:"lastUpdated"`
}

type DailyStats struct {
	Date        time.Time `json:"date"`
	HandsPlayed int64     `json:"handsPlayed"`
	HandsWon    int64     `json:"handsWon"`
	Profit      int64     `json:"profit"`
	PeakChips   int64     `json:"peakChips"`
}

type Service struct {
	userStats  map[string]*UserStats
	dailyStats map[string]map[string]*DailyStats // userID -> date -> stats
	mu         sync.RWMutex
}

func NewService() *Service {
	return &Service{
		userStats:  make(map[string]*UserStats),
		dailyStats: make(map[string]map[string]*DailyStats),
	}
}

func (s *Service) GetUserStats(userID string) *UserStats {
	s.mu.RLock()
	defer s.mu.RUnlock()

	stats := s.userStats[userID]
	if stats == nil {
		return &UserStats{UserID: userID}
	}
	return stats
}

func (s *Service) getOrCreateStats(userID string) *UserStats {
	if s.userStats[userID] == nil {
		s.userStats[userID] = &UserStats{
			UserID:      userID,
			LastUpdated: time.Now(),
		}
	}
	return s.userStats[userID]
}

func (s *Service) RecordHandPlayed(userID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	stats := s.getOrCreateStats(userID)
	stats.HandsPlayed++
	stats.SessionHands++
	stats.LastUpdated = time.Now()

	s.updateDailyStats(userID, func(daily *DailyStats) {
		daily.HandsPlayed++
	})
}

func (s *Service) RecordHandWon(userID string, amount int64, handType string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	stats := s.getOrCreateStats(userID)
	stats.HandsWon++
	stats.TotalWinnings += amount
	stats.SessionProfit += amount
	stats.CurrentStreak++
	
	if stats.CurrentStreak > stats.LongestStreak {
		stats.LongestStreak = stats.CurrentStreak
	}
	
	if amount > stats.BiggestWin {
		stats.BiggestWin = amount
	}

	// Update hand type counts
	s.recordHandType(stats, handType)
	s.updateWinRate(stats)

	stats.LastUpdated = time.Now()

	s.updateDailyStats(userID, func(daily *DailyStats) {
		daily.HandsWon++
		daily.Profit += amount
	})
}

func (s *Service) RecordHandLost(userID string, amount int64) {
	s.mu.Lock()
	defer s.mu.Unlock()

	stats := s.getOrCreateStats(userID)
	stats.TotalLosses += amount
	stats.SessionProfit -= amount
	stats.CurrentStreak = 0
	
	s.updateWinRate(stats)
	stats.LastUpdated = time.Now()

	s.updateDailyStats(userID, func(daily *DailyStats) {
		daily.Profit -= amount
	})
}

func (s *Service) RecordBigPot(userID string, potSize int64) {
	s.mu.Lock()
	defer s.mu.Unlock()

	stats := s.getOrCreateStats(userID)
	if potSize > stats.BiggestPot {
		stats.BiggestPot = potSize
		stats.LastUpdated = time.Now()
	}
}

func (s *Service) recordHandType(stats *UserStats, handType string) {
	switch handType {
	case "royal_flush":
		stats.RoyalFlushes++
	case "straight_flush":
		stats.StraightFlushes++
	case "four_of_kind":
		stats.FourOfAKinds++
	case "full_house":
		stats.FullHouses++
	case "flush":
		stats.Flushes++
	case "straight":
		stats.Straights++
	case "three_of_kind":
		stats.ThreeOfAKinds++
	case "two_pair":
		stats.TwoPairs++
	case "pair":
		stats.Pairs++
	}
}

func (s *Service) updateWinRate(stats *UserStats) {
	if stats.HandsPlayed > 0 {
		stats.WinRate = float64(stats.HandsWon) / float64(stats.HandsPlayed)
	}
}

func (s *Service) StartSession(userID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	stats := s.getOrCreateStats(userID)
	stats.SessionStartTime = time.Now()
	stats.SessionHands = 0
	stats.SessionProfit = 0
}

func (s *Service) EndSession(userID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	stats := s.userStats[userID]
	if stats == nil || stats.SessionStartTime.IsZero() {
		return
	}

	duration := time.Since(stats.SessionStartTime)
	stats.PlayTimeMinutes += int64(duration.Minutes())
	stats.SessionStartTime = time.Time{}
}

func (s *Service) RecordPlayTime(userID string, minutes int64) {
	s.mu.Lock()
	defer s.mu.Unlock()

	stats := s.getOrCreateStats(userID)
	stats.PlayTimeMinutes += minutes
	stats.LastUpdated = time.Now()
}

func (s *Service) updateDailyStats(userID string, updater func(*DailyStats)) {
	today := time.Now().Format("2006-01-02")

	if s.dailyStats[userID] == nil {
		s.dailyStats[userID] = make(map[string]*DailyStats)
	}

	if s.dailyStats[userID][today] == nil {
		s.dailyStats[userID][today] = &DailyStats{
			Date: time.Now().Truncate(24 * time.Hour),
		}
	}

	updater(s.dailyStats[userID][today])
}

func (s *Service) GetDailyStats(userID string, days int) []*DailyStats {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.dailyStats[userID] == nil {
		return []*DailyStats{}
	}

	result := make([]*DailyStats, 0)
	now := time.Now().Truncate(24 * time.Hour)

	for i := 0; i < days; i++ {
		date := now.AddDate(0, 0, -i).Format("2006-01-02")
		if stats := s.dailyStats[userID][date]; stats != nil {
			result = append(result, stats)
		}
	}

	return result
}

func (s *Service) GetLeaderboard(sortBy string, limit int) []*UserStats {
	s.mu.RLock()
	defer s.mu.RUnlock()

	stats := make([]*UserStats, 0, len(s.userStats))
	for _, stat := range s.userStats {
		stats = append(stats, stat)
	}

	// Sort based on criteria
	switch sortBy {
	case "winRate":
		for i := 0; i < len(stats)-1; i++ {
			for j := i + 1; j < len(stats); j++ {
				if stats[j].WinRate > stats[i].WinRate {
					stats[i], stats[j] = stats[j], stats[i]
				}
			}
		}
	case "handsWon":
		for i := 0; i < len(stats)-1; i++ {
			for j := i + 1; j < len(stats); j++ {
				if stats[j].HandsWon > stats[i].HandsWon {
					stats[i], stats[j] = stats[j], stats[i]
				}
			}
		}
	default: // chips/winnings
		for i := 0; i < len(stats)-1; i++ {
			for j := i + 1; j < len(stats); j++ {
				if stats[j].TotalWinnings-stats[j].TotalLosses > stats[i].TotalWinnings-stats[i].TotalLosses {
					stats[i], stats[j] = stats[j], stats[i]
				}
			}
		}
	}

	if limit > 0 && len(stats) > limit {
		stats = stats[:limit]
	}

	return stats
}

func (s *Service) GetStatsForAchievements(userID string) map[string]int64 {
	s.mu.RLock()
	defer s.mu.RUnlock()

	stats := s.userStats[userID]
	if stats == nil {
		return map[string]int64{}
	}

	return map[string]int64{
		"hands_played":    stats.HandsPlayed,
		"hands_won":       stats.HandsWon,
		"royal_flush":     int64(stats.RoyalFlushes),
		"straight_flush":  int64(stats.StraightFlushes),
		"four_of_kind":    int64(stats.FourOfAKinds),
		"full_house":      int64(stats.FullHouses),
		"biggest_pot":     stats.BiggestPot,
		"longest_streak":  int64(stats.LongestStreak),
		"play_time":       stats.PlayTimeMinutes,
	}
}
