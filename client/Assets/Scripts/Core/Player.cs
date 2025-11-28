using System;

namespace TexasHoldem.Core
{
    public enum PlayerState
    {
        Waiting,      // 等待中
        Active,       // 活跃（可操作）
        Folded,       // 已弃牌
        AllIn,        // 全下
        SittingOut    // 离开
    }

    public enum PlayerAction
    {
        None,
        Fold,         // 弃牌
        Check,        // 过牌
        Call,         // 跟注
        Raise,        // 加注
        AllIn,        // 全下
        SmallBlind,   // 小盲注
        BigBlind      // 大盲注
    }

    [Serializable]
    public class Player
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public string Avatar { get; set; }
        public int SeatIndex { get; set; }
        public long Chips { get; private set; }
        public long CurrentBet { get; private set; }
        public long TotalBetInRound { get; private set; }
        public Card[] HoleCards { get; private set; }
        public PlayerState State { get; private set; }
        public PlayerAction LastAction { get; private set; }
        public bool IsDealer { get; set; }
        public bool IsSmallBlind { get; set; }
        public bool IsBigBlind { get; set; }
        public bool IsBot { get; set; }

        public bool IsActive => State == PlayerState.Active || State == PlayerState.AllIn;
        public bool CanAct => State == PlayerState.Active;
        public bool HasFolded => State == PlayerState.Folded;
        public bool IsAllIn => State == PlayerState.AllIn;

        public Player(string id, string name, long chips)
        {
            Id = id;
            Name = name;
            Chips = chips;
            State = PlayerState.Waiting;
            HoleCards = new Card[2];
            Reset();
        }

        public void Reset()
        {
            CurrentBet = 0;
            TotalBetInRound = 0;
            HoleCards = new Card[2];
            State = PlayerState.Waiting;
            LastAction = PlayerAction.None;
            IsDealer = false;
            IsSmallBlind = false;
            IsBigBlind = false;
        }

        public void ResetForNewStreet()
        {
            CurrentBet = 0;
            LastAction = PlayerAction.None;
            if (State == PlayerState.Active)
            {
                // 保持活跃状态
            }
        }

        public void SetHoleCards(Card card1, Card card2)
        {
            HoleCards[0] = card1;
            HoleCards[1] = card2;
            State = PlayerState.Active;
        }

        public long PlaceBet(long amount)
        {
            if (amount <= 0) return 0;

            long actualBet = Math.Min(amount, Chips);
            Chips -= actualBet;
            CurrentBet += actualBet;
            TotalBetInRound += actualBet;

            if (Chips == 0)
            {
                State = PlayerState.AllIn;
            }

            return actualBet;
        }

        public void Fold()
        {
            State = PlayerState.Folded;
            LastAction = PlayerAction.Fold;
        }

        public void Check()
        {
            LastAction = PlayerAction.Check;
        }

        public long Call(long amountToCall)
        {
            long needed = amountToCall - CurrentBet;
            if (needed <= 0)
            {
                LastAction = PlayerAction.Check;
                return 0;
            }

            long actualBet = PlaceBet(needed);
            LastAction = Chips == 0 ? PlayerAction.AllIn : PlayerAction.Call;
            return actualBet;
        }

        public long Raise(long totalRaiseAmount)
        {
            long needed = totalRaiseAmount - CurrentBet;
            if (needed <= 0) return 0;

            long actualBet = PlaceBet(needed);
            LastAction = Chips == 0 ? PlayerAction.AllIn : PlayerAction.Raise;
            return actualBet;
        }

        public long GoAllIn()
        {
            long allInAmount = Chips;
            PlaceBet(allInAmount);
            LastAction = PlayerAction.AllIn;
            return allInAmount;
        }

        public void PostBlind(long amount, bool isSmallBlind)
        {
            PlaceBet(amount);
            if (isSmallBlind)
            {
                IsSmallBlind = true;
                LastAction = PlayerAction.SmallBlind;
            }
            else
            {
                IsBigBlind = true;
                LastAction = PlayerAction.BigBlind;
            }
        }

        public void AddChips(long amount)
        {
            if (amount > 0)
            {
                Chips += amount;
            }
        }

        public void SetState(PlayerState state)
        {
            State = state;
        }

        public void SitOut()
        {
            State = PlayerState.SittingOut;
        }

        public void SitIn()
        {
            if (State == PlayerState.SittingOut)
            {
                State = PlayerState.Waiting;
            }
        }

        public override string ToString()
        {
            return $"{Name} (${Chips}) [{State}]";
        }
    }
}
