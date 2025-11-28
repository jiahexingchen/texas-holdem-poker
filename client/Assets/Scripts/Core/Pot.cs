using System;
using System.Collections.Generic;
using System.Linq;

namespace TexasHoldem.Core
{
    [Serializable]
    public class Pot
    {
        public long Amount { get; private set; }
        public List<string> EligiblePlayerIds { get; private set; }
        public bool IsSidePot { get; private set; }

        public Pot(bool isSidePot = false)
        {
            Amount = 0;
            EligiblePlayerIds = new List<string>();
            IsSidePot = isSidePot;
        }

        public void AddAmount(long amount)
        {
            if (amount > 0)
            {
                Amount += amount;
            }
        }

        public void AddEligiblePlayer(string playerId)
        {
            if (!EligiblePlayerIds.Contains(playerId))
            {
                EligiblePlayerIds.Add(playerId);
            }
        }

        public void RemoveEligiblePlayer(string playerId)
        {
            EligiblePlayerIds.Remove(playerId);
        }

        public bool IsPlayerEligible(string playerId)
        {
            return EligiblePlayerIds.Contains(playerId);
        }

        public override string ToString()
        {
            string type = IsSidePot ? "边池" : "主池";
            return $"{type}: ${Amount} ({EligiblePlayerIds.Count}人)";
        }
    }

    public class PotManager
    {
        private List<Pot> _pots;
        public IReadOnlyList<Pot> Pots => _pots.AsReadOnly();
        public long TotalPot => _pots.Sum(p => p.Amount);

        public PotManager()
        {
            _pots = new List<Pot>();
            Reset();
        }

        public void Reset()
        {
            _pots.Clear();
            _pots.Add(new Pot(false));
        }

        public void CollectBets(List<Player> players)
        {
            var activePlayers = players.Where(p => p.IsActive || p.HasFolded).ToList();
            if (activePlayers.Count == 0) return;

            var bets = activePlayers
                .Where(p => p.CurrentBet > 0)
                .OrderBy(p => p.CurrentBet)
                .ToList();

            if (bets.Count == 0) return;

            long previousBet = 0;
            int potIndex = 0;

            foreach (var betGroup in bets.GroupBy(p => p.CurrentBet).OrderBy(g => g.Key))
            {
                long currentBet = betGroup.Key;
                long contribution = currentBet - previousBet;

                if (contribution > 0)
                {
                    if (potIndex >= _pots.Count)
                    {
                        _pots.Add(new Pot(true));
                    }

                    var eligiblePlayers = bets.Where(p => p.CurrentBet >= currentBet && p.IsActive).ToList();
                    
                    foreach (var player in bets.Where(p => p.CurrentBet >= currentBet))
                    {
                        _pots[potIndex].AddAmount(contribution);
                        if (player.IsActive)
                        {
                            _pots[potIndex].AddEligiblePlayer(player.Id);
                        }
                    }
                }

                bool hasAllIn = betGroup.Any(p => p.IsAllIn);
                if (hasAllIn && betGroup.Key < bets.Max(p => p.CurrentBet))
                {
                    potIndex++;
                }

                previousBet = currentBet;
            }

            RemoveEmptyPots();
        }

        public void CalculateSidePots(List<Player> players)
        {
            var activePlayers = players.Where(p => p.IsActive).OrderBy(p => p.TotalBetInRound).ToList();
            if (activePlayers.Count <= 1) return;

            _pots.Clear();

            var allInPlayers = activePlayers.Where(p => p.IsAllIn).OrderBy(p => p.TotalBetInRound).ToList();
            
            if (allInPlayers.Count == 0)
            {
                var mainPot = new Pot(false);
                foreach (var player in activePlayers)
                {
                    mainPot.AddAmount(player.TotalBetInRound);
                    mainPot.AddEligiblePlayer(player.Id);
                }
                _pots.Add(mainPot);
                return;
            }

            long previousLevel = 0;
            var remainingPlayers = new List<Player>(activePlayers);

            foreach (var allInPlayer in allInPlayers)
            {
                long allInAmount = allInPlayer.TotalBetInRound;
                long contribution = allInAmount - previousLevel;

                if (contribution > 0 && remainingPlayers.Count > 0)
                {
                    var pot = new Pot(_pots.Count > 0);
                    
                    foreach (var player in remainingPlayers)
                    {
                        pot.AddAmount(Math.Min(contribution, player.TotalBetInRound - previousLevel));
                        pot.AddEligiblePlayer(player.Id);
                    }

                    if (pot.Amount > 0)
                    {
                        _pots.Add(pot);
                    }
                }

                previousLevel = allInAmount;
                remainingPlayers = remainingPlayers.Where(p => p.TotalBetInRound > allInAmount).ToList();
            }

            if (remainingPlayers.Count > 0)
            {
                var finalPot = new Pot(_pots.Count > 0);
                foreach (var player in remainingPlayers)
                {
                    long remaining = player.TotalBetInRound - previousLevel;
                    if (remaining > 0)
                    {
                        finalPot.AddAmount(remaining);
                        finalPot.AddEligiblePlayer(player.Id);
                    }
                }
                if (finalPot.Amount > 0)
                {
                    _pots.Add(finalPot);
                }
            }

            RemoveEmptyPots();
        }

        public void PlayerFolded(string playerId)
        {
            foreach (var pot in _pots)
            {
                pot.RemoveEligiblePlayer(playerId);
            }
        }

        public Dictionary<string, long> DistributeWinnings(List<Player> players, Card[] communityCards)
        {
            var winnings = new Dictionary<string, long>();
            
            foreach (var pot in _pots)
            {
                var eligiblePlayers = players.Where(p => pot.IsPlayerEligible(p.Id) && p.IsActive).ToList();
                
                if (eligiblePlayers.Count == 0) continue;
                
                if (eligiblePlayers.Count == 1)
                {
                    string winnerId = eligiblePlayers[0].Id;
                    if (!winnings.ContainsKey(winnerId))
                        winnings[winnerId] = 0;
                    winnings[winnerId] += pot.Amount;
                    continue;
                }

                var handRanks = new Dictionary<string, HandRank>();
                foreach (var player in eligiblePlayers)
                {
                    if (player.HoleCards[0] != null && player.HoleCards[1] != null)
                    {
                        handRanks[player.Id] = HandEvaluator.Evaluate(player.HoleCards, communityCards);
                    }
                }

                if (handRanks.Count == 0) continue;

                var bestHand = handRanks.Values.Max();
                var winners = handRanks.Where(kv => kv.Value.CompareTo(bestHand) == 0).Select(kv => kv.Key).ToList();

                long share = pot.Amount / winners.Count;
                long remainder = pot.Amount % winners.Count;

                foreach (var winnerId in winners)
                {
                    if (!winnings.ContainsKey(winnerId))
                        winnings[winnerId] = 0;
                    winnings[winnerId] += share;
                }

                if (remainder > 0 && winners.Count > 0)
                {
                    winnings[winners[0]] += remainder;
                }
            }

            return winnings;
        }

        private void RemoveEmptyPots()
        {
            _pots.RemoveAll(p => p.Amount == 0);
            if (_pots.Count == 0)
            {
                _pots.Add(new Pot(false));
            }
        }

        public string GetPotDisplay()
        {
            if (_pots.Count == 1)
            {
                return $"底池: ${_pots[0].Amount}";
            }

            var parts = new List<string>();
            for (int i = 0; i < _pots.Count; i++)
            {
                string name = i == 0 ? "主池" : $"边池{i}";
                parts.Add($"{name}: ${_pots[i].Amount}");
            }
            return string.Join(" | ", parts);
        }
    }
}
