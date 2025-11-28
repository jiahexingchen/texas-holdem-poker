using System;

namespace TexasHoldem.Core
{
    public enum GamePhase
    {
        Waiting,      // 等待玩家
        Starting,     // 游戏开始
        Preflop,      // 翻牌前
        Flop,         // 翻牌
        Turn,         // 转牌
        River,        // 河牌
        Showdown,     // 摊牌
        Finished      // 结束
    }

    public enum TableType
    {
        NoLimit,      // 无限注
        PotLimit,     // 底池限注
        FixedLimit    // 限注
    }

    [Serializable]
    public class GameConfig
    {
        public long SmallBlind { get; set; } = 10;
        public long BigBlind { get; set; } = 20;
        public long Ante { get; set; } = 0;
        public int MaxPlayers { get; set; } = 9;
        public int MinPlayers { get; set; } = 2;
        public long MinBuyIn { get; set; } = 400;
        public long MaxBuyIn { get; set; } = 2000;
        public TableType TableType { get; set; } = TableType.NoLimit;
        public int ActionTimeoutSeconds { get; set; } = 30;
        public bool AllowRabbitHunt { get; set; } = false;

        public GameConfig() { }

        public GameConfig(long smallBlind, long bigBlind)
        {
            SmallBlind = smallBlind;
            BigBlind = bigBlind;
            MinBuyIn = BigBlind * 20;
            MaxBuyIn = BigBlind * 100;
        }

        public static GameConfig Default => new GameConfig(10, 20);

        public static GameConfig LowStakes => new GameConfig(5, 10);
        public static GameConfig MidStakes => new GameConfig(25, 50);
        public static GameConfig HighStakes => new GameConfig(100, 200);
    }

    [Serializable]
    public class GameState
    {
        public string GameId { get; set; }
        public string RoomId { get; set; }
        public GamePhase Phase { get; set; }
        public GameConfig Config { get; set; }
        public int DealerSeat { get; set; }
        public int SmallBlindSeat { get; set; }
        public int BigBlindSeat { get; set; }
        public int CurrentPlayerSeat { get; set; }
        public int HandNumber { get; set; }
        public long CurrentBet { get; set; }
        public long MinRaise { get; set; }
        public long LastRaiseAmount { get; set; }
        public Card[] CommunityCards { get; set; }
        public int CommunityCardCount { get; set; }
        public DateTime ActionDeadline { get; set; }
        public bool IsHandInProgress { get; set; }

        public GameState()
        {
            Phase = GamePhase.Waiting;
            Config = GameConfig.Default;
            CommunityCards = new Card[5];
            CommunityCardCount = 0;
            HandNumber = 0;
            IsHandInProgress = false;
        }

        public void Reset()
        {
            Phase = GamePhase.Waiting;
            CurrentBet = 0;
            MinRaise = Config.BigBlind;
            LastRaiseAmount = Config.BigBlind;
            CommunityCards = new Card[5];
            CommunityCardCount = 0;
            IsHandInProgress = false;
        }

        public Card[] GetVisibleCommunityCards()
        {
            var visible = new Card[CommunityCardCount];
            Array.Copy(CommunityCards, visible, CommunityCardCount);
            return visible;
        }

        public void AddCommunityCard(Card card)
        {
            if (CommunityCardCount < 5)
            {
                CommunityCards[CommunityCardCount++] = card;
            }
        }

        public void SetFlop(Card card1, Card card2, Card card3)
        {
            CommunityCards[0] = card1;
            CommunityCards[1] = card2;
            CommunityCards[2] = card3;
            CommunityCardCount = 3;
        }

        public void SetTurn(Card card)
        {
            CommunityCards[3] = card;
            CommunityCardCount = 4;
        }

        public void SetRiver(Card card)
        {
            CommunityCards[4] = card;
            CommunityCardCount = 5;
        }

        public void AdvancePhase()
        {
            Phase = Phase switch
            {
                GamePhase.Waiting => GamePhase.Starting,
                GamePhase.Starting => GamePhase.Preflop,
                GamePhase.Preflop => GamePhase.Flop,
                GamePhase.Flop => GamePhase.Turn,
                GamePhase.Turn => GamePhase.River,
                GamePhase.River => GamePhase.Showdown,
                GamePhase.Showdown => GamePhase.Finished,
                GamePhase.Finished => GamePhase.Waiting,
                _ => GamePhase.Waiting
            };
        }

        public string GetPhaseDisplayName()
        {
            return Phase switch
            {
                GamePhase.Waiting => "等待中",
                GamePhase.Starting => "开始中",
                GamePhase.Preflop => "翻牌前",
                GamePhase.Flop => "翻牌",
                GamePhase.Turn => "转牌",
                GamePhase.River => "河牌",
                GamePhase.Showdown => "摊牌",
                GamePhase.Finished => "已结束",
                _ => "未知"
            };
        }
    }
}
