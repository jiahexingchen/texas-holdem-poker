using System;

namespace TexasHoldem.Core
{
    public enum HandRankType
    {
        HighCard = 0,        // 高牌
        OnePair = 1,         // 一对
        TwoPair = 2,         // 两对
        ThreeOfAKind = 3,    // 三条
        Straight = 4,        // 顺子
        Flush = 5,           // 同花
        FullHouse = 6,       // 葫芦
        FourOfAKind = 7,     // 四条
        StraightFlush = 8,   // 同花顺
        RoyalFlush = 9       // 皇家同花顺
    }

    public class HandRank : IComparable<HandRank>
    {
        public HandRankType Type { get; private set; }
        public Rank[] Kickers { get; private set; }
        public Card[] BestHand { get; private set; }

        public HandRank(HandRankType type, Rank[] kickers, Card[] bestHand)
        {
            Type = type;
            Kickers = kickers ?? Array.Empty<Rank>();
            BestHand = bestHand ?? Array.Empty<Card>();
        }

        public int CompareTo(HandRank other)
        {
            if (other == null) return 1;

            int typeCompare = Type.CompareTo(other.Type);
            if (typeCompare != 0) return typeCompare;

            int minKickers = Math.Min(Kickers.Length, other.Kickers.Length);
            for (int i = 0; i < minKickers; i++)
            {
                int kickerCompare = Kickers[i].CompareTo(other.Kickers[i]);
                if (kickerCompare != 0) return kickerCompare;
            }

            return 0;
        }

        public override string ToString()
        {
            string handName = Type switch
            {
                HandRankType.RoyalFlush => "皇家同花顺",
                HandRankType.StraightFlush => "同花顺",
                HandRankType.FourOfAKind => "四条",
                HandRankType.FullHouse => "葫芦",
                HandRankType.Flush => "同花",
                HandRankType.Straight => "顺子",
                HandRankType.ThreeOfAKind => "三条",
                HandRankType.TwoPair => "两对",
                HandRankType.OnePair => "一对",
                HandRankType.HighCard => "高牌",
                _ => "未知"
            };

            return $"{handName} ({string.Join(", ", Array.ConvertAll(BestHand, c => c.ToString()))})";
        }

        public string ToEnglishString()
        {
            return Type switch
            {
                HandRankType.RoyalFlush => "Royal Flush",
                HandRankType.StraightFlush => "Straight Flush",
                HandRankType.FourOfAKind => "Four of a Kind",
                HandRankType.FullHouse => "Full House",
                HandRankType.Flush => "Flush",
                HandRankType.Straight => "Straight",
                HandRankType.ThreeOfAKind => "Three of a Kind",
                HandRankType.TwoPair => "Two Pair",
                HandRankType.OnePair => "One Pair",
                HandRankType.HighCard => "High Card",
                _ => "Unknown"
            };
        }

        public static bool operator ==(HandRank left, HandRank right)
        {
            if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
            if (ReferenceEquals(right, null)) return false;
            return left.CompareTo(right) == 0;
        }

        public static bool operator !=(HandRank left, HandRank right)
        {
            return !(left == right);
        }

        public static bool operator <(HandRank left, HandRank right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(HandRank left, HandRank right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(HandRank left, HandRank right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(HandRank left, HandRank right)
        {
            return left.CompareTo(right) >= 0;
        }

        public override bool Equals(object obj)
        {
            return obj is HandRank other && CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            int hash = (int)Type;
            foreach (var kicker in Kickers)
            {
                hash = hash * 17 + (int)kicker;
            }
            return hash;
        }
    }
}
