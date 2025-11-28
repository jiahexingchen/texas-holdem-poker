package user

import (
	"errors"
	"sync"
	"time"
)

var (
	ErrAlreadyFriends = errors.New("already friends")
	ErrRequestPending = errors.New("friend request already pending")
	ErrCannotAddSelf  = errors.New("cannot add yourself as friend")
	ErrBlocked        = errors.New("user is blocked")
)

type FriendService struct {
	friendships map[string]map[string]*Friendship
	userService *Service
	mu          sync.RWMutex
}

func NewFriendService(userService *Service) *FriendService {
	return &FriendService{
		friendships: make(map[string]map[string]*Friendship),
		userService: userService,
	}
}

func (s *FriendService) SendFriendRequest(userID, friendID string) error {
	if userID == friendID {
		return ErrCannotAddSelf
	}

	s.mu.Lock()
	defer s.mu.Unlock()

	// Check if already friends or request pending
	if s.hasFriendship(userID, friendID) {
		existing := s.getFriendship(userID, friendID)
		switch existing.Status {
		case FriendAccepted:
			return ErrAlreadyFriends
		case FriendPending:
			return ErrRequestPending
		case FriendBlocked:
			return ErrBlocked
		}
	}

	// Check reverse direction
	if s.hasFriendship(friendID, userID) {
		existing := s.getFriendship(friendID, userID)
		if existing.Status == FriendBlocked {
			return ErrBlocked
		}
		if existing.Status == FriendPending {
			// Auto-accept if both sent requests
			s.acceptFriendshipLocked(friendID, userID)
			return nil
		}
	}

	// Create new friendship request
	friendship := &Friendship{
		UserID:    userID,
		FriendID:  friendID,
		Status:    FriendPending,
		CreatedAt: time.Now(),
	}

	if s.friendships[userID] == nil {
		s.friendships[userID] = make(map[string]*Friendship)
	}
	s.friendships[userID][friendID] = friendship

	return nil
}

func (s *FriendService) AcceptFriendRequest(userID, friendID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	// Check if there's a pending request from friendID to userID
	if !s.hasFriendship(friendID, userID) {
		return errors.New("no pending friend request")
	}

	existing := s.getFriendship(friendID, userID)
	if existing.Status != FriendPending {
		return errors.New("no pending friend request")
	}

	s.acceptFriendshipLocked(friendID, userID)
	return nil
}

func (s *FriendService) acceptFriendshipLocked(requesterID, accepterID string) {
	// Update requester's friendship
	if s.friendships[requesterID] != nil {
		if f := s.friendships[requesterID][accepterID]; f != nil {
			f.Status = FriendAccepted
		}
	}

	// Create reverse friendship
	if s.friendships[accepterID] == nil {
		s.friendships[accepterID] = make(map[string]*Friendship)
	}
	s.friendships[accepterID][requesterID] = &Friendship{
		UserID:    accepterID,
		FriendID:  requesterID,
		Status:    FriendAccepted,
		CreatedAt: time.Now(),
	}
}

func (s *FriendService) RejectFriendRequest(userID, friendID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if !s.hasFriendship(friendID, userID) {
		return errors.New("no pending friend request")
	}

	delete(s.friendships[friendID], userID)
	return nil
}

func (s *FriendService) RemoveFriend(userID, friendID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.friendships[userID] != nil {
		delete(s.friendships[userID], friendID)
	}
	if s.friendships[friendID] != nil {
		delete(s.friendships[friendID], userID)
	}

	return nil
}

func (s *FriendService) BlockUser(userID, blockedID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.friendships[userID] == nil {
		s.friendships[userID] = make(map[string]*Friendship)
	}

	s.friendships[userID][blockedID] = &Friendship{
		UserID:    userID,
		FriendID:  blockedID,
		Status:    FriendBlocked,
		CreatedAt: time.Now(),
	}

	// Remove from blocked user's friend list
	if s.friendships[blockedID] != nil {
		delete(s.friendships[blockedID], userID)
	}

	return nil
}

func (s *FriendService) UnblockUser(userID, blockedID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.friendships[userID] != nil {
		if f := s.friendships[userID][blockedID]; f != nil && f.Status == FriendBlocked {
			delete(s.friendships[userID], blockedID)
		}
	}

	return nil
}

func (s *FriendService) GetFriends(userID string) ([]*User, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	friends := make([]*User, 0)

	if s.friendships[userID] == nil {
		return friends, nil
	}

	for friendID, f := range s.friendships[userID] {
		if f.Status == FriendAccepted {
			user, err := s.userService.GetUser(friendID)
			if err == nil {
				friends = append(friends, user)
			}
		}
	}

	return friends, nil
}

func (s *FriendService) GetPendingRequests(userID string) ([]*FriendRequest, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	requests := make([]*FriendRequest, 0)

	// Find requests TO this user
	for requesterID, friendMap := range s.friendships {
		if f, ok := friendMap[userID]; ok && f.Status == FriendPending {
			user, err := s.userService.GetUser(requesterID)
			if err == nil {
				requests = append(requests, &FriendRequest{
					UserID:    requesterID,
					Nickname:  user.Nickname,
					Avatar:    user.Avatar,
					Level:     user.Level,
					CreatedAt: f.CreatedAt,
				})
			}
		}
	}

	return requests, nil
}

func (s *FriendService) GetBlockedUsers(userID string) ([]*User, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()

	blocked := make([]*User, 0)

	if s.friendships[userID] == nil {
		return blocked, nil
	}

	for blockedID, f := range s.friendships[userID] {
		if f.Status == FriendBlocked {
			user, err := s.userService.GetUser(blockedID)
			if err == nil {
				blocked = append(blocked, user)
			}
		}
	}

	return blocked, nil
}

func (s *FriendService) AreFriends(userID, friendID string) bool {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.friendships[userID] == nil {
		return false
	}

	f := s.friendships[userID][friendID]
	return f != nil && f.Status == FriendAccepted
}

func (s *FriendService) IsBlocked(userID, targetID string) bool {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.friendships[userID] == nil {
		return false
	}

	f := s.friendships[userID][targetID]
	return f != nil && f.Status == FriendBlocked
}

func (s *FriendService) hasFriendship(userID, friendID string) bool {
	if s.friendships[userID] == nil {
		return false
	}
	_, exists := s.friendships[userID][friendID]
	return exists
}

func (s *FriendService) getFriendship(userID, friendID string) *Friendship {
	if s.friendships[userID] == nil {
		return nil
	}
	return s.friendships[userID][friendID]
}

type FriendRequest struct {
	UserID    string    `json:"userId"`
	Nickname  string    `json:"nickname"`
	Avatar    string    `json:"avatar"`
	Level     int       `json:"level"`
	CreatedAt time.Time `json:"createdAt"`
}

func (s *FriendService) GetFriendCount(userID string) int {
	s.mu.RLock()
	defer s.mu.RUnlock()

	count := 0
	if s.friendships[userID] != nil {
		for _, f := range s.friendships[userID] {
			if f.Status == FriendAccepted {
				count++
			}
		}
	}
	return count
}
