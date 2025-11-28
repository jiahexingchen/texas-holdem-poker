using System;
using System.Collections.Generic;
using System.Linq;
using TexasHoldem.Core;

namespace TexasHoldem.Game
{
    public class GameEventArgs : EventArgs
    {
        public GamePhase Phase { get; set; }
        public Player Player { get; set; }
        public PlayerAction Action { get; set; }
        public long Amount { get; set; }
        public Card[] Cards { get; set; }
        public string Message { get; set; }
    }

    public class GameManager
    {
        private GameState _state;
        private List<Player> _players;
        private Deck _deck;
        private PotManager _potManager;
        private int _actionCount;

        public GameState State => _state;
        public IReadOnlyList<Player> Players => _players.AsReadOnly();
        public PotManager PotManager => _potManager;
        public long TotalPot => _potManager.TotalPot;

        public event EventHandler<GameEventArgs> OnGameEvent;
        public event EventHandler<GameEventArgs> OnPhaseChanged;
        public event EventHandler<GameEventArgs> OnPlayerAction;
        public event EventHandler<GameEventArgs> OnCardsDealt;
        public event EventHandler<GameEventArgs> OnHandComplete;

        public GameManager() : this(GameConfig.Default) { }

        public GameManager(GameConfig config)
        {
            _state = new GameState { Config = config };
            _players = new List<Player>();
            _deck = new Deck();
            _potManager = new PotManager();
        }

        public bool AddPlayer(Player player)
        {
            if (_players.Count >= _state.Config.MaxPlayers)
                return false;

            if (_players.Any(p => p.Id == player.Id))
                return false;

            int seatIndex = FindEmptySeat();
            if (seatIndex < 0) return false;

            player.SeatIndex = seatIndex;
            _players.Add(player);
            return true;
        }

        public bool RemovePlayer(string playerId)
        {
            var player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return false;

            if (_state.IsHandInProgress && player.IsActive)
            {
                player.Fold();
                _potManager.PlayerFolded(playerId);
            }

            _players.Remove(player);
            return true;
        }

        private int FindEmptySeat()
        {
            var occupiedSeats = _players.Select(p => p.SeatIndex).ToHashSet();
            for (int i = 0; i < _state.Config.MaxPlayers; i++)
            {
                if (!occupiedSeats.Contains(i))
                    return i;
            }
            return -1;
        }

        public bool CanStartHand()
        {
            int activePlayers = _players.Count(p => p.Chips > 0 && p.State != PlayerState.SittingOut);
            return activePlayers >= _state.Config.MinPlayers && !_state.IsHandInProgress;
        }

        public void StartNewHand()
        {
            if (!CanStartHand())
                throw new InvalidOperationException("Cannot start hand");

            _state.HandNumber++;
            _state.IsHandInProgress = true;
            _state.Phase = GamePhase.Starting;
            _actionCount = 0;

            foreach (var player in _players)
            {
                player.Reset();
                if (player.Chips > 0 && player.State != PlayerState.SittingOut)
                {
                    player.SetState(PlayerState.Active);
                }
            }

            _potManager.Reset();
            _deck.Reset();
            _deck.Shuffle();
            _state.CommunityCards = new Card[5];
            _state.CommunityCardCount = 0;

            MoveButton();
            PostBlinds();
            DealHoleCards();

            _state.Phase = GamePhase.Preflop;
            SetNextPlayer(_state.BigBlindSeat);

            RaiseEvent(OnPhaseChanged, new GameEventArgs { Phase = _state.Phase });
        }

        private void MoveButton()
        {
            var activePlayers = GetActivePlayers();
            if (activePlayers.Count < 2) return;

            if (_state.HandNumber == 1)
            {
                _state.DealerSeat = activePlayers[0].SeatIndex;
            }
            else
            {
                int currentIndex = activePlayers.FindIndex(p => p.SeatIndex == _state.DealerSeat);
                currentIndex = (currentIndex + 1) % activePlayers.Count;
                _state.DealerSeat = activePlayers[currentIndex].SeatIndex;
            }

            var dealer = _players.First(p => p.SeatIndex == _state.DealerSeat);
            dealer.IsDealer = true;

            int sbIndex = GetNextActivePlayerIndex(_state.DealerSeat);
            int bbIndex = GetNextActivePlayerIndex(activePlayers[sbIndex].SeatIndex);

            if (activePlayers.Count == 2)
            {
                _state.SmallBlindSeat = _state.DealerSeat;
                _state.BigBlindSeat = activePlayers[sbIndex].SeatIndex;
            }
            else
            {
                _state.SmallBlindSeat = activePlayers[sbIndex].SeatIndex;
                _state.BigBlindSeat = activePlayers[bbIndex].SeatIndex;
            }
        }

        private void PostBlinds()
        {
            var sbPlayer = _players.First(p => p.SeatIndex == _state.SmallBlindSeat);
            var bbPlayer = _players.First(p => p.SeatIndex == _state.BigBlindSeat);

            sbPlayer.PostBlind(_state.Config.SmallBlind, true);
            bbPlayer.PostBlind(_state.Config.BigBlind, false);

            _state.CurrentBet = _state.Config.BigBlind;
            _state.MinRaise = _state.Config.BigBlind;
            _state.LastRaiseAmount = _state.Config.BigBlind;

            if (_state.Config.Ante > 0)
            {
                foreach (var player in GetActivePlayers())
                {
                    player.PlaceBet(_state.Config.Ante);
                }
            }
        }

        private void DealHoleCards()
        {
            var activePlayers = GetActivePlayers();
            
            for (int round = 0; round < 2; round++)
            {
                foreach (var player in activePlayers)
                {
                    var card = _deck.Deal();
                    if (round == 0)
                    {
                        player.SetHoleCards(card, null);
                    }
                    else
                    {
                        player.SetHoleCards(player.HoleCards[0], card);
                    }
                }
            }

            foreach (var player in activePlayers)
            {
                RaiseEvent(OnCardsDealt, new GameEventArgs
                {
                    Player = player,
                    Cards = player.HoleCards
                });
            }
        }

        public bool ProcessAction(string playerId, PlayerAction action, long amount = 0)
        {
            var player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player == null || !player.CanAct)
                return false;

            if (player.SeatIndex != _state.CurrentPlayerSeat)
                return false;

            bool success = action switch
            {
                PlayerAction.Fold => ProcessFold(player),
                PlayerAction.Check => ProcessCheck(player),
                PlayerAction.Call => ProcessCall(player),
                PlayerAction.Raise => ProcessRaise(player, amount),
                PlayerAction.AllIn => ProcessAllIn(player),
                _ => false
            };

            if (success)
            {
                _actionCount++;
                RaiseEvent(OnPlayerAction, new GameEventArgs
                {
                    Player = player,
                    Action = action,
                    Amount = amount
                });

                CheckRoundComplete();
            }

            return success;
        }

        private bool ProcessFold(Player player)
        {
            player.Fold();
            _potManager.PlayerFolded(player.Id);
            return true;
        }

        private bool ProcessCheck(Player player)
        {
            if (player.CurrentBet < _state.CurrentBet)
                return false;

            player.Check();
            return true;
        }

        private bool ProcessCall(Player player)
        {
            long toCall = _state.CurrentBet - player.CurrentBet;
            if (toCall <= 0)
            {
                player.Check();
                return true;
            }

            player.Call(_state.CurrentBet);
            return true;
        }

        private bool ProcessRaise(Player player, long totalAmount)
        {
            long minRaiseTotal = _state.CurrentBet + _state.MinRaise;
            
            if (totalAmount < minRaiseTotal && totalAmount < player.Chips + player.CurrentBet)
                return false;

            long actualRaise = totalAmount - _state.CurrentBet;
            player.Raise(totalAmount);

            _state.LastRaiseAmount = actualRaise;
            _state.MinRaise = actualRaise;
            _state.CurrentBet = player.CurrentBet;
            _actionCount = 0;

            return true;
        }

        private bool ProcessAllIn(Player player)
        {
            long allInAmount = player.GoAllIn();
            
            if (player.CurrentBet > _state.CurrentBet)
            {
                long raiseAmount = player.CurrentBet - _state.CurrentBet;
                if (raiseAmount >= _state.MinRaise)
                {
                    _state.LastRaiseAmount = raiseAmount;
                    _state.MinRaise = raiseAmount;
                    _actionCount = 0;
                }
                _state.CurrentBet = player.CurrentBet;
            }

            return true;
        }

        private void CheckRoundComplete()
        {
            var activePlayers = GetActivePlayers().Where(p => !p.IsAllIn).ToList();
            var allActivePlayers = GetActivePlayers();

            if (allActivePlayers.Count <= 1)
            {
                EndHand();
                return;
            }

            bool allMatched = allActivePlayers.All(p => 
                p.CurrentBet == _state.CurrentBet || p.IsAllIn);
            
            bool allActed = activePlayers.All(p => 
                p.LastAction != PlayerAction.None && 
                p.LastAction != PlayerAction.SmallBlind && 
                p.LastAction != PlayerAction.BigBlind);

            if (allMatched && allActed)
            {
                AdvanceToNextStreet();
            }
            else
            {
                SetNextPlayer(_state.CurrentPlayerSeat);
            }
        }

        private void AdvanceToNextStreet()
        {
            CollectBets();

            foreach (var player in GetActivePlayers())
            {
                player.ResetForNewStreet();
            }

            _state.CurrentBet = 0;
            _state.MinRaise = _state.Config.BigBlind;
            _actionCount = 0;

            switch (_state.Phase)
            {
                case GamePhase.Preflop:
                    DealFlop();
                    break;
                case GamePhase.Flop:
                    DealTurn();
                    break;
                case GamePhase.Turn:
                    DealRiver();
                    break;
                case GamePhase.River:
                    EndHand();
                    return;
            }

            _state.AdvancePhase();

            var nonAllInPlayers = GetActivePlayers().Where(p => !p.IsAllIn).ToList();
            if (nonAllInPlayers.Count <= 1)
            {
                RunOutBoard();
            }
            else
            {
                SetFirstPlayerAfterDealer();
                RaiseEvent(OnPhaseChanged, new GameEventArgs { Phase = _state.Phase });
            }
        }

        private void DealFlop()
        {
            _deck.Burn();
            _state.SetFlop(_deck.Deal(), _deck.Deal(), _deck.Deal());
            
            RaiseEvent(OnCardsDealt, new GameEventArgs
            {
                Cards = _state.GetVisibleCommunityCards(),
                Message = "Flop"
            });
        }

        private void DealTurn()
        {
            _deck.Burn();
            _state.SetTurn(_deck.Deal());
            
            RaiseEvent(OnCardsDealt, new GameEventArgs
            {
                Cards = _state.GetVisibleCommunityCards(),
                Message = "Turn"
            });
        }

        private void DealRiver()
        {
            _deck.Burn();
            _state.SetRiver(_deck.Deal());
            
            RaiseEvent(OnCardsDealt, new GameEventArgs
            {
                Cards = _state.GetVisibleCommunityCards(),
                Message = "River"
            });
        }

        private void RunOutBoard()
        {
            while (_state.CommunityCardCount < 5)
            {
                _deck.Burn();
                _state.AddCommunityCard(_deck.Deal());
            }
            EndHand();
        }

        private void CollectBets()
        {
            _potManager.CalculateSidePots(_players);
        }

        private void EndHand()
        {
            _state.Phase = GamePhase.Showdown;
            CollectBets();

            var activePlayers = GetActivePlayers();
            Dictionary<string, long> winnings;

            if (activePlayers.Count == 1)
            {
                winnings = new Dictionary<string, long>
                {
                    { activePlayers[0].Id, TotalPot }
                };
            }
            else
            {
                winnings = _potManager.DistributeWinnings(_players, _state.GetVisibleCommunityCards());
            }

            foreach (var kvp in winnings)
            {
                var winner = _players.First(p => p.Id == kvp.Key);
                winner.AddChips(kvp.Value);
            }

            _state.Phase = GamePhase.Finished;
            _state.IsHandInProgress = false;

            RaiseEvent(OnHandComplete, new GameEventArgs
            {
                Message = $"Hand #{_state.HandNumber} complete"
            });
        }

        private List<Player> GetActivePlayers()
        {
            return _players
                .Where(p => p.IsActive)
                .OrderBy(p => p.SeatIndex)
                .ToList();
        }

        private int GetNextActivePlayerIndex(int currentSeat)
        {
            var activePlayers = GetActivePlayers();
            int currentIndex = activePlayers.FindIndex(p => p.SeatIndex == currentSeat);
            return (currentIndex + 1) % activePlayers.Count;
        }

        private void SetNextPlayer(int currentSeat)
        {
            var activePlayers = GetActivePlayers().Where(p => p.CanAct).ToList();
            if (activePlayers.Count == 0) return;

            int currentIndex = activePlayers.FindIndex(p => p.SeatIndex == currentSeat);
            if (currentIndex < 0) currentIndex = 0;
            
            currentIndex = (currentIndex + 1) % activePlayers.Count;
            _state.CurrentPlayerSeat = activePlayers[currentIndex].SeatIndex;
            _state.ActionDeadline = DateTime.UtcNow.AddSeconds(_state.Config.ActionTimeoutSeconds);
        }

        private void SetFirstPlayerAfterDealer()
        {
            var activePlayers = GetActivePlayers().Where(p => p.CanAct).ToList();
            if (activePlayers.Count == 0) return;

            int dealerIndex = activePlayers.FindIndex(p => p.SeatIndex == _state.DealerSeat);
            if (dealerIndex < 0) dealerIndex = 0;
            
            int firstIndex = (dealerIndex + 1) % activePlayers.Count;
            _state.CurrentPlayerSeat = activePlayers[firstIndex].SeatIndex;
            _state.ActionDeadline = DateTime.UtcNow.AddSeconds(_state.Config.ActionTimeoutSeconds);
        }

        public Player GetCurrentPlayer()
        {
            return _players.FirstOrDefault(p => p.SeatIndex == _state.CurrentPlayerSeat);
        }

        public (long minRaise, long maxRaise) GetRaiseLimits(string playerId)
        {
            var player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return (0, 0);

            long minRaise = _state.CurrentBet + _state.MinRaise;
            long maxRaise = player.Chips + player.CurrentBet;

            return (Math.Min(minRaise, maxRaise), maxRaise);
        }

        public long GetCallAmount(string playerId)
        {
            var player = _players.FirstOrDefault(p => p.Id == playerId);
            if (player == null) return 0;

            return Math.Min(_state.CurrentBet - player.CurrentBet, player.Chips);
        }

        private void RaiseEvent(EventHandler<GameEventArgs> handler, GameEventArgs args)
        {
            handler?.Invoke(this, args);
            OnGameEvent?.Invoke(this, args);
        }
    }
}
