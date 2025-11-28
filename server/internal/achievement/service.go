package achievement

import (
	"sync"
	"time"
)

type AchievementCategory string

const (
	CategoryBeginner     AchievementCategory = "beginner"
	CategoryIntermediate AchievementCategory = "intermediate"
	CategoryAdvanced     AchievementCategory = "advanced"
	CategoryHand         AchievementCategory = "hand"
	CategoryWealth       AchievementCategory = "wealth"
	CategorySocial       AchievementCategory = "social"
)

type Achievement struct {
	ID          string              `json:"id"`
	Name        string              `json:"name"`
	Description string              `json:"description"`
	Category    AchievementCategory `json:"category"`
	Icon        string              `json:"icon"`
	Reward      int64               `json:"reward"`
	Requirement map[string]int64    `json:"requirement"`
	Hidden      bool                `json:"hidden"`
}

type UserAchievement struct {
	UserID        string    `json:"userId"`
	AchievementID string    `json:"achievementId"`
	UnlockedAt    time.Time `json:"unlockedAt"`
	Claimed       bool      `json:"claimed"`
}

type Service struct {
	achievements     map[string]*Achievement
	userAchievements map[string]map[string]*UserAchievement
	mu               sync.RWMutex
}

func NewService() *Service {
	s := &Service{
		achievements:     make(map[string]*Achievement),
		userAchievements: make(map[string]map[string]*UserAchievement),
	}
	s.initDefaultAchievements()
	return s
}

func (s *Service) initDefaultAchievements() {
	achievements := []*Achievement{
		// Beginner
		{
			ID:          "first_win",
			Name:        "初出茅庐",
			Description: "赢得第一手牌",
			Category:    CategoryBeginner,
			Icon:        "trophy_bronze",
			Reward:      100,
			Requirement: map[string]int64{"hands_won": 1},
		},
		{
			ID:          "first_game",
			Name:        "新手上路",
			Description: "完成第一场游戏",
			Category:    CategoryBeginner,
			Icon:        "star",
			Reward:      50,
			Requirement: map[string]int64{"hands_played": 1},
		},
		{
			ID:          "win_10",
			Name:        "小有成就",
			Description: "累计赢得10手牌",
			Category:    CategoryBeginner,
			Icon:        "trophy_bronze",
			Reward:      500,
			Requirement: map[string]int64{"hands_won": 10},
		},
		{
			ID:          "play_50",
			Name:        "持之以恒",
			Description: "累计参与50手牌",
			Category:    CategoryBeginner,
			Icon:        "medal",
			Reward:      300,
			Requirement: map[string]int64{"hands_played": 50},
		},

		// Intermediate
		{
			ID:          "win_100",
			Name:        "身经百战",
			Description: "累计赢得100手牌",
			Category:    CategoryIntermediate,
			Icon:        "trophy_silver",
			Reward:      2000,
			Requirement: map[string]int64{"hands_won": 100},
		},
		{
			ID:          "win_500",
			Name:        "百战百胜",
			Description: "累计赢得500手牌",
			Category:    CategoryIntermediate,
			Icon:        "trophy_gold",
			Reward:      5000,
			Requirement: map[string]int64{"hands_won": 500},
		},
		{
			ID:          "play_1000",
			Name:        "老玩家",
			Description: "累计参与1000手牌",
			Category:    CategoryIntermediate,
			Icon:        "veteran",
			Reward:      3000,
			Requirement: map[string]int64{"hands_played": 1000},
		},

		// Advanced
		{
			ID:          "win_1000",
			Name:        "扑克大师",
			Description: "累计赢得1000手牌",
			Category:    CategoryAdvanced,
			Icon:        "trophy_platinum",
			Reward:      10000,
			Requirement: map[string]int64{"hands_won": 1000},
		},

		// Hand achievements
		{
			ID:          "royal_flush",
			Name:        "皇家同花顺",
			Description: "打出皇家同花顺",
			Category:    CategoryHand,
			Icon:        "crown",
			Reward:      10000,
			Requirement: map[string]int64{"royal_flush": 1},
		},
		{
			ID:          "straight_flush",
			Name:        "同花顺达人",
			Description: "打出同花顺",
			Category:    CategoryHand,
			Icon:        "flush",
			Reward:      5000,
			Requirement: map[string]int64{"straight_flush": 1},
		},
		{
			ID:          "four_of_kind",
			Name:        "四条高手",
			Description: "打出四条",
			Category:    CategoryHand,
			Icon:        "quads",
			Reward:      2000,
			Requirement: map[string]int64{"four_of_kind": 1},
		},
		{
			ID:          "full_house",
			Name:        "葫芦大师",
			Description: "打出10次葫芦",
			Category:    CategoryHand,
			Icon:        "fullhouse",
			Reward:      1000,
			Requirement: map[string]int64{"full_house": 10},
		},

		// Wealth
		{
			ID:          "rich_10k",
			Name:        "小康之家",
			Description: "筹码达到10,000",
			Category:    CategoryWealth,
			Icon:        "coins",
			Reward:      500,
			Requirement: map[string]int64{"chips": 10000},
		},
		{
			ID:          "rich_100k",
			Name:        "富甲一方",
			Description: "筹码达到100,000",
			Category:    CategoryWealth,
			Icon:        "money_bag",
			Reward:      2000,
			Requirement: map[string]int64{"chips": 100000},
		},
		{
			ID:          "millionaire",
			Name:        "百万富翁",
			Description: "筹码达到1,000,000",
			Category:    CategoryWealth,
			Icon:        "diamond",
			Reward:      5000,
			Requirement: map[string]int64{"chips": 1000000},
		},

		// Social
		{
			ID:          "first_friend",
			Name:        "交友达人",
			Description: "添加第一个好友",
			Category:    CategorySocial,
			Icon:        "handshake",
			Reward:      200,
			Requirement: map[string]int64{"friends": 1},
		},
		{
			ID:          "social_butterfly",
			Name:        "社交蝴蝶",
			Description: "添加10个好友",
			Category:    CategorySocial,
			Icon:        "friends",
			Reward:      1000,
			Requirement: map[string]int64{"friends": 10},
		},
		{
			ID:          "popular",
			Name:        "万人迷",
			Description: "添加50个好友",
			Category:    CategorySocial,
			Icon:        "popular",
			Reward:      3000,
			Requirement: map[string]int64{"friends": 50},
		},
	}

	for _, a := range achievements {
		s.achievements[a.ID] = a
	}
}

func (s *Service) GetAllAchievements() []*Achievement {
	s.mu.RLock()
	defer s.mu.RUnlock()

	result := make([]*Achievement, 0, len(s.achievements))
	for _, a := range s.achievements {
		if !a.Hidden {
			result = append(result, a)
		}
	}
	return result
}

func (s *Service) GetUserAchievements(userID string) []*UserAchievement {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.userAchievements[userID] == nil {
		return []*UserAchievement{}
	}

	result := make([]*UserAchievement, 0)
	for _, ua := range s.userAchievements[userID] {
		result = append(result, ua)
	}
	return result
}

func (s *Service) CheckAndUnlock(userID string, stats map[string]int64) []string {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.userAchievements[userID] == nil {
		s.userAchievements[userID] = make(map[string]*UserAchievement)
	}

	unlocked := make([]string, 0)

	for id, achievement := range s.achievements {
		// Skip if already unlocked
		if _, exists := s.userAchievements[userID][id]; exists {
			continue
		}

		// Check requirements
		allMet := true
		for key, required := range achievement.Requirement {
			if current, ok := stats[key]; !ok || current < required {
				allMet = false
				break
			}
		}

		if allMet {
			s.userAchievements[userID][id] = &UserAchievement{
				UserID:        userID,
				AchievementID: id,
				UnlockedAt:    time.Now(),
				Claimed:       false,
			}
			unlocked = append(unlocked, id)
		}
	}

	return unlocked
}

func (s *Service) ClaimReward(userID, achievementID string) (int64, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.userAchievements[userID] == nil {
		return 0, nil
	}

	ua, exists := s.userAchievements[userID][achievementID]
	if !exists || ua.Claimed {
		return 0, nil
	}

	achievement := s.achievements[achievementID]
	if achievement == nil {
		return 0, nil
	}

	ua.Claimed = true
	return achievement.Reward, nil
}

func (s *Service) GetAchievement(id string) *Achievement {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.achievements[id]
}

func (s *Service) HasAchievement(userID, achievementID string) bool {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.userAchievements[userID] == nil {
		return false
	}
	_, exists := s.userAchievements[userID][achievementID]
	return exists
}

func (s *Service) GetProgress(userID string, stats map[string]int64) map[string]float64 {
	s.mu.RLock()
	defer s.mu.RUnlock()

	progress := make(map[string]float64)

	for id, achievement := range s.achievements {
		// Skip if already unlocked
		if s.userAchievements[userID] != nil {
			if _, exists := s.userAchievements[userID][id]; exists {
				progress[id] = 1.0
				continue
			}
		}

		// Calculate progress
		totalProgress := 0.0
		reqCount := len(achievement.Requirement)
		
		for key, required := range achievement.Requirement {
			current := stats[key]
			p := float64(current) / float64(required)
			if p > 1.0 {
				p = 1.0
			}
			totalProgress += p
		}

		if reqCount > 0 {
			progress[id] = totalProgress / float64(reqCount)
		}
	}

	return progress
}
