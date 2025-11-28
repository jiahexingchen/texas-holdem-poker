using UnityEngine;
using TexasHoldem.Core;

namespace TexasHoldem.Utils
{
    public class CardSpriteGenerator : MonoBehaviour
    {
        [Header("Card Settings")]
        [SerializeField] private int cardWidth = 140;
        [SerializeField] private int cardHeight = 190;
        [SerializeField] private int cornerRadius = 10;

        [Header("Colors")]
        [SerializeField] private Color cardBackgroundColor = Color.white;
        [SerializeField] private Color redSuitColor = new Color(0.8f, 0.1f, 0.1f);
        [SerializeField] private Color blackSuitColor = Color.black;
        [SerializeField] private Color cardBackColor = new Color(0.2f, 0.3f, 0.6f);
        [SerializeField] private Color cardBackPatternColor = new Color(0.3f, 0.4f, 0.7f);

        private static CardSpriteGenerator _instance;
        public static CardSpriteGenerator Instance => _instance;

        private void Awake()
        {
            _instance = this;
        }

        public Sprite GenerateCardSprite(Card card)
        {
            Texture2D texture = new Texture2D(cardWidth, cardHeight);
            
            // Fill background
            Color[] pixels = new Color[cardWidth * cardHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = cardBackgroundColor;
            }
            texture.SetPixels(pixels);

            // Draw border
            DrawBorder(texture, Color.gray, 2);

            // Get suit color
            Color suitColor = (card.Suit == Suit.Hearts || card.Suit == Suit.Diamonds) 
                ? redSuitColor 
                : blackSuitColor;

            // Draw rank
            DrawRank(texture, card.Rank, suitColor);

            // Draw suit symbol
            DrawSuit(texture, card.Suit, suitColor);

            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, cardWidth, cardHeight), new Vector2(0.5f, 0.5f), 100);
        }

        public Sprite GenerateCardBack()
        {
            Texture2D texture = new Texture2D(cardWidth, cardHeight);
            
            // Fill with pattern
            for (int y = 0; y < cardHeight; y++)
            {
                for (int x = 0; x < cardWidth; x++)
                {
                    bool pattern = ((x / 10) + (y / 10)) % 2 == 0;
                    texture.SetPixel(x, y, pattern ? cardBackColor : cardBackPatternColor);
                }
            }

            // Draw border
            DrawBorder(texture, Color.white, 3);

            // Draw center decoration
            DrawCenterDecoration(texture);

            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, cardWidth, cardHeight), new Vector2(0.5f, 0.5f), 100);
        }

        private void DrawBorder(Texture2D texture, Color color, int thickness)
        {
            for (int i = 0; i < thickness; i++)
            {
                // Top and bottom
                for (int x = 0; x < cardWidth; x++)
                {
                    texture.SetPixel(x, i, color);
                    texture.SetPixel(x, cardHeight - 1 - i, color);
                }
                // Left and right
                for (int y = 0; y < cardHeight; y++)
                {
                    texture.SetPixel(i, y, color);
                    texture.SetPixel(cardWidth - 1 - i, y, color);
                }
            }
        }

        private void DrawRank(Texture2D texture, Rank rank, Color color)
        {
            string rankStr = GetRankString(rank);
            int startX = 10;
            int startY = cardHeight - 30;

            // Simple text rendering (placeholder - in real game use TextMeshPro)
            DrawText(texture, rankStr, startX, startY, color, 20);
        }

        private void DrawSuit(Texture2D texture, Suit suit, Color color)
        {
            int centerX = cardWidth / 2;
            int centerY = cardHeight / 2;
            int size = 40;

            switch (suit)
            {
                case Suit.Hearts:
                    DrawHeart(texture, centerX, centerY, size, color);
                    break;
                case Suit.Diamonds:
                    DrawDiamond(texture, centerX, centerY, size, color);
                    break;
                case Suit.Clubs:
                    DrawClub(texture, centerX, centerY, size, color);
                    break;
                case Suit.Spades:
                    DrawSpade(texture, centerX, centerY, size, color);
                    break;
            }
        }

        private void DrawHeart(Texture2D texture, int cx, int cy, int size, Color color)
        {
            for (int y = -size; y <= size; y++)
            {
                for (int x = -size; x <= size; x++)
                {
                    float fx = x / (float)size;
                    float fy = y / (float)size;
                    
                    // Heart equation
                    float heart = Mathf.Pow(fx * fx + fy * fy - 1, 3) - fx * fx * fy * fy * fy;
                    
                    if (heart <= 0)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < cardWidth && py >= 0 && py < cardHeight)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        private void DrawDiamond(Texture2D texture, int cx, int cy, int size, Color color)
        {
            for (int y = -size; y <= size; y++)
            {
                for (int x = -size; x <= size; x++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= size)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < cardWidth && py >= 0 && py < cardHeight)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        private void DrawClub(Texture2D texture, int cx, int cy, int size, Color color)
        {
            int radius = size / 3;
            
            // Three circles
            DrawCircle(texture, cx, cy + radius, radius, color);
            DrawCircle(texture, cx - radius, cy - radius / 2, radius, color);
            DrawCircle(texture, cx + radius, cy - radius / 2, radius, color);
            
            // Stem
            for (int y = cy - size; y < cy - radius / 2; y++)
            {
                for (int x = cx - radius / 3; x <= cx + radius / 3; x++)
                {
                    if (x >= 0 && x < cardWidth && y >= 0 && y < cardHeight)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void DrawSpade(Texture2D texture, int cx, int cy, int size, Color color)
        {
            // Inverted heart shape
            for (int y = -size; y <= size; y++)
            {
                for (int x = -size; x <= size; x++)
                {
                    float fx = x / (float)size;
                    float fy = -y / (float)size; // Invert
                    
                    float heart = Mathf.Pow(fx * fx + fy * fy - 1, 3) - fx * fx * fy * fy * fy;
                    
                    if (heart <= 0)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < cardWidth && py >= 0 && py < cardHeight)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
            
            // Stem
            int radius = size / 3;
            for (int y = cy - size; y < cy; y++)
            {
                for (int x = cx - radius / 2; x <= cx + radius / 2; x++)
                {
                    if (x >= 0 && x < cardWidth && y >= 0 && y < cardHeight)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < cardWidth && py >= 0 && py < cardHeight)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }

        private void DrawCenterDecoration(Texture2D texture)
        {
            int cx = cardWidth / 2;
            int cy = cardHeight / 2;
            int radius = 20;

            DrawCircle(texture, cx, cy, radius, Color.white);
            DrawCircle(texture, cx, cy, radius - 3, cardBackColor);
        }

        private void DrawText(Texture2D texture, string text, int x, int y, Color color, int size)
        {
            // Simplified text drawing - in production use proper font rendering
            // This just draws a colored rectangle as placeholder
            for (int dy = 0; dy < size; dy++)
            {
                for (int dx = 0; dx < size * text.Length / 2; dx++)
                {
                    int px = x + dx;
                    int py = y - dy;
                    if (px >= 0 && px < cardWidth && py >= 0 && py < cardHeight)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }

        private string GetRankString(Rank rank)
        {
            return rank switch
            {
                Rank.Ace => "A",
                Rank.King => "K",
                Rank.Queen => "Q",
                Rank.Jack => "J",
                Rank.Ten => "10",
                _ => ((int)rank).ToString()
            };
        }

        public static Sprite GetCardSprite(Card card)
        {
            if (Instance != null)
            {
                return Instance.GenerateCardSprite(card);
            }
            return null;
        }

        public static Sprite GetCardBackSprite()
        {
            if (Instance != null)
            {
                return Instance.GenerateCardBack();
            }
            return null;
        }
    }
}
