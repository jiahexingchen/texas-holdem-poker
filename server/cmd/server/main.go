package main

import (
	"context"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"texas-holdem-server/internal/config"
	"texas-holdem-server/internal/matchmaking"
	"texas-holdem-server/internal/room"
	"texas-holdem-server/internal/user"
	"texas-holdem-server/internal/ws"
)

func main() {
	cfg := config.Load()

	// Initialize services
	hub := ws.NewHub()
	go hub.Run()

	roomManager := room.NewManager(hub)
	userService := user.NewService(cfg.JWTSecret)
	matchService := matchmaking.NewService(roomManager)
	_ = matchService // Will be used later

	wsHandler := ws.NewHandler(hub, roomManager)
	userHandler := user.NewHandler(userService)

	mux := http.NewServeMux()
	
	// WebSocket
	mux.HandleFunc("/ws", wsHandler.HandleWebSocket)
	
	// Health check
	mux.HandleFunc("/health", handleHealth)
	
	// Room API
	mux.HandleFunc("/api/rooms", handleRooms(roomManager))
	
	// User API
	userHandler.RegisterRoutes(mux)

	server := &http.Server{
		Addr:         cfg.ServerAddr,
		Handler:      corsMiddleware(mux),
		ReadTimeout:  15 * time.Second,
		WriteTimeout: 15 * time.Second,
		IdleTimeout:  60 * time.Second,
	}

	go func() {
		log.Printf("Server starting on %s", cfg.ServerAddr)
		if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf("Server error: %v", err)
		}
	}()

	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	log.Println("Shutting down server...")

	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	if err := server.Shutdown(ctx); err != nil {
		log.Fatalf("Server forced to shutdown: %v", err)
	}

	log.Println("Server stopped")
}

func handleHealth(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	w.Write([]byte(`{"status":"ok"}`))
}

func handleRooms(rm *room.Manager) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
			return
		}

		rooms := rm.GetPublicRooms()
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusOK)

		// Simple JSON response
		response := "["
		for i, room := range rooms {
			if i > 0 {
				response += ","
			}
			response += room.ToJSON()
		}
		response += "]"
		w.Write([]byte(response))
	}
}

func corsMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Access-Control-Allow-Origin", "*")
		w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
		w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")

		if r.Method == "OPTIONS" {
			w.WriteHeader(http.StatusOK)
			return
		}

		next.ServeHTTP(w, r)
	})
}
