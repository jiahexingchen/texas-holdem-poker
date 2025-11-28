package config

import (
	"os"
	"strconv"
)

type Config struct {
	ServerAddr    string
	RedisAddr     string
	DatabaseURL   string
	JWTSecret     string
	Environment   string
	
	// Game settings
	DefaultSmallBlind int64
	DefaultBigBlind   int64
	MaxPlayersPerRoom int
	ActionTimeout     int // seconds
	
	// Matchmaking
	MatchmakingTimeout int // seconds
	AIFillDelay        int // seconds before AI fills empty seats
}

func Load() *Config {
	return &Config{
		ServerAddr:         getEnv("SERVER_ADDR", ":8080"),
		RedisAddr:          getEnv("REDIS_ADDR", "localhost:6379"),
		DatabaseURL:        getEnv("DATABASE_URL", "postgres://localhost:5432/texas_holdem"),
		JWTSecret:          getEnv("JWT_SECRET", "your-secret-key-change-in-production"),
		Environment:        getEnv("ENVIRONMENT", "development"),
		DefaultSmallBlind:  getEnvInt64("DEFAULT_SMALL_BLIND", 10),
		DefaultBigBlind:    getEnvInt64("DEFAULT_BIG_BLIND", 20),
		MaxPlayersPerRoom:  getEnvInt("MAX_PLAYERS_PER_ROOM", 9),
		ActionTimeout:      getEnvInt("ACTION_TIMEOUT", 30),
		MatchmakingTimeout: getEnvInt("MATCHMAKING_TIMEOUT", 60),
		AIFillDelay:        getEnvInt("AI_FILL_DELAY", 10),
	}
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}

func getEnvInt(key string, defaultValue int) int {
	if value := os.Getenv(key); value != "" {
		if i, err := strconv.Atoi(value); err == nil {
			return i
		}
	}
	return defaultValue
}

func getEnvInt64(key string, defaultValue int64) int64 {
	if value := os.Getenv(key); value != "" {
		if i, err := strconv.ParseInt(value, 10, 64); err == nil {
			return i
		}
	}
	return defaultValue
}
