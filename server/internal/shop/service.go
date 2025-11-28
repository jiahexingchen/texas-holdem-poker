package shop

import (
	"errors"
	"sync"
	"time"
)

var (
	ErrItemNotFound       = errors.New("item not found")
	ErrInsufficientFunds  = errors.New("insufficient funds")
	ErrAlreadyOwned       = errors.New("item already owned")
	ErrItemExpired        = errors.New("item has expired")
)

type ItemType string

const (
	ItemTypeAvatar     ItemType = "avatar"
	ItemTypeAvatarFrame ItemType = "avatar_frame"
	ItemTypeCardBack   ItemType = "card_back"
	ItemTypeTableTheme ItemType = "table_theme"
	ItemTypeEmoji      ItemType = "emoji"
	ItemTypeChips      ItemType = "chips"
	ItemTypeVIP        ItemType = "vip"
)

type CurrencyType string

const (
	CurrencyChips    CurrencyType = "chips"
	CurrencyDiamonds CurrencyType = "diamonds"
)

type ShopItem struct {
	ID           string       `json:"id"`
	Name         string       `json:"name"`
	Description  string       `json:"description"`
	ItemType     ItemType     `json:"itemType"`
	Price        int64        `json:"price"`
	Currency     CurrencyType `json:"currency"`
	Icon         string       `json:"icon"`
	Rarity       string       `json:"rarity"` // common, rare, epic, legendary
	Duration     int          `json:"duration"` // 0 for permanent, days for temporary
	IsLimited    bool         `json:"isLimited"`
	Stock        int          `json:"stock"` // -1 for unlimited
	SoldCount    int          `json:"soldCount"`
	StartTime    time.Time    `json:"startTime,omitempty"`
	EndTime      time.Time    `json:"endTime,omitempty"`
	Discount     int          `json:"discount"` // percentage off
}

type UserItem struct {
	UserID    string    `json:"userId"`
	ItemID    string    `json:"itemId"`
	ItemType  ItemType  `json:"itemType"`
	Quantity  int       `json:"quantity"`
	Equipped  bool      `json:"equipped"`
	ExpiresAt time.Time `json:"expiresAt,omitempty"`
	AcquiredAt time.Time `json:"acquiredAt"`
}

type PurchaseRecord struct {
	ID         string       `json:"id"`
	UserID     string       `json:"userId"`
	ItemID     string       `json:"itemId"`
	Price      int64        `json:"price"`
	Currency   CurrencyType `json:"currency"`
	Quantity   int          `json:"quantity"`
	CreatedAt  time.Time    `json:"createdAt"`
}

type Service struct {
	items     map[string]*ShopItem
	userItems map[string]map[string]*UserItem
	purchases []PurchaseRecord
	mu        sync.RWMutex
}

func NewService() *Service {
	s := &Service{
		items:     make(map[string]*ShopItem),
		userItems: make(map[string]map[string]*UserItem),
		purchases: make([]PurchaseRecord, 0),
	}
	s.initDefaultItems()
	return s
}

func (s *Service) initDefaultItems() {
	items := []*ShopItem{
		// Avatars
		{
			ID:          "avatar_classic",
			Name:        "经典头像",
			Description: "经典扑克玩家头像",
			ItemType:    ItemTypeAvatar,
			Price:       500,
			Currency:    CurrencyChips,
			Icon:        "avatar_classic",
			Rarity:      "common",
			Stock:       -1,
		},
		{
			ID:          "avatar_vip",
			Name:        "VIP头像",
			Description: "尊贵VIP专属头像",
			ItemType:    ItemTypeAvatar,
			Price:       100,
			Currency:    CurrencyDiamonds,
			Icon:        "avatar_vip",
			Rarity:      "epic",
			Stock:       -1,
		},

		// Avatar Frames
		{
			ID:          "frame_gold",
			Name:        "金色边框",
			Description: "闪耀的金色头像边框",
			ItemType:    ItemTypeAvatarFrame,
			Price:       2000,
			Currency:    CurrencyChips,
			Icon:        "frame_gold",
			Rarity:      "rare",
			Stock:       -1,
		},
		{
			ID:          "frame_diamond",
			Name:        "钻石边框",
			Description: "璀璨钻石头像边框",
			ItemType:    ItemTypeAvatarFrame,
			Price:       200,
			Currency:    CurrencyDiamonds,
			Icon:        "frame_diamond",
			Rarity:      "legendary",
			Stock:       -1,
		},

		// Card Backs
		{
			ID:          "card_red",
			Name:        "经典红色",
			Description: "经典红色卡背",
			ItemType:    ItemTypeCardBack,
			Price:       0,
			Currency:    CurrencyChips,
			Icon:        "card_red",
			Rarity:      "common",
			Stock:       -1,
		},
		{
			ID:          "card_blue",
			Name:        "深邃蓝色",
			Description: "深邃蓝色卡背",
			ItemType:    ItemTypeCardBack,
			Price:       1000,
			Currency:    CurrencyChips,
			Icon:        "card_blue",
			Rarity:      "common",
			Stock:       -1,
		},
		{
			ID:          "card_gold",
			Name:        "奢华金色",
			Description: "奢华金色卡背",
			ItemType:    ItemTypeCardBack,
			Price:       5000,
			Currency:    CurrencyChips,
			Icon:        "card_gold",
			Rarity:      "rare",
			Stock:       -1,
		},
		{
			ID:          "card_dragon",
			Name:        "龙纹卡背",
			Description: "神秘龙纹卡背",
			ItemType:    ItemTypeCardBack,
			Price:       500,
			Currency:    CurrencyDiamonds,
			Icon:        "card_dragon",
			Rarity:      "legendary",
			Stock:       -1,
		},

		// Table Themes
		{
			ID:          "table_green",
			Name:        "经典绿色",
			Description: "经典绿色牌桌",
			ItemType:    ItemTypeTableTheme,
			Price:       0,
			Currency:    CurrencyChips,
			Icon:        "table_green",
			Rarity:      "common",
			Stock:       -1,
		},
		{
			ID:          "table_blue",
			Name:        "深蓝牌桌",
			Description: "深蓝色豪华牌桌",
			ItemType:    ItemTypeTableTheme,
			Price:       3000,
			Currency:    CurrencyChips,
			Icon:        "table_blue",
			Rarity:      "rare",
			Stock:       -1,
		},
		{
			ID:          "table_red",
			Name:        "皇家红色",
			Description: "皇家红色牌桌",
			ItemType:    ItemTypeTableTheme,
			Price:       5000,
			Currency:    CurrencyChips,
			Icon:        "table_red",
			Rarity:      "rare",
			Stock:       -1,
		},

		// Chips packages
		{
			ID:          "chips_small",
			Name:        "小额筹码",
			Description: "获得10,000筹码",
			ItemType:    ItemTypeChips,
			Price:       10,
			Currency:    CurrencyDiamonds,
			Icon:        "chips_small",
			Rarity:      "common",
			Stock:       -1,
		},
		{
			ID:          "chips_medium",
			Name:        "中额筹码",
			Description: "获得50,000筹码",
			ItemType:    ItemTypeChips,
			Price:       45,
			Currency:    CurrencyDiamonds,
			Icon:        "chips_medium",
			Rarity:      "common",
			Stock:       -1,
		},
		{
			ID:          "chips_large",
			Name:        "大额筹码",
			Description: "获得120,000筹码",
			ItemType:    ItemTypeChips,
			Price:       100,
			Currency:    CurrencyDiamonds,
			Icon:        "chips_large",
			Rarity:      "common",
			Stock:       -1,
		},
	}

	for _, item := range items {
		s.items[item.ID] = item
	}
}

func (s *Service) GetAllItems() []*ShopItem {
	s.mu.RLock()
	defer s.mu.RUnlock()

	now := time.Now()
	result := make([]*ShopItem, 0)

	for _, item := range s.items {
		// Check if limited time item is available
		if item.IsLimited {
			if !item.StartTime.IsZero() && now.Before(item.StartTime) {
				continue
			}
			if !item.EndTime.IsZero() && now.After(item.EndTime) {
				continue
			}
		}

		// Check stock
		if item.Stock == 0 {
			continue
		}

		result = append(result, item)
	}

	return result
}

func (s *Service) GetItemsByType(itemType ItemType) []*ShopItem {
	s.mu.RLock()
	defer s.mu.RUnlock()

	result := make([]*ShopItem, 0)
	for _, item := range s.items {
		if item.ItemType == itemType {
			result = append(result, item)
		}
	}
	return result
}

func (s *Service) GetItem(itemID string) *ShopItem {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return s.items[itemID]
}

func (s *Service) Purchase(userID, itemID string, getBalance func(CurrencyType) int64, deductBalance func(CurrencyType, int64) error) (*UserItem, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	item := s.items[itemID]
	if item == nil {
		return nil, ErrItemNotFound
	}

	// Check stock
	if item.Stock == 0 {
		return nil, ErrItemNotFound
	}

	// Check if already owned (for non-consumable items)
	if item.ItemType != ItemTypeChips {
		if s.userItems[userID] != nil {
			if _, exists := s.userItems[userID][itemID]; exists {
				return nil, ErrAlreadyOwned
			}
		}
	}

	// Calculate price with discount
	price := item.Price
	if item.Discount > 0 {
		price = price * int64(100-item.Discount) / 100
	}

	// Check balance
	if getBalance(item.Currency) < price {
		return nil, ErrInsufficientFunds
	}

	// Deduct balance
	if err := deductBalance(item.Currency, price); err != nil {
		return nil, err
	}

	// Create user item
	if s.userItems[userID] == nil {
		s.userItems[userID] = make(map[string]*UserItem)
	}

	var expiresAt time.Time
	if item.Duration > 0 {
		expiresAt = time.Now().AddDate(0, 0, item.Duration)
	}

	userItem := &UserItem{
		UserID:     userID,
		ItemID:     itemID,
		ItemType:   item.ItemType,
		Quantity:   1,
		Equipped:   false,
		ExpiresAt:  expiresAt,
		AcquiredAt: time.Now(),
	}

	s.userItems[userID][itemID] = userItem

	// Update stock
	if item.Stock > 0 {
		item.Stock--
	}
	item.SoldCount++

	// Record purchase
	s.purchases = append(s.purchases, PurchaseRecord{
		ID:        time.Now().Format("20060102150405"),
		UserID:    userID,
		ItemID:    itemID,
		Price:     price,
		Currency:  item.Currency,
		Quantity:  1,
		CreatedAt: time.Now(),
	})

	return userItem, nil
}

func (s *Service) GetUserItems(userID string) []*UserItem {
	s.mu.RLock()
	defer s.mu.RUnlock()

	if s.userItems[userID] == nil {
		return []*UserItem{}
	}

	result := make([]*UserItem, 0)
	now := time.Now()

	for _, item := range s.userItems[userID] {
		// Skip expired items
		if !item.ExpiresAt.IsZero() && now.After(item.ExpiresAt) {
			continue
		}
		result = append(result, item)
	}

	return result
}

func (s *Service) EquipItem(userID, itemID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.userItems[userID] == nil {
		return ErrItemNotFound
	}

	userItem, exists := s.userItems[userID][itemID]
	if !exists {
		return ErrItemNotFound
	}

	// Check if expired
	if !userItem.ExpiresAt.IsZero() && time.Now().After(userItem.ExpiresAt) {
		return ErrItemExpired
	}

	// Unequip other items of same type
	item := s.items[itemID]
	for _, ui := range s.userItems[userID] {
		if s.items[ui.ItemID].ItemType == item.ItemType {
			ui.Equipped = false
		}
	}

	userItem.Equipped = true
	return nil
}

func (s *Service) UnequipItem(userID, itemID string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if s.userItems[userID] == nil {
		return ErrItemNotFound
	}

	userItem, exists := s.userItems[userID][itemID]
	if !exists {
		return ErrItemNotFound
	}

	userItem.Equipped = false
	return nil
}

func (s *Service) GetEquippedItems(userID string) map[ItemType]*UserItem {
	s.mu.RLock()
	defer s.mu.RUnlock()

	equipped := make(map[ItemType]*UserItem)

	if s.userItems[userID] == nil {
		return equipped
	}

	for _, item := range s.userItems[userID] {
		if item.Equipped {
			equipped[item.ItemType] = item
		}
	}

	return equipped
}
