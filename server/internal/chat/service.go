package chat

import (
	"sync"
	"time"
)

type MessageType int

const (
	MessageTypeText MessageType = iota
	MessageTypeEmoji
	MessageTypeQuickChat
	MessageTypeSystem
)

type ChatMessage struct {
	ID         string      `json:"id"`
	RoomID     string      `json:"roomId"`
	SenderID   string      `json:"senderId"`
	SenderName string      `json:"senderName"`
	Type       MessageType `json:"type"`
	Content    string      `json:"content"`
	Timestamp  int64       `json:"timestamp"`
}

type QuickChatOption struct {
	ID   string `json:"id"`
	Text string `json:"text"`
}

type Emoji struct {
	ID       string `json:"id"`
	Name     string `json:"name"`
	Category string `json:"category"`
	IsVIP    bool   `json:"isVip"`
}

type Service struct {
	roomMessages map[string][]*ChatMessage
	quickChats   []QuickChatOption
	emojis       []Emoji
	maxMessages  int
	mu           sync.RWMutex
}

func NewService() *Service {
	s := &Service{
		roomMessages: make(map[string][]*ChatMessage),
		maxMessages:  100,
	}
	s.initQuickChats()
	s.initEmojis()
	return s
}

func (s *Service) initQuickChats() {
	s.quickChats = []QuickChatOption{
		{ID: "hello", Text: "大家好！"},
		{ID: "gl", Text: "祝你好运！"},
		{ID: "gg", Text: "打得好！"},
		{ID: "nh", Text: "好牌！"},
		{ID: "ty", Text: "谢谢！"},
		{ID: "hurry", Text: "快点啊！"},
		{ID: "wp", Text: "打得漂亮！"},
		{ID: "bluff", Text: "你在诈唬！"},
		{ID: "sorry", Text: "抱歉！"},
		{ID: "bye", Text: "再见！"},
	}
}

func (s *Service) initEmojis() {
	s.emojis = []Emoji{
		// Basic emotions
		{ID: "smile", Name: "微笑", Category: "emotion", IsVIP: false},
		{ID: "laugh", Name: "大笑", Category: "emotion", IsVIP: false},
		{ID: "cry", Name: "哭", Category: "emotion", IsVIP: false},
		{ID: "angry", Name: "生气", Category: "emotion", IsVIP: false},
		{ID: "cool", Name: "酷", Category: "emotion", IsVIP: false},
		{ID: "think", Name: "思考", Category: "emotion", IsVIP: false},
		{ID: "sweat", Name: "汗", Category: "emotion", IsVIP: false},
		{ID: "shock", Name: "惊讶", Category: "emotion", IsVIP: false},

		// Poker specific
		{ID: "chips", Name: "筹码", Category: "poker", IsVIP: false},
		{ID: "cards", Name: "扑克牌", Category: "poker", IsVIP: false},
		{ID: "allin", Name: "全下", Category: "poker", IsVIP: false},
		{ID: "fold", Name: "弃牌", Category: "poker", IsVIP: false},
		{ID: "bluff", Name: "诈唬", Category: "poker", IsVIP: false},
		{ID: "nuts", Name: "坚果", Category: "poker", IsVIP: false},

		// VIP emojis
		{ID: "fireworks", Name: "烟花", Category: "vip", IsVIP: true},
		{ID: "crown", Name: "皇冠", Category: "vip", IsVIP: true},
		{ID: "diamond", Name: "钻石", Category: "vip", IsVIP: true},
		{ID: "trophy", Name: "奖杯", Category: "vip", IsVIP: true},
		{ID: "rocket", Name: "火箭", Category: "vip", IsVIP: true},
		{ID: "rainbow", Name: "彩虹", Category: "vip", IsVIP: true},
	}
}

func (s *Service) SendMessage(roomID, senderID, senderName, content string, msgType MessageType) *ChatMessage {
	s.mu.Lock()
	defer s.mu.Unlock()

	msg := &ChatMessage{
		ID:         generateMessageID(),
		RoomID:     roomID,
		SenderID:   senderID,
		SenderName: senderName,
		Type:       msgType,
		Content:    content,
		Timestamp:  time.Now().UnixMilli(),
	}

	if s.roomMessages[roomID] == nil {
		s.roomMessages[roomID] = make([]*ChatMessage, 0)
	}

	s.roomMessages[roomID] = append(s.roomMessages[roomID], msg)

	// Trim old messages
	if len(s.roomMessages[roomID]) > s.maxMessages {
		s.roomMessages[roomID] = s.roomMessages[roomID][1:]
	}

	return msg
}

func (s *Service) SendQuickChat(roomID, senderID, senderName, quickChatID string) *ChatMessage {
	text := ""
	for _, qc := range s.quickChats {
		if qc.ID == quickChatID {
			text = qc.Text
			break
		}
	}

	if text == "" {
		return nil
	}

	return s.SendMessage(roomID, senderID, senderName, text, MessageTypeQuickChat)
}

func (s *Service) SendEmoji(roomID, senderID, senderName, emojiID string, isVIP bool) *ChatMessage {
	// Check if emoji exists and VIP requirement
	var emoji *Emoji
	for _, e := range s.emojis {
		if e.ID == emojiID {
			emoji = &e
			break
		}
	}

	if emoji == nil {
		return nil
	}

	if emoji.IsVIP && !isVIP {
		return nil
	}

	return s.SendMessage(roomID, senderID, senderName, emojiID, MessageTypeEmoji)
}

func (s *Service) SendSystemMessage(roomID, content string) *ChatMessage {
	return s.SendMessage(roomID, "system", "系统", content, MessageTypeSystem)
}

func (s *Service) GetRoomMessages(roomID string, limit int) []*ChatMessage {
	s.mu.RLock()
	defer s.mu.RUnlock()

	messages := s.roomMessages[roomID]
	if messages == nil {
		return []*ChatMessage{}
	}

	if limit <= 0 || limit > len(messages) {
		limit = len(messages)
	}

	start := len(messages) - limit
	result := make([]*ChatMessage, limit)
	copy(result, messages[start:])

	return result
}

func (s *Service) GetQuickChats() []QuickChatOption {
	return s.quickChats
}

func (s *Service) GetEmojis(includeVIP bool) []Emoji {
	if includeVIP {
		return s.emojis
	}

	result := make([]Emoji, 0)
	for _, e := range s.emojis {
		if !e.IsVIP {
			result = append(result, e)
		}
	}
	return result
}

func (s *Service) ClearRoomMessages(roomID string) {
	s.mu.Lock()
	defer s.mu.Unlock()
	delete(s.roomMessages, roomID)
}

func generateMessageID() string {
	return time.Now().Format("20060102150405.000")
}
