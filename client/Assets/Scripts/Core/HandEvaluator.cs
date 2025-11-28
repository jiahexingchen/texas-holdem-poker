using System;
using System.Collections.Generic;
using System.Linq;

namespace TexasHoldem.Core
{
    public static class HandEvaluator
    {
        public static HandRank Evaluate(Card[] holeCards, Card[] communityCards)
        {
            if (holeCards == null || holeCards.Length != 2)
                throw new ArgumentException("Must have exactly 2 hole cards");
            
            if (communityCards == null || communityCards.Length < 3 || communityCards.Length > 5)
                throw new ArgumentException("Must have 3-5 community cards");

            var allCards = holeCards.Concat(communityCards).ToArray();
            return EvaluateBestHand(allCards);
        }

        public static HandRank EvaluateBestHand(Card[] cards)
        {
            if (cards.Length < 5)
                throw new ArgumentException("Need at least 5 cards to evaluate");

            HandRank bestHand = null;
            var combinations = GetCombinations(cards, 5);

            foreach (var combo in combinations)
            {
                var hand = EvaluateFiveCards(combo);
                if (bestHand == null || hand > bestHand)
                {
                    bestHand = hand;
                }
            }

            return bestHand;
        }

        private static HandRank EvaluateFiveCards(Card[] cards)
        {
            if (cards.Length != 5)
                throw new ArgumentException("Must have exactly 5 cards");

            var sortedCards = cards.OrderByDescending(c => c.Rank).ToArray();
            
            bool isFlush = IsFlush(sortedCards);
            bool isStraight = IsStraight(sortedCards, out Rank highCard);
            var groups = GetRankGroups(sortedCards);

            // Royal Flush
            if (isFlush && isStraight && highCard == Rank.Ace)
            {
                return new HandRank(HandRankType.RoyalFlush, new[] { Rank.Ace }, sortedCards);
            }

            // Straight Flush
            if (isFlush && isStraight)
            {
                return new HandRank(HandRankType.StraightFlush, new[] { highCard }, sortedCards);
            }

            // Four of a Kind
            var fourOfKind = groups.FirstOrDefault(g => g.Count == 4);
            if (fourOfKind != null)
            {
                var kicker = groups.First(g => g.Count == 1);
                var kickers = new[] { fourOfKind.Rank, kicker.Rank };
                var bestHand = fourOfKind.Cards.Concat(kicker.Cards).ToArray();
                return new HandRank(HandRankType.FourOfAKind, kickers, bestHand);
            }

            // Full House
            var threeOfKind = groups.FirstOrDefault(g => g.Count == 3);
            var pair = groups.FirstOrDefault(g => g.Count == 2);
            if (threeOfKind != null && pair != null)
            {
                var kickers = new[] { threeOfKind.Rank, pair.Rank };
                var bestHand = threeOfKind.Cards.Concat(pair.Cards).ToArray();
                return new HandRank(HandRankType.FullHouse, kickers, bestHand);
            }

            // Flush
            if (isFlush)
            {
                var kickers = sortedCards.Select(c => c.Rank).ToArray();
                return new HandRank(HandRankType.Flush, kickers, sortedCards);
            }

            // Straight
            if (isStraight)
            {
                return new HandRank(HandRankType.Straight, new[] { highCard }, sortedCards);
            }

            // Three of a Kind
            if (threeOfKind != null)
            {
                var kickers = new List<Rank> { threeOfKind.Rank };
                var singleCards = groups.Where(g => g.Count == 1).OrderByDescending(g => g.Rank).Take(2);
                kickers.AddRange(singleCards.Select(g => g.Rank));
                var bestHand = threeOfKind.Cards.Concat(singleCards.SelectMany(g => g.Cards)).ToArray();
                return new HandRank(HandRankType.ThreeOfAKind, kickers.ToArray(), bestHand);
            }

            // Two Pair
            var pairs = groups.Where(g => g.Count == 2).OrderByDescending(g => g.Rank).ToList();
            if (pairs.Count >= 2)
            {
                var kickers = new List<Rank> { pairs[0].Rank, pairs[1].Rank };
                var kickerCard = groups.Where(g => g.Count == 1).OrderByDescending(g => g.Rank).First();
                kickers.Add(kickerCard.Rank);
                var bestHand = pairs[0].Cards.Concat(pairs[1].Cards).Concat(kickerCard.Cards).ToArray();
                return new HandRank(HandRankType.TwoPair, kickers.ToArray(), bestHand);
            }

            // One Pair
            if (pair != null)
            {
                var kickers = new List<Rank> { pair.Rank };
                var singleCards = groups.Where(g => g.Count == 1).OrderByDescending(g => g.Rank).Take(3);
                kickers.AddRange(singleCards.Select(g => g.Rank));
                var bestHand = pair.Cards.Concat(singleCards.SelectMany(g => g.Cards)).ToArray();
                return new HandRank(HandRankType.OnePair, kickers.ToArray(), bestHand);
            }

            // High Card
            var highCardKickers = sortedCards.Select(c => c.Rank).ToArray();
            return new HandRank(HandRankType.HighCard, highCardKickers, sortedCards);
        }

        private static bool IsFlush(Card[] cards)
        {
            return cards.All(c => c.Suit == cards[0].Suit);
        }

        private static bool IsStraight(Card[] cards, out Rank highCard)
        {
            var ranks = cards.Select(c => (int)c.Rank).OrderByDescending(r => r).ToArray();
            highCard = (Rank)ranks[0];

            // Check for A-2-3-4-5 (wheel)
            if (ranks[0] == 14 && ranks[1] == 5 && ranks[2] == 4 && ranks[3] == 3 && ranks[4] == 2)
            {
                highCard = Rank.Five;
                return true;
            }

            // Check for regular straight
            for (int i = 0; i < 4; i++)
            {
                if (ranks[i] - ranks[i + 1] != 1)
                    return false;
            }

            return true;
        }

        private static List<RankGroup> GetRankGroups(Card[] cards)
        {
            return cards
                .GroupBy(c => c.Rank)
                .Select(g => new RankGroup
                {
                    Rank = g.Key,
                    Count = g.Count(),
                    Cards = g.ToArray()
                })
                .OrderByDescending(g => g.Count)
                .ThenByDescending(g => g.Rank)
                .ToList();
        }

        private static IEnumerable<Card[]> GetCombinations(Card[] cards, int k)
        {
            int n = cards.Length;
            int[] indices = new int[k];
            
            for (int i = 0; i < k; i++)
                indices[i] = i;

            while (true)
            {
                Card[] result = new Card[k];
                for (int i = 0; i < k; i++)
                    result[i] = cards[indices[i]];
                yield return result;

                int j = k - 1;
                while (j >= 0 && indices[j] == n - k + j)
                    j--;

                if (j < 0) yield break;

                indices[j]++;
                for (int i = j + 1; i < k; i++)
                    indices[i] = indices[i - 1] + 1;
            }
        }

        public static int CompareHands(HandRank hand1, HandRank hand2)
        {
            return hand1.CompareTo(hand2);
        }

        public static List<int> DetermineWinners(List<HandRank> hands)
        {
            if (hands == null || hands.Count == 0)
                return new List<int>();

            HandRank bestHand = hands[0];
            var winners = new List<int> { 0 };

            for (int i = 1; i < hands.Count; i++)
            {
                int compare = hands[i].CompareTo(bestHand);
                if (compare > 0)
                {
                    bestHand = hands[i];
                    winners.Clear();
                    winners.Add(i);
                }
                else if (compare == 0)
                {
                    winners.Add(i);
                }
            }

            return winners;
        }

        private class RankGroup
        {
            public Rank Rank { get; set; }
            public int Count { get; set; }
            public Card[] Cards { get; set; }
        }
    }
}
