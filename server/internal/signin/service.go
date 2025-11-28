package signin

import (
	"sync"
	"time"
)

type SignInRecord struct {
	UserID          string    `json:"userId"`
	SignDate        time.Time `json:"signDate"`
	ConsecutiveDays int       `json:"consecutiveDays"`
	Reward          int64     `json:"reward"`
}

type SignInReward struct {
	Day    int   `json:"day"`
	Reward int64 `json:"reward"`
	Bonus  int64 `json:"bonus"` // Extra bonus for consecutive days
}

type Service struct {
	records map[string][]*SignInRecord
	rewards []SignInReward
	mu      sync.RWMutex
}

func NewService() *Service {
	s := &Service{
		records: make(map[string][]*SignInRecord),
		rewards: []SignInReward{
			{Day: 1, Reward: 100, Bonus: 0},
			{Day: 2, Reward: 150, Bonus: 0},
			{Day: 3, Reward: 200, Bonus: 50},
			{Day: 4, Reward: 250, Bonus: 0},
			{Day: 5, Reward: 300, Bonus: 100},
			{Day: 6, Reward: 350, Bonus: 0},
			{Day: 7, Reward: 500, Bonus: 200},
		},
	}
	return s
}

func (s *Service) SignIn(userID string) (*SignInRecord, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	today := time.Now().Truncate(24 * time.Hour)
	
	// Check if already signed in today
	if records := s.records[userID]; len(records) > 0 {
		lastRecord := records[len(records)-1]
		if lastRecord.SignDate.Equal(today) {
			return nil, nil // Already signed in
		}
	}

	// Calculate consecutive days
	consecutiveDays := 1
	if records := s.records[userID]; len(records) > 0 {
		lastRecord := records[len(records)-1]
		yesterday := today.AddDate(0, 0, -1)
		if lastRecord.SignDate.Equal(yesterday) {
			consecutiveDays = lastRecord.ConsecutiveDays + 1
		}
	}

	// Cap at 7 days for reward cycle
	rewardDay := ((consecutiveDays - 1) % 7) + 1
	reward := s.rewards[rewardDay-1]
	totalReward := reward.Reward + reward.Bonus

	record := &SignInRecord{
		UserID:          userID,
		SignDate:        today,
		ConsecutiveDays: consecutiveDays,
		Reward:          totalReward,
	}

	s.records[userID] = append(s.records[userID], record)

	return record, nil
}

func (s *Service) GetSignInStatus(userID string) (bool, int, []bool) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	today := time.Now().Truncate(24 * time.Hour)
	signedToday := false
	consecutiveDays := 0
	weekStatus := make([]bool, 7)

	records := s.records[userID]
	if len(records) == 0 {
		return false, 0, weekStatus
	}

	// Check today
	lastRecord := records[len(records)-1]
	if lastRecord.SignDate.Equal(today) {
		signedToday = true
		consecutiveDays = lastRecord.ConsecutiveDays
	} else {
		// Check if streak continues from yesterday
		yesterday := today.AddDate(0, 0, -1)
		if lastRecord.SignDate.Equal(yesterday) {
			consecutiveDays = lastRecord.ConsecutiveDays
		}
	}

	// Get this week's status
	weekStart := today.AddDate(0, 0, -int(today.Weekday()))
	for i := 0; i < 7; i++ {
		checkDate := weekStart.AddDate(0, 0, i)
		for _, record := range records {
			if record.SignDate.Equal(checkDate) {
				weekStatus[i] = true
				break
			}
		}
	}

	return signedToday, consecutiveDays, weekStatus
}

func (s *Service) GetRewards() []SignInReward {
	return s.rewards
}

func (s *Service) GetMonthlySignInDays(userID string, year int, month time.Month) int {
	s.mu.RLock()
	defer s.mu.RUnlock()

	count := 0
	records := s.records[userID]

	for _, record := range records {
		if record.SignDate.Year() == year && record.SignDate.Month() == month {
			count++
		}
	}

	return count
}

func (s *Service) GetTotalSignInDays(userID string) int {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return len(s.records[userID])
}

func (s *Service) GetSignInHistory(userID string, days int) []*SignInRecord {
	s.mu.RLock()
	defer s.mu.RUnlock()

	records := s.records[userID]
	if len(records) == 0 {
		return []*SignInRecord{}
	}

	start := len(records) - days
	if start < 0 {
		start = 0
	}

	return records[start:]
}
