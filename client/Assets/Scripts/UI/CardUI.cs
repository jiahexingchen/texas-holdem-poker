using UnityEngine;
using UnityEngine.UI;
using TexasHoldem.Core;

namespace TexasHoldem.UI
{
    public class CardUI : MonoBehaviour
    {
        [SerializeField] private Image cardImage;
        [SerializeField] private Image rankImage;
        [SerializeField] private Image suitImage;
        [SerializeField] private Sprite cardBackSprite;

        private Card _card;
        private bool _isFaceUp = true;

        public Card Card => _card;
        public bool IsFaceUp => _isFaceUp;

        public void SetCard(Card card)
        {
            _card = card;
            UpdateCardVisual();
        }

        public void SetFaceUp(bool faceUp)
        {
            _isFaceUp = faceUp;
            UpdateCardVisual();
        }

        public void Flip()
        {
            _isFaceUp = !_isFaceUp;
            UpdateCardVisual();
        }

        private void UpdateCardVisual()
        {
            if (cardImage == null) return;

            if (!_isFaceUp || _card == null)
            {
                cardImage.sprite = cardBackSprite ?? GetCardBackSprite();
                if (rankImage != null) rankImage.gameObject.SetActive(false);
                if (suitImage != null) suitImage.gameObject.SetActive(false);
            }
            else
            {
                cardImage.sprite = GetCardSprite(_card);
                if (rankImage != null) rankImage.gameObject.SetActive(true);
                if (suitImage != null) suitImage.gameObject.SetActive(true);
            }
        }

        private Sprite GetCardSprite(Card card)
        {
            string spriteName = card.ToShortString();
            var sprite = Resources.Load<Sprite>($"Cards/{spriteName}");
            
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>($"Cards/{GetCardFileName(card)}");
            }
            
            return sprite;
        }

        private string GetCardFileName(Card card)
        {
            string rankName = card.Rank switch
            {
                Rank.Ace => "ace",
                Rank.King => "king",
                Rank.Queen => "queen",
                Rank.Jack => "jack",
                _ => ((int)card.Rank).ToString()
            };

            string suitName = card.Suit switch
            {
                Suit.Hearts => "hearts",
                Suit.Diamonds => "diamonds",
                Suit.Clubs => "clubs",
                Suit.Spades => "spades",
                _ => "unknown"
            };

            return $"{rankName}_of_{suitName}";
        }

        private Sprite GetCardBackSprite()
        {
            return Resources.Load<Sprite>("Cards/back");
        }

        public void SetColor(Color color)
        {
            if (cardImage != null)
                cardImage.color = color;
        }

        public void ResetColor()
        {
            if (cardImage != null)
                cardImage.color = Color.white;
        }
    }
}
