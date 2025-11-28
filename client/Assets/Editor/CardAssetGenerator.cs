using UnityEngine;
using UnityEditor;
using System.IO;

namespace TexasHoldem.Editor
{
    public class CardAssetGenerator : EditorWindow
    {
        private int cardWidth = 140;
        private int cardHeight = 190;
        private Color redColor = new Color(0.8f, 0.1f, 0.1f);
        private Color blackColor = Color.black;
        private Color backgroundColor = Color.white;
        private Color cardBackColor = new Color(0.2f, 0.3f, 0.6f);

        [MenuItem("Texas Holdem/Generate Card Assets")]
        public static void ShowWindow()
        {
            GetWindow<CardAssetGenerator>("Card Asset Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Card Asset Generator", EditorStyles.boldLabel);
            
            cardWidth = EditorGUILayout.IntField("Card Width", cardWidth);
            cardHeight = EditorGUILayout.IntField("Card Height", cardHeight);
            
            EditorGUILayout.Space();
            
            redColor = EditorGUILayout.ColorField("Red Suit Color", redColor);
            blackColor = EditorGUILayout.ColorField("Black Suit Color", blackColor);
            backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
            cardBackColor = EditorGUILayout.ColorField("Card Back Color", cardBackColor);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate All Cards"))
            {
                GenerateAllCards();
            }
            
            if (GUILayout.Button("Generate Card Back"))
            {
                GenerateCardBack();
            }
            
            if (GUILayout.Button("Generate Chip Assets"))
            {
                GenerateChipAssets();
            }
        }

        private void GenerateAllCards()
        {
            string path = "Assets/Resources/Cards";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string[] suits = { "h", "d", "c", "s" };
            string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A" };
            bool[] isRed = { true, true, false, false };

            int total = suits.Length * ranks.Length;
            int current = 0;

            for (int s = 0; s < suits.Length; s++)
            {
                for (int r = 0; r < ranks.Length; r++)
                {
                    string cardName = ranks[r] + suits[s];
                    Color suitColor = isRed[s] ? redColor : blackColor;
                    
                    Texture2D texture = GenerateCardTexture(ranks[r], suits[s], suitColor);
                    SaveTexture(texture, $"{path}/{cardName}.png");
                    
                    current++;
                    EditorUtility.DisplayProgressBar("Generating Cards", 
                        $"Creating {cardName}...", (float)current / total);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {total} card assets in {path}");
        }

        private Texture2D GenerateCardTexture(string rank, string suit, Color suitColor)
        {
            Texture2D texture = new Texture2D(cardWidth, cardHeight);
            
            // Fill background
            for (int y = 0; y < cardHeight; y++)
            {
                for (int x = 0; x < cardWidth; x++)
                {
                    texture.SetPixel(x, y, backgroundColor);
                }
            }

            // Draw border
            DrawBorder(texture, Color.gray, 2);

            // Draw suit symbol in center
            int centerX = cardWidth / 2;
            int centerY = cardHeight / 2;
            DrawSuitSymbol(texture, suit, centerX, centerY, 30, suitColor);

            // Draw rank in corners
            DrawRankText(texture, rank, 10, cardHeight - 25, suitColor);
            DrawSmallSuit(texture, suit, 10, cardHeight - 50, suitColor);

            texture.Apply();
            return texture;
        }

        private void DrawBorder(Texture2D texture, Color color, int thickness)
        {
            for (int i = 0; i < thickness; i++)
            {
                for (int x = 0; x < cardWidth; x++)
                {
                    texture.SetPixel(x, i, color);
                    texture.SetPixel(x, cardHeight - 1 - i, color);
                }
                for (int y = 0; y < cardHeight; y++)
                {
                    texture.SetPixel(i, y, color);
                    texture.SetPixel(cardWidth - 1 - i, y, color);
                }
            }
        }

        private void DrawSuitSymbol(Texture2D texture, string suit, int cx, int cy, int size, Color color)
        {
            switch (suit)
            {
                case "h":
                    DrawHeart(texture, cx, cy, size, color);
                    break;
                case "d":
                    DrawDiamond(texture, cx, cy, size, color);
                    break;
                case "c":
                    DrawClub(texture, cx, cy, size, color);
                    break;
                case "s":
                    DrawSpade(texture, cx, cy, size, color);
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
            int r = size / 3;
            DrawCircle(texture, cx, cy + r, r, color);
            DrawCircle(texture, cx - r, cy - r / 2, r, color);
            DrawCircle(texture, cx + r, cy - r / 2, r, color);
            
            for (int y = cy - size; y < cy - r / 2; y++)
            {
                for (int x = cx - r / 3; x <= cx + r / 3; x++)
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
            // Inverted heart
            for (int y = -size; y <= size; y++)
            {
                for (int x = -size; x <= size; x++)
                {
                    float fx = x / (float)size;
                    float fy = -y / (float)size;
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
            int r = size / 3;
            for (int y = cy - size; y < cy; y++)
            {
                for (int x = cx - r / 2; x <= cx + r / 2; x++)
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

        private void DrawRankText(Texture2D texture, string rank, int x, int y, Color color)
        {
            // Simple block representation
            int size = 15;
            for (int dy = 0; dy < size; dy++)
            {
                for (int dx = 0; dx < size; dx++)
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

        private void DrawSmallSuit(Texture2D texture, string suit, int x, int y, Color color)
        {
            DrawSuitSymbol(texture, suit, x + 8, y + 8, 8, color);
        }

        private void GenerateCardBack()
        {
            string path = "Assets/Resources/Cards";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Texture2D texture = new Texture2D(cardWidth, cardHeight);
            
            // Pattern
            for (int y = 0; y < cardHeight; y++)
            {
                for (int x = 0; x < cardWidth; x++)
                {
                    bool pattern = ((x / 8) + (y / 8)) % 2 == 0;
                    texture.SetPixel(x, y, pattern ? cardBackColor : cardBackColor * 1.2f);
                }
            }

            // Border
            DrawBorder(texture, Color.white, 3);

            // Center decoration
            int cx = cardWidth / 2;
            int cy = cardHeight / 2;
            DrawCircle(texture, cx, cy, 25, Color.white);
            DrawCircle(texture, cx, cy, 20, cardBackColor);

            texture.Apply();
            SaveTexture(texture, $"{path}/card_back.png");
            
            AssetDatabase.Refresh();
            Debug.Log("Generated card back");
        }

        private void GenerateChipAssets()
        {
            string path = "Assets/Resources/Chips";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var chips = new (int value, Color color)[]
            {
                (1, Color.white),
                (5, Color.red),
                (25, Color.green),
                (100, Color.black),
                (500, new Color(0.5f, 0, 0.5f)),
                (1000, new Color(1f, 0.8f, 0))
            };

            int chipSize = 80;

            foreach (var (value, color) in chips)
            {
                Texture2D texture = new Texture2D(chipSize, chipSize);
                
                // Fill transparent
                for (int y = 0; y < chipSize; y++)
                {
                    for (int x = 0; x < chipSize; x++)
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }

                int cx = chipSize / 2;
                int cy = chipSize / 2;
                int radius = chipSize / 2 - 2;

                // Main chip circle
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (x * x + y * y <= radius * radius)
                        {
                            texture.SetPixel(cx + x, cy + y, color);
                        }
                    }
                }

                // Edge ring
                int innerRadius = radius - 5;
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        int dist = x * x + y * y;
                        if (dist <= radius * radius && dist >= innerRadius * innerRadius)
                        {
                            Color edgeColor = color * 0.7f;
                            edgeColor.a = 1;
                            texture.SetPixel(cx + x, cy + y, edgeColor);
                        }
                    }
                }

                // Center circle
                int centerRadius = 15;
                for (int y = -centerRadius; y <= centerRadius; y++)
                {
                    for (int x = -centerRadius; x <= centerRadius; x++)
                    {
                        if (x * x + y * y <= centerRadius * centerRadius)
                        {
                            texture.SetPixel(cx + x, cy + y, Color.white);
                        }
                    }
                }

                texture.Apply();
                SaveTexture(texture, $"{path}/chip_{value}.png");
            }

            AssetDatabase.Refresh();
            Debug.Log($"Generated {chips.Length} chip assets");
        }

        private void SaveTexture(Texture2D texture, string path)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }
    }
}
