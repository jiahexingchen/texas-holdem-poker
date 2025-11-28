using System;

namespace TexasHoldem.Core
{
    public enum Suit
    {
        Hearts = 0,    // 红桃
        Diamonds = 1,  // 方块
        Clubs = 2,     // 梅花
        Spades = 3     // 黑桃
    }

    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    [Serializable]
    public class Card : IComparable<Card>, IEquatable<Card>
    {
        public Suit Suit { get; private set; }
        public Rank Rank { get; private set; }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public Card(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex > 51)
                throw new ArgumentOutOfRangeException(nameof(cardIndex), "Card index must be 0-51");
            
            Suit = (Suit)(cardIndex / 13);
            Rank = (Rank)(cardIndex % 13 + 2);
        }

        public int ToIndex()
        {
            return (int)Suit * 13 + ((int)Rank - 2);
        }

        public int CompareTo(Card other)
        {
            if (other == null) return 1;
            return Rank.CompareTo(other.Rank);
        }

        public bool Equals(Card other)
        {
            if (other == null) return false;
            return Suit == other.Suit && Rank == other.Rank;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Card);
        }

        public override int GetHashCode()
        {
            return ToIndex();
        }

        public override string ToString()
        {
            string rankStr = Rank switch
            {
                Rank.Ace => "A",
                Rank.King => "K",
                Rank.Queen => "Q",
                Rank.Jack => "J",
                Rank.Ten => "T",
                _ => ((int)Rank).ToString()
            };

            string suitStr = Suit switch
            {
                Suit.Hearts => "♥",
                Suit.Diamonds => "♦",
                Suit.Clubs => "♣",
                Suit.Spades => "♠",
                _ => "?"
            };

            return $"{rankStr}{suitStr}";
        }

        public string ToShortString()
        {
            string rankStr = Rank switch
            {
                Rank.Ace => "A",
                Rank.King => "K",
                Rank.Queen => "Q",
                Rank.Jack => "J",
                Rank.Ten => "T",
                _ => ((int)Rank).ToString()
            };

            string suitStr = Suit switch
            {
                Suit.Hearts => "h",
                Suit.Diamonds => "d",
                Suit.Clubs => "c",
                Suit.Spades => "s",
                _ => "?"
            };

            return $"{rankStr}{suitStr}";
        }

        public static Card FromString(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length < 2)
                throw new ArgumentException("Invalid card string");

            char rankChar = char.ToUpper(str[0]);
            char suitChar = char.ToLower(str[1]);

            Rank rank = rankChar switch
            {
                'A' => Rank.Ace,
                'K' => Rank.King,
                'Q' => Rank.Queen,
                'J' => Rank.Jack,
                'T' => Rank.Ten,
                '9' => Rank.Nine,
                '8' => Rank.Eight,
                '7' => Rank.Seven,
                '6' => Rank.Six,
                '5' => Rank.Five,
                '4' => Rank.Four,
                '3' => Rank.Three,
                '2' => Rank.Two,
                _ => throw new ArgumentException($"Invalid rank: {rankChar}")
            };

            Suit suit = suitChar switch
            {
                'h' => Suit.Hearts,
                'd' => Suit.Diamonds,
                'c' => Suit.Clubs,
                's' => Suit.Spades,
                _ => throw new ArgumentException($"Invalid suit: {suitChar}")
            };

            return new Card(suit, rank);
        }

        public static bool operator ==(Card left, Card right)
        {
            if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(Card left, Card right)
        {
            return !(left == right);
        }

        public static bool operator <(Card left, Card right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Card left, Card right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Card left, Card right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Card left, Card right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
