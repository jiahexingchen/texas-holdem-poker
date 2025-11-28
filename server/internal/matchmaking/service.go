package matchmaking

import (
	"sync"
	"time"

	"texas-holdem-server/internal/room"
)

type BlindLevel int

const (
	BlindLevel1 BlindLevel = iota // 5/10
	BlindLevel2                   // 10/20
	BlindLevel3                   // 25/50
	BlindLevel4                   // 50/100
	BlindLevel5                   // 100/200
	BlindLevel6                   // 250/500
)

type MatchRequest struct {
	PlayerID   string
	PlayerName string
	BlindLevel BlindLevel
	Chips      int64
	RequestAt  time.Time
	Callback   func(roomID string, err error)
}

type Service struct {
	roomManager *room.Manager
	queues      map[BlindLevel][]*MatchRequest
	mu          sync.Mutex
	stopChan    chan struct{}
}

func NewService(roomManager *room.Manager) *Service {
	s := &Service{
		roomManager: roomManager,
		queues:      make(map[BlindLevel][]*MatchRequest),
		stopChan:    make(chan struct{}),
	}

	for i := BlindLevel1; i <= BlindLevel6; i++ {
		s.queues[i] = make([]*MatchRequest, 0)
	}

	go s.processLoop()

	return s
}

func (s *Service) Stop() {
	close(s.stopChan)
}

func (s *Service) EnqueuePlayer(req *MatchRequest) {
	s.mu.Lock()
	defer s.mu.Unlock()

	for _, existing := range s.queues[req.BlindLevel] {
		if existing.PlayerID == req.PlayerID {
			return
		}
	}

	s.queues[req.BlindLevel] = append(s.queues[req.BlindLevel], req)
}

func (s *Service) DequeuePlayer(playerID string, blindLevel BlindLevel) {
	s.mu.Lock()
	defer s.mu.Unlock()

	queue := s.queues[blindLevel]
	for i, req := range queue {
		if req.PlayerID == playerID {
			s.queues[blindLevel] = append(queue[:i], queue[i+1:]...)
			return
		}
	}
}

func (s *Service) processLoop() {
	ticker := time.NewTicker(1 * time.Second)
	defer ticker.Stop()

	for {
		select {
		case <-s.stopChan:
			return
		case <-ticker.C:
			s.processQueues()
		}
	}
}

func (s *Service) processQueues() {
	s.mu.Lock()
	defer s.mu.Unlock()

	for level, queue := range s.queues {
		if len(queue) < 2 {
			s.checkTimeouts(level)
			continue
		}

		blinds := s.getBlinds(level)
		config := room.RoomConfig{
			SmallBlind: blinds.Small,
			BigBlind:   blinds.Big,
			MaxPlayers: 9,
			MinPlayers: 2,
			IsPrivate:  false,
			AutoStart:  true,
		}

		r, err := s.roomManager.CreateRoom(config)
		if err != nil {
			continue
		}

		matched := 0
		maxPlayers := min(len(queue), config.MaxPlayers)
		
		for matched < maxPlayers && len(s.queues[level]) > 0 {
			req := s.queues[level][0]
			s.queues[level] = s.queues[level][1:]

			err := s.roomManager.JoinRoom(r.ID, req.PlayerID, req.PlayerName, req.Chips)
			if err == nil {
				matched++
				if req.Callback != nil {
					go req.Callback(r.ID, nil)
				}
			}
		}
	}
}

func (s *Service) checkTimeouts(level BlindLevel) {
	now := time.Now()
	timeout := 30 * time.Second

	queue := s.queues[level]
	for i := len(queue) - 1; i >= 0; i-- {
		req := queue[i]
		if now.Sub(req.RequestAt) > timeout {
			s.queues[level] = append(queue[:i], queue[i+1:]...)
			
			roomID, err := s.createRoomWithAI(level, req)
			if req.Callback != nil {
				go req.Callback(roomID, err)
			}
		}
	}
}

func (s *Service) createRoomWithAI(level BlindLevel, req *MatchRequest) (string, error) {
	blinds := s.getBlinds(level)
	config := room.RoomConfig{
		SmallBlind: blinds.Small,
		BigBlind:   blinds.Big,
		MaxPlayers: 9,
		MinPlayers: 2,
		IsPrivate:  false,
		AutoStart:  true,
	}

	roomID, err := s.roomManager.QuickMatch(req.PlayerID, req.PlayerName, int(level))
	if err != nil {
		r, err := s.roomManager.CreateRoom(config)
		if err != nil {
			return "", err
		}
		return r.ID, nil
	}

	return roomID, nil
}

type Blinds struct {
	Small int64
	Big   int64
}

func (s *Service) getBlinds(level BlindLevel) Blinds {
	blinds := []Blinds{
		{5, 10},
		{10, 20},
		{25, 50},
		{50, 100},
		{100, 200},
		{250, 500},
	}

	if int(level) < len(blinds) {
		return blinds[level]
	}
	return blinds[0]
}

func (s *Service) GetQueueStatus() map[BlindLevel]int {
	s.mu.Lock()
	defer s.mu.Unlock()

	status := make(map[BlindLevel]int)
	for level, queue := range s.queues {
		status[level] = len(queue)
	}
	return status
}

func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}
