using NUnit.Framework;
using TexasHoldem.Core;
using System.Linq;

namespace TexasHoldem.Tests
{
    public class CardTests
    {
        [Test]
        public void Card_Constructor_ValidCard()
        {
            var card = new Card(Suit.Hearts, Rank.Ace);
            
            Assert.AreEqual(Suit.Hearts, card.Suit);
            Assert.AreEqual(Rank.Ace, card.Rank);
        }

        [Test]
        public void Card_FromIndex_ValidConversion()
        {
            var card = new Card(0); // First card: 2 of Hearts
            
            Assert.AreEqual(Suit.Hearts, card.Suit);
            Assert.AreEqual(Rank.Two, card.Rank);
        }

        [Test]
        public void Card_ToIndex_RoundTrip()
        {
            for (int i = 0; i < 52; i++)
            {
                var card = new Card(i);
                Assert.AreEqual(i, card.ToIndex());
            }
        }

        [Test]
        public void Card_ToString_CorrectFormat()
        {
            var aceHearts = new Card(Suit.Hearts, Rank.Ace);
            Assert.AreEqual("A♥", aceHearts.ToString());

            var tenSpades = new Card(Suit.Spades, Rank.Ten);
            Assert.AreEqual("T♠", tenSpades.ToString());
        }

        [Test]
        public void Card_FromString_ParsesCorrectly()
        {
            var card = Card.FromString("Ah");
            Assert.AreEqual(Suit.Hearts, card.Suit);
            Assert.AreEqual(Rank.Ace, card.Rank);
        }

        [Test]
        public void Card_Comparison_ByRank()
        {
            var ace = new Card(Suit.Hearts, Rank.Ace);
            var king = new Card(Suit.Spades, Rank.King);
            
            Assert.IsTrue(ace > king);
            Assert.IsTrue(king < ace);
        }
    }

    public class DeckTests
    {
        [Test]
        public void Deck_NewDeck_Has52Cards()
        {
            var deck = new Deck();
            Assert.AreEqual(52, deck.RemainingCards);
        }

        [Test]
        public void Deck_Deal_ReducesCount()
        {
            var deck = new Deck();
            deck.Shuffle();
            deck.Deal();
            
            Assert.AreEqual(51, deck.RemainingCards);
        }

        [Test]
        public void Deck_DealMultiple_CorrectCount()
        {
            var deck = new Deck();
            deck.Shuffle();
            var cards = deck.Deal(5);
            
            Assert.AreEqual(5, cards.Length);
            Assert.AreEqual(47, deck.RemainingCards);
        }

        [Test]
        public void Deck_Reset_Restores52Cards()
        {
            var deck = new Deck();
            deck.Shuffle();
            deck.Deal(10);
            deck.Reset();
            
            Assert.AreEqual(52, deck.RemainingCards);
        }

        [Test]
        public void Deck_Shuffle_RandomOrder()
        {
            var deck1 = new Deck(12345);
            var deck2 = new Deck(12345);
            
            deck1.Shuffle();
            deck2.Shuffle();
            
            // Same seed should produce same order
            for (int i = 0; i < 10; i++)
            {
                var card1 = deck1.Deal();
                var card2 = deck2.Deal();
                Assert.AreEqual(card1, card2);
            }
        }
    }

    public class HandEvaluatorTests
    {
        [Test]
        public void Evaluate_RoyalFlush()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.Ace),
                new Card(Suit.Hearts, Rank.King)
            };
            var community = new[] {
                new Card(Suit.Hearts, Rank.Queen),
                new Card(Suit.Hearts, Rank.Jack),
                new Card(Suit.Hearts, Rank.Ten),
                new Card(Suit.Clubs, Rank.Two),
                new Card(Suit.Diamonds, Rank.Three)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.RoyalFlush, result.Type);
        }

        [Test]
        public void Evaluate_StraightFlush()
        {
            var hole = new[] {
                new Card(Suit.Spades, Rank.Nine),
                new Card(Suit.Spades, Rank.Eight)
            };
            var community = new[] {
                new Card(Suit.Spades, Rank.Seven),
                new Card(Suit.Spades, Rank.Six),
                new Card(Suit.Spades, Rank.Five),
                new Card(Suit.Hearts, Rank.Ace),
                new Card(Suit.Diamonds, Rank.King)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.StraightFlush, result.Type);
        }

        [Test]
        public void Evaluate_FourOfAKind()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.Ace),
                new Card(Suit.Spades, Rank.Ace)
            };
            var community = new[] {
                new Card(Suit.Diamonds, Rank.Ace),
                new Card(Suit.Clubs, Rank.Ace),
                new Card(Suit.Hearts, Rank.King),
                new Card(Suit.Spades, Rank.Queen),
                new Card(Suit.Diamonds, Rank.Jack)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.FourOfAKind, result.Type);
        }

        [Test]
        public void Evaluate_FullHouse()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.King),
                new Card(Suit.Spades, Rank.King)
            };
            var community = new[] {
                new Card(Suit.Diamonds, Rank.King),
                new Card(Suit.Clubs, Rank.Queen),
                new Card(Suit.Hearts, Rank.Queen),
                new Card(Suit.Spades, Rank.Two),
                new Card(Suit.Diamonds, Rank.Three)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.FullHouse, result.Type);
        }

        [Test]
        public void Evaluate_Flush()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.Ace),
                new Card(Suit.Hearts, Rank.Ten)
            };
            var community = new[] {
                new Card(Suit.Hearts, Rank.Eight),
                new Card(Suit.Hearts, Rank.Five),
                new Card(Suit.Hearts, Rank.Two),
                new Card(Suit.Spades, Rank.King),
                new Card(Suit.Diamonds, Rank.Queen)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.Flush, result.Type);
        }

        [Test]
        public void Evaluate_Straight()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.Nine),
                new Card(Suit.Spades, Rank.Eight)
            };
            var community = new[] {
                new Card(Suit.Diamonds, Rank.Seven),
                new Card(Suit.Clubs, Rank.Six),
                new Card(Suit.Hearts, Rank.Five),
                new Card(Suit.Spades, Rank.Ace),
                new Card(Suit.Diamonds, Rank.King)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.Straight, result.Type);
        }

        [Test]
        public void Evaluate_WheelStraight()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.Ace),
                new Card(Suit.Spades, Rank.Two)
            };
            var community = new[] {
                new Card(Suit.Diamonds, Rank.Three),
                new Card(Suit.Clubs, Rank.Four),
                new Card(Suit.Hearts, Rank.Five),
                new Card(Suit.Spades, Rank.King),
                new Card(Suit.Diamonds, Rank.Queen)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.Straight, result.Type);
            Assert.AreEqual(Rank.Five, result.Kickers[0]); // Wheel high card is 5
        }

        [Test]
        public void Evaluate_ThreeOfAKind()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.Jack),
                new Card(Suit.Spades, Rank.Jack)
            };
            var community = new[] {
                new Card(Suit.Diamonds, Rank.Jack),
                new Card(Suit.Clubs, Rank.King),
                new Card(Suit.Hearts, Rank.Queen),
                new Card(Suit.Spades, Rank.Two),
                new Card(Suit.Diamonds, Rank.Three)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.ThreeOfAKind, result.Type);
        }

        [Test]
        public void Evaluate_TwoPair()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.King),
                new Card(Suit.Spades, Rank.Queen)
            };
            var community = new[] {
                new Card(Suit.Diamonds, Rank.King),
                new Card(Suit.Clubs, Rank.Queen),
                new Card(Suit.Hearts, Rank.Two),
                new Card(Suit.Spades, Rank.Five),
                new Card(Suit.Diamonds, Rank.Seven)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.TwoPair, result.Type);
        }

        [Test]
        public void Evaluate_OnePair()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.Ace),
                new Card(Suit.Spades, Rank.Ace)
            };
            var community = new[] {
                new Card(Suit.Diamonds, Rank.King),
                new Card(Suit.Clubs, Rank.Queen),
                new Card(Suit.Hearts, Rank.Jack),
                new Card(Suit.Spades, Rank.Two),
                new Card(Suit.Diamonds, Rank.Three)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.OnePair, result.Type);
        }

        [Test]
        public void Evaluate_HighCard()
        {
            var hole = new[] {
                new Card(Suit.Hearts, Rank.Ace),
                new Card(Suit.Spades, Rank.King)
            };
            var community = new[] {
                new Card(Suit.Diamonds, Rank.Ten),
                new Card(Suit.Clubs, Rank.Eight),
                new Card(Suit.Hearts, Rank.Six),
                new Card(Suit.Spades, Rank.Four),
                new Card(Suit.Diamonds, Rank.Two)
            };

            var result = HandEvaluator.Evaluate(hole, community);
            
            Assert.AreEqual(HandRankType.HighCard, result.Type);
        }

        [Test]
        public void HandRank_Comparison()
        {
            var flush = new HandRank(HandRankType.Flush, new[] { Rank.Ace, Rank.King, Rank.Queen, Rank.Jack, Rank.Ten }, null);
            var straight = new HandRank(HandRankType.Straight, new[] { Rank.King }, null);
            
            Assert.IsTrue(flush > straight);
            Assert.IsTrue(straight < flush);
        }

        [Test]
        public void DetermineWinners_SingleWinner()
        {
            var hands = new System.Collections.Generic.List<HandRank>
            {
                new HandRank(HandRankType.OnePair, new[] { Rank.King }, null),
                new HandRank(HandRankType.TwoPair, new[] { Rank.Ace, Rank.King }, null),
                new HandRank(HandRankType.OnePair, new[] { Rank.Queen }, null)
            };

            var winners = HandEvaluator.DetermineWinners(hands);
            
            Assert.AreEqual(1, winners.Count);
            Assert.AreEqual(1, winners[0]); // Index of TwoPair
        }

        [Test]
        public void DetermineWinners_SplitPot()
        {
            var hands = new System.Collections.Generic.List<HandRank>
            {
                new HandRank(HandRankType.Straight, new[] { Rank.King }, null),
                new HandRank(HandRankType.Straight, new[] { Rank.King }, null)
            };

            var winners = HandEvaluator.DetermineWinners(hands);
            
            Assert.AreEqual(2, winners.Count);
        }
    }

    public class PlayerTests
    {
        [Test]
        public void Player_Constructor()
        {
            var player = new Player("p1", "Test", 1000);
            
            Assert.AreEqual("p1", player.Id);
            Assert.AreEqual("Test", player.Name);
            Assert.AreEqual(1000, player.Chips);
            Assert.AreEqual(PlayerState.Waiting, player.State);
        }

        [Test]
        public void Player_PlaceBet_DeductsChips()
        {
            var player = new Player("p1", "Test", 1000);
            
            var bet = player.PlaceBet(100);
            
            Assert.AreEqual(100, bet);
            Assert.AreEqual(900, player.Chips);
            Assert.AreEqual(100, player.CurrentBet);
        }

        [Test]
        public void Player_PlaceBet_AllIn()
        {
            var player = new Player("p1", "Test", 100);
            player.SetState(PlayerState.Active);
            
            var bet = player.PlaceBet(200);
            
            Assert.AreEqual(100, bet);
            Assert.AreEqual(0, player.Chips);
            Assert.AreEqual(PlayerState.AllIn, player.State);
        }

        [Test]
        public void Player_Fold()
        {
            var player = new Player("p1", "Test", 1000);
            player.SetState(PlayerState.Active);
            
            player.Fold();
            
            Assert.AreEqual(PlayerState.Folded, player.State);
            Assert.AreEqual(PlayerAction.Fold, player.LastAction);
        }

        [Test]
        public void Player_Reset()
        {
            var player = new Player("p1", "Test", 1000);
            player.PlaceBet(100);
            player.SetHoleCards(new Card(Suit.Hearts, Rank.Ace), new Card(Suit.Spades, Rank.King));
            
            player.Reset();
            
            Assert.AreEqual(0, player.CurrentBet);
            Assert.AreEqual(PlayerAction.None, player.LastAction);
        }
    }
}
