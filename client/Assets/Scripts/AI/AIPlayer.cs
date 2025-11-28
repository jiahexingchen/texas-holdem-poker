using System;
using System.Collections.Generic;
using System.Linq;
using TexasHoldem.Core;
using TexasHoldem.Game;

namespace TexasHoldem.AI
{
    public enum AIDifficulty
    {
        Easy,       // 简单 - 随机策略
        Medium,     // 中等 - 基础牌力评估
        Hard,       // 困难 - 位置意识+范围思考
        Expert      // 专家 - 简化GTO策略
    }

    public class AIDecision
    {
        public PlayerAction Action { get; set; }
        public long Amount { get; set; }
        public float Confidence { get; set; }
        public string Reason { get; set; }
    }

    public class AIPlayer
    {
        private readonly Player _player;
        private readonly AIDifficulty _difficulty;
        private readonly Random _random;

        public string PlayerId => _player.Id;
        public AIDifficulty Difficulty => _difficulty;

        private const float AGGRESSION_FACTOR = 0.3f;
        private const float BLUFF_FREQUENCY = 0.15f;

        public AIPlayer(Player player, AIDifficulty difficulty)
        {
            _player = player;
            _difficulty = difficulty;
            _random = new Random();
        }

        public AIDecision MakeDecision(GameManager game)
        {
            return _difficulty switch
            {
                AIDifficulty.Easy => MakeEasyDecision(game),
                AIDifficulty.Medium => MakeMediumDecision(game),
                AIDifficulty.Hard => MakeHardDecision(game),
                AIDifficulty.Expert => MakeExpertDecision(game),
                _ => MakeEasyDecision(game)
            };
        }

        private AIDecision MakeEasyDecision(GameManager game)
        {
            float roll = (float)_random.NextDouble();

            if (roll < 0.2f)
            {
                return new AIDecision { Action = PlayerAction.Fold, Reason = "Random fold" };
            }

            long callAmount = game.GetCallAmount(_player.Id);
            if (callAmount == 0)
            {
                if (roll < 0.7f)
                {
                    return new AIDecision { Action = PlayerAction.Check, Reason = "Random check" };
                }
                else
                {
                    var (minRaise, maxRaise) = game.GetRaiseLimits(_player.Id);
                    long raiseAmount = minRaise + (long)((maxRaise - minRaise) * _random.NextDouble() * 0.3);
                    return new AIDecision { Action = PlayerAction.Raise, Amount = raiseAmount, Reason = "Random raise" };
                }
            }
            else
            {
                if (roll < 0.6f)
                {
                    return new AIDecision { Action = PlayerAction.Call, Reason = "Random call" };
                }
                else if (roll < 0.85f)
                {
                    return new AIDecision { Action = PlayerAction.Fold, Reason = "Random fold" };
                }
                else
                {
                    var (minRaise, maxRaise) = game.GetRaiseLimits(_player.Id);
                    return new AIDecision { Action = PlayerAction.Raise, Amount = minRaise, Reason = "Random raise" };
                }
            }
        }

        private AIDecision MakeMediumDecision(GameManager game)
        {
            float handStrength = EvaluateHandStrength(game);
            long callAmount = game.GetCallAmount(_player.Id);
            float potOdds = CalculatePotOdds(game, callAmount);

            if (callAmount == 0)
            {
                if (handStrength > 0.7f)
                {
                    var (minRaise, maxRaise) = game.GetRaiseLimits(_player.Id);
                    long raiseAmount = (long)(minRaise + (maxRaise - minRaise) * handStrength * 0.5);
                    return new AIDecision 
                    { 
                        Action = PlayerAction.Raise, 
                        Amount = raiseAmount,
                        Confidence = handStrength,
                        Reason = $"Strong hand ({handStrength:P0})"
                    };
                }
                else if (handStrength > 0.4f && _random.NextDouble() < AGGRESSION_FACTOR)
                {
                    var (minRaise, _) = game.GetRaiseLimits(_player.Id);
                    return new AIDecision 
                    { 
                        Action = PlayerAction.Raise, 
                        Amount = minRaise,
                        Confidence = handStrength,
                        Reason = "Semi-bluff"
                    };
                }
                return new AIDecision { Action = PlayerAction.Check, Reason = "Check back" };
            }
            else
            {
                if (handStrength > potOdds + 0.1f)
                {
                    if (handStrength > 0.8f && _random.NextDouble() < 0.5f)
                    {
                        var (minRaise, maxRaise) = game.GetRaiseLimits(_player.Id);
                        long raiseAmount = (long)(minRaise + (maxRaise - minRaise) * 0.4);
                        return new AIDecision 
                        { 
                            Action = PlayerAction.Raise, 
                            Amount = raiseAmount,
                            Confidence = handStrength,
                            Reason = "Value raise"
                        };
                    }
                    return new AIDecision 
                    { 
                        Action = PlayerAction.Call,
                        Confidence = handStrength,
                        Reason = $"Good odds ({handStrength:P0} vs {potOdds:P0})"
                    };
                }
                else if (handStrength > potOdds - 0.1f && _random.NextDouble() < 0.3f)
                {
                    return new AIDecision 
                    { 
                        Action = PlayerAction.Call,
                        Confidence = handStrength,
                        Reason = "Borderline call"
                    };
                }
                return new AIDecision { Action = PlayerAction.Fold, Reason = "Bad odds" };
            }
        }

        private AIDecision MakeHardDecision(GameManager game)
        {
            float handStrength = EvaluateHandStrength(game);
            float positionValue = GetPositionValue(game);
            float adjustedStrength = handStrength * (0.8f + positionValue * 0.4f);
            
            long callAmount = game.GetCallAmount(_player.Id);
            float potOdds = CalculatePotOdds(game, callAmount);
            int activePlayers = game.Players.Count(p => p.IsActive && !p.HasFolded);

            adjustedStrength *= (1f - (activePlayers - 2) * 0.05f);

            bool shouldBluff = _random.NextDouble() < BLUFF_FREQUENCY * positionValue;

            if (callAmount == 0)
            {
                if (adjustedStrength > 0.65f || shouldBluff)
                {
                    var (minRaise, maxRaise) = game.GetRaiseLimits(_player.Id);
                    float betSizing = adjustedStrength > 0.8f ? 0.7f : 0.4f;
                    long raiseAmount = (long)(minRaise + (maxRaise - minRaise) * betSizing);
                    return new AIDecision 
                    { 
                        Action = PlayerAction.Raise, 
                        Amount = raiseAmount,
                        Confidence = adjustedStrength,
                        Reason = shouldBluff ? "Positional bluff" : "Value bet"
                    };
                }
                return new AIDecision { Action = PlayerAction.Check, Reason = "Check" };
            }
            else
            {
                float effectiveOdds = potOdds * (1f + positionValue * 0.2f);
                
                if (adjustedStrength > effectiveOdds + 0.15f)
                {
                    if (adjustedStrength > 0.75f)
                    {
                        var (minRaise, maxRaise) = game.GetRaiseLimits(_player.Id);
                        long raiseAmount = (long)(minRaise + (maxRaise - minRaise) * adjustedStrength * 0.5);
                        return new AIDecision 
                        { 
                            Action = PlayerAction.Raise, 
                            Amount = raiseAmount,
                            Confidence = adjustedStrength,
                            Reason = "Value raise"
                        };
                    }
                    return new AIDecision 
                    { 
                        Action = PlayerAction.Call,
                        Confidence = adjustedStrength,
                        Reason = "Profitable call"
                    };
                }
                else if (adjustedStrength > effectiveOdds && _random.NextDouble() < 0.4f)
                {
                    return new AIDecision 
                    { 
                        Action = PlayerAction.Call,
                        Confidence = adjustedStrength,
                        Reason = "Marginal call"
                    };
                }
                return new AIDecision { Action = PlayerAction.Fold, Reason = "Fold to aggression" };
            }
        }

        private AIDecision MakeExpertDecision(GameManager game)
        {
            var decision = MakeHardDecision(game);

            float gtoAdjustment = (float)_random.NextDouble() * 0.1f - 0.05f;
            
            if (decision.Action == PlayerAction.Call && _random.NextDouble() < 0.15f)
            {
                var (minRaise, _) = game.GetRaiseLimits(_player.Id);
                return new AIDecision 
                { 
                    Action = PlayerAction.Raise, 
                    Amount = minRaise,
                    Confidence = decision.Confidence,
                    Reason = "Balanced raise"
                };
            }
            
            if (decision.Action == PlayerAction.Raise && _random.NextDouble() < 0.1f)
            {
                long callAmount = game.GetCallAmount(_player.Id);
                if (callAmount > 0)
                {
                    return new AIDecision 
                    { 
                        Action = PlayerAction.Call,
                        Confidence = decision.Confidence,
                        Reason = "Trap call"
                    };
                }
            }

            return decision;
        }

        private float EvaluateHandStrength(GameManager game)
        {
            var holeCards = _player.HoleCards;
            if (holeCards[0] == null || holeCards[1] == null)
                return 0f;

            var communityCards = game.State.GetVisibleCommunityCards();
            
            if (communityCards.Length == 0)
            {
                return EvaluatePreflopStrength(holeCards);
            }
            else
            {
                return EvaluatePostflopStrength(holeCards, communityCards);
            }
        }

        private float EvaluatePreflopStrength(Card[] holeCards)
        {
            int rank1 = (int)holeCards[0].Rank;
            int rank2 = (int)holeCards[1].Rank;
            bool isPair = rank1 == rank2;
            bool isSuited = holeCards[0].Suit == holeCards[1].Suit;
            bool isConnected = Math.Abs(rank1 - rank2) == 1;
            
            int highRank = Math.Max(rank1, rank2);
            int lowRank = Math.Min(rank1, rank2);

            float strength = 0f;

            if (isPair)
            {
                strength = 0.5f + (highRank - 2) / 24f;
                if (highRank >= 10) strength += 0.1f;
                if (highRank == 14) strength = 0.95f;
            }
            else
            {
                strength = ((highRank - 2) + (lowRank - 2) * 0.5f) / 30f;
                
                if (highRank == 14 && lowRank >= 10) strength = 0.75f;
                if (highRank == 14 && lowRank == 13) strength = 0.85f;
                
                if (isSuited) strength += 0.08f;
                if (isConnected) strength += 0.05f;
            }

            return Math.Clamp(strength, 0f, 1f);
        }

        private float EvaluatePostflopStrength(Card[] holeCards, Card[] communityCards)
        {
            try
            {
                var handRank = HandEvaluator.Evaluate(holeCards, communityCards);
                
                float baseStrength = (int)handRank.Type switch
                {
                    >= 8 => 0.98f,  // Royal/Straight Flush
                    7 => 0.95f,     // Four of a Kind
                    6 => 0.90f,     // Full House
                    5 => 0.80f,     // Flush
                    4 => 0.70f,     // Straight
                    3 => 0.60f,     // Three of a Kind
                    2 => 0.50f,     // Two Pair
                    1 => 0.35f,     // One Pair
                    _ => 0.15f      // High Card
                };

                if (handRank.Kickers.Length > 0)
                {
                    baseStrength += ((int)handRank.Kickers[0] - 2) / 100f;
                }

                return Math.Clamp(baseStrength, 0f, 1f);
            }
            catch
            {
                return 0.2f;
            }
        }

        private float GetPositionValue(GameManager game)
        {
            var activePlayers = game.Players.Where(p => p.IsActive).OrderBy(p => p.SeatIndex).ToList();
            int playerIndex = activePlayers.FindIndex(p => p.Id == _player.Id);
            int dealerIndex = activePlayers.FindIndex(p => p.IsDealer);
            
            int relativePosition = (playerIndex - dealerIndex + activePlayers.Count) % activePlayers.Count;
            
            return (float)relativePosition / (activePlayers.Count - 1);
        }

        private float CalculatePotOdds(GameManager game, long callAmount)
        {
            if (callAmount <= 0) return 0f;
            
            long totalPot = game.TotalPot + callAmount;
            return (float)callAmount / totalPot;
        }
    }

    public class AIManager
    {
        private readonly Dictionary<string, AIPlayer> _aiPlayers;
        private readonly GameManager _gameManager;

        public AIManager(GameManager gameManager)
        {
            _gameManager = gameManager;
            _aiPlayers = new Dictionary<string, AIPlayer>();
        }

        public void AddAIPlayer(Player player, AIDifficulty difficulty)
        {
            player.IsBot = true;
            var aiPlayer = new AIPlayer(player, difficulty);
            _aiPlayers[player.Id] = aiPlayer;
        }

        public void RemoveAIPlayer(string playerId)
        {
            _aiPlayers.Remove(playerId);
        }

        public bool IsAIPlayer(string playerId)
        {
            return _aiPlayers.ContainsKey(playerId);
        }

        public AIDecision GetAIDecision(string playerId)
        {
            if (!_aiPlayers.TryGetValue(playerId, out var aiPlayer))
                return null;

            return aiPlayer.MakeDecision(_gameManager);
        }

        public void ProcessAITurn()
        {
            var currentPlayer = _gameManager.GetCurrentPlayer();
            if (currentPlayer == null || !IsAIPlayer(currentPlayer.Id))
                return;

            var decision = GetAIDecision(currentPlayer.Id);
            if (decision != null)
            {
                _gameManager.ProcessAction(currentPlayer.Id, decision.Action, decision.Amount);
            }
        }

        public Player CreateAIPlayer(string name, long chips, AIDifficulty difficulty)
        {
            string id = $"ai_{Guid.NewGuid():N}";
            var player = new Player(id, name, chips);
            player.IsBot = true;
            AddAIPlayer(player, difficulty);
            return player;
        }
    }
}
