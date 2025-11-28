package notification

import (
	"sync"
	"time"
)

type NotificationType string

const (
	NotifyFriendRequest   NotificationType = "friend_request"
	NotifyFriendAccepted  NotificationType = "friend_accepted"
	NotifyGameInvite      NotificationType = "game_invite"
	NotifyAchievement     NotificationType = "achievement"
	NotifyTaskComplete    NotificationType = "task_complete"
	NotifyReward          NotificationType = "reward"
	NotifySystem          NotificationType = "system"
	NotifyMaintenance     NotificationType = "maintenance"
	NotifyPromotion       NotificationType = "promotion"
)

type Notification struct {
	ID        string           `json:"id"`
	UserID    string           `json:"userId"`
	Type      NotificationType `json:"type"`
	Title     string           `json:"title"`
	Content   string           `json:"content"`
	Data      map[string]interface{} `json:"data,omitempty"`
	Read      bool             `json:"read"`
	CreatedAt time.Time        `json:"createdAt"`
	ExpiresAt time.Time        `json:"expiresAt,omitempty"`
}

type Service struct {
	notifications map[string][]*Notification // userID -> notifications
	maxPerUser    int
	mu            sync.RWMutex
	
	// Callback for real-time push
	onNotify func(userID string, notification *Notification)
}

func NewService(maxPerUser int) *Service {
	if maxPerUser <= 0 {
		maxPerUser = 100
	}
	return &Service{
		notifications: make(map[string][]*Notification),
		maxPerUser:    maxPerUser,
	}
}

func (s *Service) SetNotifyCallback(callback func(userID string, notification *Notification)) {
	s.onNotify = callback
}

func (s *Service) Send(userID string, notifType NotificationType, title, content string, data map[string]interface{}) *Notification {
	s.mu.Lock()
	defer s.mu.Unlock()

	notification := &Notification{
		ID:        generateNotificationID(),
		UserID:    userID,
		Type:      notifType,
		Title:     title,
		Content:   content,
		Data:      data,
		Read:      false,
		CreatedAt: time.Now(),
	}

	if s.notifications[userID] == nil {
		s.notifications[userID] = make([]*Notification, 0)
	}

	s.notifications[userID] = append(s.notifications[userID], notification)

	// Trim old notifications
	if len(s.notifications[userID]) > s.maxPerUser {
		s.notifications[userID] = s.notifications[userID][1:]
	}

	// Real-time callback
	if s.onNotify != nil {
		go s.onNotify(userID, notification)
	}

	return notification
}

func (s *Service) SendFriendRequest(userID, fromUserID, fromName string) *Notification {
	return s.Send(userID, NotifyFriendRequest, "好友请求", 
		fromName + " 请求添加你为好友",
		map[string]interface{}{"fromUserId": fromUserID, "fromName": fromName})
}

func (s *Service) SendFriendAccepted(userID, friendName string) *Notification {
	return s.Send(userID, NotifyFriendAccepted, "好友添加成功",
		friendName + " 已接受你的好友请求", nil)
}

func (s *Service) SendGameInvite(userID, fromUserID, fromName, roomID string) *Notification {
	return s.Send(userID, NotifyGameInvite, "游戏邀请",
		fromName + " 邀请你加入游戏",
		map[string]interface{}{"fromUserId": fromUserID, "roomId": roomID})
}

func (s *Service) SendAchievementUnlocked(userID, achievementName string, reward int64) *Notification {
	return s.Send(userID, NotifyAchievement, "成就解锁",
		"恭喜解锁成就: " + achievementName,
		map[string]interface{}{"achievementName": achievementName, "reward": reward})
}

func (s *Service) SendTaskComplete(userID, taskName string, reward int64) *Notification {
	return s.Send(userID, NotifyTaskComplete, "任务完成",
		"完成任务: " + taskName,
		map[string]interface{}{"taskName": taskName, "reward": reward})
}

func (s *Service) SendReward(userID, reason string, amount int64, rewardType string) *Notification {
	return s.Send(userID, NotifyReward, "获得奖励",
		reason,
		map[string]interface{}{"amount": amount, "type": rewardType})
}

func (s *Service) SendSystemNotification(userID, title, content string) *Notification {
	return s.Send(userID, NotifySystem, title, content, nil)
}

func (s *Service) BroadcastMaintenance(userIDs []string, startTime time.Time, duration time.Duration) {
	title := "维护通知"
	content := "服务器将于 " + startTime.Format("01-02 15:04") + " 进行维护，预计时长 " + duration.String()
	
	for _, userID := range userIDs {
		s.Send(userID, NotifyMaintenance, title, content,
			map[string]interface{}{"startTime": startTime.Unix(), "duration": int(duration.Minutes())})
	}
}

func (s *Service) BroadcastPromotion(userIDs []string, title, content string, data map[string]interface{}) {
	for _, userID := range userIDs {
		s.Send(userID, NotifyPromotion, title, content, data)
	}
}

func (s *Service) GetNotifications(userID string, unreadOnly bool, limit int) []*Notification {
	s.mu.RLock()
	defer s.mu.RUnlock()

	notifications := s.notifications[userID]
	if notifications == nil {
		return []*Notification{}
	}

	now := time.Now()
	result := make([]*Notification, 0)

	for i := len(notifications) - 1; i >= 0 && (limit <= 0 || len(result) < limit); i-- {
		n := notifications[i]
		
		// Skip expired
		if !n.ExpiresAt.IsZero() && now.After(n.ExpiresAt) {
			continue
		}

		// Filter by read status
		if unreadOnly && n.Read {
			continue
		}

		result = append(result, n)
	}

	return result
}

func (s *Service) MarkAsRead(userID string, notificationIDs []string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	notifications := s.notifications[userID]
	if notifications == nil {
		return
	}

	idSet := make(map[string]bool)
	for _, id := range notificationIDs {
		idSet[id] = true
	}

	for _, n := range notifications {
		if idSet[n.ID] {
			n.Read = true
		}
	}
}

func (s *Service) MarkAllAsRead(userID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	notifications := s.notifications[userID]
	for _, n := range notifications {
		n.Read = true
	}
}

func (s *Service) GetUnreadCount(userID string) int {
	s.mu.RLock()
	defer s.mu.RUnlock()

	notifications := s.notifications[userID]
	count := 0
	now := time.Now()

	for _, n := range notifications {
		if !n.Read && (n.ExpiresAt.IsZero() || now.Before(n.ExpiresAt)) {
			count++
		}
	}

	return count
}

func (s *Service) DeleteNotification(userID, notificationID string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	notifications := s.notifications[userID]
	for i, n := range notifications {
		if n.ID == notificationID {
			s.notifications[userID] = append(notifications[:i], notifications[i+1:]...)
			return
		}
	}
}

func (s *Service) ClearNotifications(userID string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	delete(s.notifications, userID)
}

func generateNotificationID() string {
	return time.Now().Format("20060102150405.000000")
}
