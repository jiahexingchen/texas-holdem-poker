package user

import (
	"encoding/json"
	"net/http"
	"strings"
)

type Handler struct {
	service *Service
}

func NewHandler(service *Service) *Handler {
	return &Handler{service: service}
}

func (h *Handler) RegisterRoutes(mux *http.ServeMux) {
	mux.HandleFunc("/api/auth/register", h.handleRegister)
	mux.HandleFunc("/api/auth/login", h.handleLogin)
	mux.HandleFunc("/api/auth/guest", h.handleGuestLogin)
	mux.HandleFunc("/api/user/profile", h.authMiddleware(h.handleProfile))
	mux.HandleFunc("/api/user/stats", h.authMiddleware(h.handleStats))
	mux.HandleFunc("/api/user/chips", h.authMiddleware(h.handleChips))
	mux.HandleFunc("/api/user/daily", h.authMiddleware(h.handleDailyLogin))
	mux.HandleFunc("/api/user/bankruptcy", h.authMiddleware(h.handleBankruptcy))
	mux.HandleFunc("/api/leaderboard", h.handleLeaderboard)
}

func (h *Handler) handleRegister(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req RegisterRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.jsonError(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	if req.Username == "" || req.Email == "" || req.Password == "" {
		h.jsonError(w, "Username, email and password are required", http.StatusBadRequest)
		return
	}

	if len(req.Password) < 6 {
		h.jsonError(w, "Password must be at least 6 characters", http.StatusBadRequest)
		return
	}

	resp, err := h.service.Register(&req)
	if err != nil {
		if err == ErrUserExists {
			h.jsonError(w, "User already exists", http.StatusConflict)
			return
		}
		h.jsonError(w, err.Error(), http.StatusInternalServerError)
		return
	}

	h.jsonResponse(w, resp, http.StatusCreated)
}

func (h *Handler) handleLogin(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req LoginRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		h.jsonError(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	resp, err := h.service.Login(&req)
	if err != nil {
		if err == ErrUserNotFound || err == ErrInvalidPassword {
			h.jsonError(w, "Invalid username or password", http.StatusUnauthorized)
			return
		}
		h.jsonError(w, err.Error(), http.StatusInternalServerError)
		return
	}

	h.jsonResponse(w, resp, http.StatusOK)
}

func (h *Handler) handleGuestLogin(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	resp, err := h.service.LoginAsGuest()
	if err != nil {
		h.jsonError(w, err.Error(), http.StatusInternalServerError)
		return
	}

	h.jsonResponse(w, resp, http.StatusOK)
}

func (h *Handler) handleProfile(w http.ResponseWriter, r *http.Request, user *User) {
	switch r.Method {
	case http.MethodGet:
		h.jsonResponse(w, user, http.StatusOK)

	case http.MethodPut:
		var updates map[string]interface{}
		if err := json.NewDecoder(r.Body).Decode(&updates); err != nil {
			h.jsonError(w, "Invalid request body", http.StatusBadRequest)
			return
		}

		updated, err := h.service.UpdateUser(user.ID, updates)
		if err != nil {
			h.jsonError(w, err.Error(), http.StatusInternalServerError)
			return
		}

		h.jsonResponse(w, updated, http.StatusOK)

	default:
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
	}
}

func (h *Handler) handleStats(w http.ResponseWriter, r *http.Request, user *User) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	stats, err := h.service.GetUserStats(user.ID)
	if err != nil {
		h.jsonError(w, err.Error(), http.StatusInternalServerError)
		return
	}

	h.jsonResponse(w, stats, http.StatusOK)
}

func (h *Handler) handleChips(w http.ResponseWriter, r *http.Request, user *User) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	h.jsonResponse(w, map[string]interface{}{
		"chips":    user.Chips,
		"diamonds": user.Diamonds,
	}, http.StatusOK)
}

func (h *Handler) handleDailyLogin(w http.ResponseWriter, r *http.Request, user *User) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	reward, err := h.service.DailyLogin(user.ID)
	if err != nil {
		h.jsonError(w, err.Error(), http.StatusInternalServerError)
		return
	}

	h.jsonResponse(w, map[string]interface{}{
		"reward":    reward,
		"newChips":  user.Chips,
		"claimed":   reward > 0,
	}, http.StatusOK)
}

func (h *Handler) handleBankruptcy(w http.ResponseWriter, r *http.Request, user *User) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	bonus, err := h.service.ClaimBankruptcy(user.ID)
	if err != nil {
		h.jsonError(w, err.Error(), http.StatusBadRequest)
		return
	}

	h.jsonResponse(w, map[string]interface{}{
		"bonus":    bonus,
		"newChips": user.Chips,
	}, http.StatusOK)
}

func (h *Handler) handleLeaderboard(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	users := h.service.GetLeaderboard(100)

	leaderboard := make([]map[string]interface{}, 0, len(users))
	for i, u := range users {
		leaderboard = append(leaderboard, map[string]interface{}{
			"rank":     i + 1,
			"id":       u.ID,
			"nickname": u.Nickname,
			"avatar":   u.Avatar,
			"level":    u.Level,
			"chips":    u.Chips,
		})
	}

	h.jsonResponse(w, leaderboard, http.StatusOK)
}

func (h *Handler) authMiddleware(next func(http.ResponseWriter, *http.Request, *User)) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		authHeader := r.Header.Get("Authorization")
		if authHeader == "" {
			h.jsonError(w, "Authorization header required", http.StatusUnauthorized)
			return
		}

		parts := strings.SplitN(authHeader, " ", 2)
		if len(parts) != 2 || parts[0] != "Bearer" {
			h.jsonError(w, "Invalid authorization header", http.StatusUnauthorized)
			return
		}

		user, err := h.service.ValidateToken(parts[1])
		if err != nil {
			h.jsonError(w, "Invalid or expired token", http.StatusUnauthorized)
			return
		}

		next(w, r, user)
	}
}

func (h *Handler) jsonResponse(w http.ResponseWriter, data interface{}, status int) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	json.NewEncoder(w).Encode(data)
}

func (h *Handler) jsonError(w http.ResponseWriter, message string, status int) {
	h.jsonResponse(w, map[string]string{"error": message}, status)
}
