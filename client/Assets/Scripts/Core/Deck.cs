using System;
using System.Collections.Generic;

namespace TexasHoldem.Core
{
    public class Deck
    {
        private List<Card> _cards;
        private int _currentIndex;
        private Random _random;

        public int RemainingCards => _cards.Count - _currentIndex;

        public Deck() : this(new Random()) { }

        public Deck(Random random)
        {
            _random = random;
            _cards = new List<Card>(52);
            Reset();
        }

        public Deck(int seed) : this(new Random(seed)) { }

        public void Reset()
        {
            _cards.Clear();
            for (int i = 0; i < 52; i++)
            {
                _cards.Add(new Card(i));
            }
            _currentIndex = 0;
        }

        public void Shuffle()
        {
            _currentIndex = 0;
            // Fisher-Yates shuffle
            for (int i = _cards.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public Card Deal()
        {
            if (_currentIndex >= _cards.Count)
                throw new InvalidOperationException("No more cards in deck");
            
            return _cards[_currentIndex++];
        }

        public Card[] Deal(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
            
            if (_currentIndex + count > _cards.Count)
                throw new InvalidOperationException($"Not enough cards in deck. Requested: {count}, Remaining: {RemainingCards}");
            
            Card[] result = new Card[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = _cards[_currentIndex++];
            }
            return result;
        }

        public void Burn()
        {
            if (_currentIndex >= _cards.Count)
                throw new InvalidOperationException("No more cards to burn");
            
            _currentIndex++;
        }

        public void RemoveCards(IEnumerable<Card> cardsToRemove)
        {
            foreach (var card in cardsToRemove)
            {
                _cards.Remove(card);
            }
        }

        public Card Peek()
        {
            if (_currentIndex >= _cards.Count)
                throw new InvalidOperationException("No more cards in deck");
            
            return _cards[_currentIndex];
        }

        public List<Card> GetRemainingCards()
        {
            return _cards.GetRange(_currentIndex, RemainingCards);
        }
    }
}
