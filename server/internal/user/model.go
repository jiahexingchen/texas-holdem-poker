package user

import (
	"time"
)

type User struct {
	ID           string    `json:"id" db:"id"`
	Username     string    `json:"username" db:"username"`
	Email        string    `json:"email" db:"email"`
	Phone        string    `json:"phone,omitempty" db:"phone"`
	PasswordHash string    `json:"-" db:"password_hash"`
	Nickname     string    `json:"nickname" db:"nickname"`
	Avatar       string    `json:"avatar" db:"avatar"`
	Level        int       `json:"level" db:"level"`
	Exp          int64     `json:"exp" db:"exp"`
	Chips        int64     `json:"chips" db:"chips"`
	Diamonds     int64     `json:"diamonds" db:"diamonds"`
	VipLevel     int       `json:"vipLevel" db:"vip_level"`
	Status       int       `json:"status" db:"status"`
	CreatedAt    time.Time `json:"createdAt" db:"created_at"`
	UpdatedAt    time.Time `json:"updatedAt" db:"updated_at"`
	LastLoginAt  time.Time `json:"lastLoginAt" db:"last_login_at"`
}

type UserStats struct {
	UserID       string  `json:"userId" db:"user_id"`
	HandsPlayed  int64   `json:"handsPlayed" db:"hands_played"`
	HandsWon     int64   `json:"handsWon" db:"hands_won"`
	TotalWinnings int64  `json:"totalWinnings" db:"total_winnings"`
	TotalLosses  int64   `json:"totalLosses" db:"total_losses"`
	BiggestPot   int64   `json:"biggestPot" db:"biggest_pot"`
	WinRate      float64 `json:"winRate" db:"win_rate"`
}

type GameRecord struct {
	ID          string    `json:"id" db:"id"`
	UserID      string    `json:"userId" db:"user_id"`
	RoomID      string    `json:"roomId" db:"room_id"`
	HandNumber  int       `json:"handNumber" db:"hand_number"`
	BuyIn       int64     `json:"buyIn" db:"buy_in"`
	CashOut     int64     `json:"cashOut" db:"cash_out"`
	Profit      int64     `json:"profit" db:"profit"`
	HandsPlayed int       `json:"handsPlayed" db:"hands_played"`
	HandsWon    int       `json:"handsWon" db:"hands_won"`
	CreatedAt   time.Time `json:"createdAt" db:"created_at"`
}

type Friendship struct {
	UserID    string    `json:"userId" db:"user_id"`
	FriendID  string    `json:"friendId" db:"friend_id"`
	Status    int       `json:"status" db:"status"` // 0=pending, 1=accepted, 2=blocked
	CreatedAt time.Time `json:"createdAt" db:"created_at"`
}

type UserItem struct {
	UserID    string    `json:"userId" db:"user_id"`
	ItemID    string    `json:"itemId" db:"item_id"`
	ItemType  string    `json:"itemType" db:"item_type"`
	Quantity  int       `json:"quantity" db:"quantity"`
	Equipped  bool      `json:"equipped" db:"equipped"`
	ExpiresAt time.Time `json:"expiresAt,omitempty" db:"expires_at"`
}

type DailyTask struct {
	ID          string `json:"id" db:"id"`
	Name        string `json:"name" db:"name"`
	Description string `json:"description" db:"description"`
	TaskType    string `json:"taskType" db:"task_type"`
	Target      int    `json:"target" db:"target"`
	Reward      int64  `json:"reward" db:"reward"`
	RewardType  string `json:"rewardType" db:"reward_type"`
}

type UserTask struct {
	UserID    string    `json:"userId" db:"user_id"`
	TaskID    string    `json:"taskId" db:"task_id"`
	Progress  int       `json:"progress" db:"progress"`
	Completed bool      `json:"completed" db:"completed"`
	Claimed   bool      `json:"claimed" db:"claimed"`
	Date      time.Time `json:"date" db:"date"`
}

const (
	StatusNormal  = 0
	StatusBanned  = 1
	StatusDeleted = 2
)

const (
	FriendPending  = 0
	FriendAccepted = 1
	FriendBlocked  = 2
)

func NewUser(username, email, passwordHash string) *User {
	now := time.Now()
	return &User{
		Username:     username,
		Email:        email,
		PasswordHash: passwordHash,
		Nickname:     username,
		Avatar:       "default",
		Level:        1,
		Exp:          0,
		Chips:        10000, // 初始筹码
		Diamonds:     0,
		VipLevel:     0,
		Status:       StatusNormal,
		CreatedAt:    now,
		UpdatedAt:    now,
		LastLoginAt:  now,
	}
}

func (u *User) AddChips(amount int64) {
	if amount > 0 {
		u.Chips += amount
		u.UpdatedAt = time.Now()
	}
}

func (u *User) DeductChips(amount int64) bool {
	if amount > 0 && u.Chips >= amount {
		u.Chips -= amount
		u.UpdatedAt = time.Now()
		return true
	}
	return false
}

func (u *User) AddExp(amount int64) {
	u.Exp += amount
	u.checkLevelUp()
	u.UpdatedAt = time.Now()
}

func (u *User) checkLevelUp() {
	expRequired := int64(u.Level * 1000)
	for u.Exp >= expRequired {
		u.Exp -= expRequired
		u.Level++
		expRequired = int64(u.Level * 1000)
	}
}

func (u *User) ToPublicInfo() map[string]interface{} {
	return map[string]interface{}{
		"id":       u.ID,
		"nickname": u.Nickname,
		"avatar":   u.Avatar,
		"level":    u.Level,
		"vipLevel": u.VipLevel,
	}
}
