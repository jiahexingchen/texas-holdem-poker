package task

import (
	"sync"
	"time"
)

type TaskType string

const (
	TaskHandsPlayed   TaskType = "hands_played"
	TaskHandsWon      TaskType = "hands_won"
	TaskPlayTime      TaskType = "play_time"
	TaskBigPot        TaskType = "big_pot"
	TaskFriendGame    TaskType = "friend_game"
	TaskWinStreak     TaskType = "win_streak"
	TaskRoyalFlush    TaskType = "royal_flush"
	TaskAllIn         TaskType = "all_in"
)

type Task struct {
	ID          string   `json:"id"`
	Name        string   `json:"name"`
	Description string   `json:"description"`
	TaskType    TaskType `json:"taskType"`
	Target      int      `json:"target"`
	Reward      int64    `json:"reward"`
	RewardType  string   `json:"rewardType"`
}

type UserTaskProgress struct {
	UserID    string    `json:"userId"`
	TaskID    string    `json:"taskId"`
	Progress  int       `json:"progress"`
	Completed bool      `json:"completed"`
	Claimed   bool      `json:"claimed"`
	Date      time.Time `json:"date"`
}

type Service struct {
	tasks     map[string]*Task
	userTasks map[string]map[string]*UserTaskProgress // userID -> taskID -> progress
	mu        sync.RWMutex
}

func NewService() *Service {
	s := &Service{
		tasks:     make(map[string]*Task),
		userTasks: make(map[string]map[string]*UserTaskProgress),
	}
	s.initDefaultTasks()
	return s
}

func (s *Service) initDefaultTasks() {
	defaultTasks := []*Task{
		{
			ID:          "play_5_hands",
			Name:        "参与5手牌局",
			Description: "完成5手牌局游戏",
			TaskType:    TaskHandsPlayed,
			Target:      5,
			Reward:      100,
			RewardType:  "chips",
		},
		{
			ID:          "play_20_hands",
			Name:        "参与20手牌局",
			Description: "完成20手牌局游戏",
			TaskType:    TaskHandsPlayed,
			Target:      20,
			Reward:      300,
			RewardType:  "chips",
		},
		{
			ID:          "win_3_hands",
			Name:        "赢得3手牌局",
			Description: "赢得3手牌局",
			TaskType:    TaskHandsWon,
			Target:      3,
			Reward:      200,
			RewardType:  "chips",
		},
		{
			ID:          "win_10_hands",
			Name:        "赢得10手牌局",
			Description: "赢得10手牌局",
			TaskType:    TaskHandsWon,
			Target:      10,
			Reward:      500,
			RewardType:  "chips",
		},
		{
			ID:          "play_30_min",
			Name:        "游戏30分钟",
			Description: "累计游戏时长30分钟",
			TaskType:    TaskPlayTime,
			Target:      30,
			Reward:      150,
			RewardType:  "chips",
		},
		{
			ID:          "win_big_pot",
			Name:        "赢得大底池",
			Description: "赢得1000以上的底池",
			TaskType:    TaskBigPot,
			Target:      1,
			Reward:      300,
			RewardType:  "chips",
		},
		{
			ID:          "all_in_win",
			Name:        "全下获胜",
			Description: "全下并赢得底池",
			TaskType:    TaskAllIn,
			Target:      1,
			Reward:      250,
			RewardType:  "chips",
		},
		{
			ID:          "win_streak_3",
			Name:        "三连胜",
			Description: "连续赢得3手牌",
			TaskType:    TaskWinStreak,
			Target:      3,
			Reward:      400,
			RewardType:  "chips",
		},
	}

	for _, task := range defaultTasks {
		s.tasks[task.ID] = task
	}
}

func (s *Service) GetDailyTasks(userID string) []*UserTaskProgress {
	s.mu.Lock()
	defer s.mu.Unlock()

	today := time.Now().Truncate(24 * time.Hour)
	
	if s.userTasks[userID] == nil {
		s.userTasks[userID] = make(map[string]*UserTaskProgress)
	}

	// Reset tasks if it's a new day
	for taskID := range s.tasks {
		progress, exists := s.userTasks[userID][taskID]
		if !exists || progress.Date.Before(today) {
			s.userTasks[userID][taskID] = &UserTaskProgress{
				UserID:    userID,
				TaskID:    taskID,
				Progress:  0,
				Completed: false,
				Claimed:   false,
				Date:      today,
			}
		}
	}

	result := make([]*UserTaskProgress, 0, len(s.tasks))
	for _, progress := range s.userTasks[userID] {
		result = append(result, progress)
	}

	return result
}

func (s *Service) UpdateProgress(userID string, taskType TaskType, amount int) []string {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.userTasks[userID] == nil {
		s.userTasks[userID] = make(map[string]*UserTaskProgress)
	}

	completedTasks := make([]string, 0)

	for taskID, task := range s.tasks {
		if task.TaskType != taskType {
			continue
		}

		progress, exists := s.userTasks[userID][taskID]
		if !exists {
			progress = &UserTaskProgress{
				UserID:    userID,
				TaskID:    taskID,
				Progress:  0,
				Completed: false,
				Claimed:   false,
				Date:      time.Now().Truncate(24 * time.Hour),
			}
			s.userTasks[userID][taskID] = progress
		}

		if progress.Completed {
			continue
		}

		progress.Progress += amount
		if progress.Progress >= task.Target {
			progress.Progress = task.Target
			progress.Completed = true
			completedTasks = append(completedTasks, taskID)
		}
	}

	return completedTasks
}

func (s *Service) ClaimReward(userID, taskID string) (int64, string, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.userTasks[userID] == nil {
		return 0, "", nil
	}

	progress, exists := s.userTasks[userID][taskID]
	if !exists || !progress.Completed || progress.Claimed {
		return 0, "", nil
	}

	task := s.tasks[taskID]
	if task == nil {
		return 0, "", nil
	}

	progress.Claimed = true
	return task.Reward, task.RewardType, nil
}

func (s *Service) GetTask(taskID string) *Task {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.tasks[taskID]
}

func (s *Service) GetAllTasks() []*Task {
	s.mu.RLock()
	defer s.mu.RUnlock()

	result := make([]*Task, 0, len(s.tasks))
	for _, task := range s.tasks {
		result = append(result, task)
	}
	return result
}

// Event handlers for game events
func (s *Service) OnHandPlayed(userID string) []string {
	return s.UpdateProgress(userID, TaskHandsPlayed, 1)
}

func (s *Service) OnHandWon(userID string) []string {
	return s.UpdateProgress(userID, TaskHandsWon, 1)
}

func (s *Service) OnBigPotWon(userID string, potSize int64) []string {
	if potSize >= 1000 {
		return s.UpdateProgress(userID, TaskBigPot, 1)
	}
	return nil
}

func (s *Service) OnAllInWin(userID string) []string {
	return s.UpdateProgress(userID, TaskAllIn, 1)
}

func (s *Service) OnPlayTime(userID string, minutes int) []string {
	return s.UpdateProgress(userID, TaskPlayTime, minutes)
}
