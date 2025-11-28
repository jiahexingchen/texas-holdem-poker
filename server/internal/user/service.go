package user

import (
	"errors"
	"fmt"
	"sync"
	"time"

	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"
	"golang.org/x/crypto/bcrypt"
)

var (
	ErrUserNotFound     = errors.New("user not found")
	ErrUserExists       = errors.New("user already exists")
	ErrInvalidPassword  = errors.New("invalid password")
	ErrInvalidToken     = errors.New("invalid token")
	ErrInsufficientChips = errors.New("insufficient chips")
)

type Service struct {
	users     map[string]*User
	usersByEmail map[string]*User
	usersByUsername map[string]*User
	stats     map[string]*UserStats
	jwtSecret []byte
	mu        sync.RWMutex
}

func NewService(jwtSecret string) *Service {
	return &Service{
		users:           make(map[string]*User),
		usersByEmail:    make(map[string]*User),
		usersByUsername: make(map[string]*User),
		stats:           make(map[string]*UserStats),
		jwtSecret:       []byte(jwtSecret),
	}
}

type RegisterRequest struct {
	Username string `json:"username"`
	Email    string `json:"email"`
	Password string `json:"password"`
	Phone    string `json:"phone,omitempty"`
}

type LoginRequest struct {
	Username string `json:"username"`
	Password string `json:"password"`
}

type AuthResponse struct {
	Token     string `json:"token"`
	ExpiresAt int64  `json:"expiresAt"`
	User      *User  `json:"user"`
}

func (s *Service) Register(req *RegisterRequest) (*AuthResponse, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	if _, exists := s.usersByEmail[req.Email]; exists {
		return nil, ErrUserExists
	}
	if _, exists := s.usersByUsername[req.Username]; exists {
		return nil, ErrUserExists
	}

	hashedPassword, err := bcrypt.GenerateFromPassword([]byte(req.Password), bcrypt.DefaultCost)
	if err != nil {
		return nil, fmt.Errorf("failed to hash password: %w", err)
	}

	user := NewUser(req.Username, req.Email, string(hashedPassword))
	user.ID = uuid.New().String()
	user.Phone = req.Phone

	s.users[user.ID] = user
	s.usersByEmail[user.Email] = user
	s.usersByUsername[user.Username] = user

	s.stats[user.ID] = &UserStats{UserID: user.ID}

	token, expiresAt, err := s.generateToken(user)
	if err != nil {
		return nil, err
	}

	return &AuthResponse{
		Token:     token,
		ExpiresAt: expiresAt,
		User:      user,
	}, nil
}

func (s *Service) Login(req *LoginRequest) (*AuthResponse, error) {
	s.mu.RLock()
	user, exists := s.usersByUsername[req.Username]
	if !exists {
		user, exists = s.usersByEmail[req.Username]
	}
	s.mu.RUnlock()

	if !exists {
		return nil, ErrUserNotFound
	}

	if err := bcrypt.CompareHashAndPassword([]byte(user.PasswordHash), []byte(req.Password)); err != nil {
		return nil, ErrInvalidPassword
	}

	s.mu.Lock()
	user.LastLoginAt = time.Now()
	s.mu.Unlock()

	token, expiresAt, err := s.generateToken(user)
	if err != nil {
		return nil, err
	}

	return &AuthResponse{
		Token:     token,
		ExpiresAt: expiresAt,
		User:      user,
	}, nil
}

func (s *Service) LoginAsGuest() (*AuthResponse, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	guestID := uuid.New().String()[:8]
	user := &User{
		ID:          uuid.New().String(),
		Username:    "Guest_" + guestID,
		Nickname:    "Guest_" + guestID,
		Avatar:      "default",
		Level:       1,
		Chips:       5000,
		Status:      StatusNormal,
		CreatedAt:   time.Now(),
		UpdatedAt:   time.Now(),
		LastLoginAt: time.Now(),
	}

	s.users[user.ID] = user
	s.stats[user.ID] = &UserStats{UserID: user.ID}

	token, expiresAt, err := s.generateToken(user)
	if err != nil {
		return nil, err
	}

	return &AuthResponse{
		Token:     token,
		ExpiresAt: expiresAt,
		User:      user,
	}, nil
}

func (s *Service) generateToken(user *User) (string, int64, error) {
	expiresAt := time.Now().Add(24 * time.Hour).Unix()

	claims := jwt.MapClaims{
		"sub":      user.ID,
		"username": user.Username,
		"exp":      expiresAt,
		"iat":      time.Now().Unix(),
	}

	token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
	tokenString, err := token.SignedString(s.jwtSecret)
	if err != nil {
		return "", 0, err
	}

	return tokenString, expiresAt, nil
}

func (s *Service) ValidateToken(tokenString string) (*User, error) {
	token, err := jwt.Parse(tokenString, func(token *jwt.Token) (interface{}, error) {
		if _, ok := token.Method.(*jwt.SigningMethodHMAC); !ok {
			return nil, ErrInvalidToken
		}
		return s.jwtSecret, nil
	})

	if err != nil || !token.Valid {
		return nil, ErrInvalidToken
	}

	claims, ok := token.Claims.(jwt.MapClaims)
	if !ok {
		return nil, ErrInvalidToken
	}

	userID, ok := claims["sub"].(string)
	if !ok {
		return nil, ErrInvalidToken
	}

	return s.GetUser(userID)
}

func (s *Service) GetUser(userID string) (*User, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	user, exists := s.users[userID]
	if !exists {
		return nil, ErrUserNotFound
	}
	return user, nil
}

func (s *Service) UpdateUser(userID string, updates map[string]interface{}) (*User, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	user, exists := s.users[userID]
	if !exists {
		return nil, ErrUserNotFound
	}

	if nickname, ok := updates["nickname"].(string); ok {
		user.Nickname = nickname
	}
	if avatar, ok := updates["avatar"].(string); ok {
		user.Avatar = avatar
	}

	user.UpdatedAt = time.Now()
	return user, nil
}

func (s *Service) GetUserStats(userID string) (*UserStats, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	stats, exists := s.stats[userID]
	if !exists {
		return nil, ErrUserNotFound
	}
	return stats, nil
}

func (s *Service) UpdateStats(userID string, won bool, amount int64) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	stats, exists := s.stats[userID]
	if !exists {
		return ErrUserNotFound
	}

	stats.HandsPlayed++
	if won {
		stats.HandsWon++
		stats.TotalWinnings += amount
	} else {
		stats.TotalLosses += amount
	}

	if amount > stats.BiggestPot {
		stats.BiggestPot = amount
	}

	if stats.HandsPlayed > 0 {
		stats.WinRate = float64(stats.HandsWon) / float64(stats.HandsPlayed)
	}

	return nil
}

func (s *Service) AddChips(userID string, amount int64, reason string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	user, exists := s.users[userID]
	if !exists {
		return ErrUserNotFound
	}

	user.AddChips(amount)
	return nil
}

func (s *Service) DeductChips(userID string, amount int64, reason string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	user, exists := s.users[userID]
	if !exists {
		return ErrUserNotFound
	}

	if !user.DeductChips(amount) {
		return ErrInsufficientChips
	}
	return nil
}

func (s *Service) GetLeaderboard(limit int) []*User {
	s.mu.RLock()
	defer s.mu.RUnlock()

	users := make([]*User, 0, len(s.users))
	for _, u := range s.users {
		users = append(users, u)
	}

	for i := 0; i < len(users)-1; i++ {
		for j := i + 1; j < len(users); j++ {
			if users[j].Chips > users[i].Chips {
				users[i], users[j] = users[j], users[i]
			}
		}
	}

	if limit > 0 && len(users) > limit {
		users = users[:limit]
	}

	return users
}

func (s *Service) DailyLogin(userID string) (int64, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	user, exists := s.users[userID]
	if !exists {
		return 0, ErrUserNotFound
	}

	now := time.Now()
	lastLogin := user.LastLoginAt

	if now.Year() == lastLogin.Year() && now.YearDay() == lastLogin.YearDay() {
		return 0, nil
	}

	reward := int64(100 + user.Level*10)
	user.AddChips(reward)
	user.LastLoginAt = now

	return reward, nil
}

func (s *Service) ClaimBankruptcy(userID string) (int64, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	user, exists := s.users[userID]
	if !exists {
		return 0, ErrUserNotFound
	}

	if user.Chips >= 100 {
		return 0, errors.New("not bankrupt")
	}

	bonus := int64(1000)
	user.Chips = bonus
	user.UpdatedAt = time.Now()

	return bonus, nil
}
