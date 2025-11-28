using System.Collections.Generic;
using UnityEngine;
using TexasHoldem.Core;

namespace TexasHoldem.Utils
{
    public class ResourceManager : MonoBehaviour
    {
        private static ResourceManager _instance;
        public static ResourceManager Instance => _instance;

        [Header("Card Sprites")]
        [SerializeField] private Sprite cardBackSprite;
        [SerializeField] private Sprite[] cardSprites; // 52 cards

        [Header("Chip Sprites")]
        [SerializeField] private Sprite chip1Sprite;
        [SerializeField] private Sprite chip5Sprite;
        [SerializeField] private Sprite chip25Sprite;
        [SerializeField] private Sprite chip100Sprite;
        [SerializeField] private Sprite chip500Sprite;
        [SerializeField] private Sprite chip1000Sprite;

        [Header("UI Sprites")]
        [SerializeField] private Sprite buttonNormalSprite;
        [SerializeField] private Sprite buttonPressedSprite;
        [SerializeField] private Sprite panelBackgroundSprite;
        [SerializeField] private Sprite dealerButtonSprite;
        [SerializeField] private Sprite timerSprite;

        [Header("Avatar Sprites")]
        [SerializeField] private Sprite[] defaultAvatars;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip cardDealSound;
        [SerializeField] private AudioClip cardFlipSound;
        [SerializeField] private AudioClip chipMoveSound;
        [SerializeField] private AudioClip checkSound;
        [SerializeField] private AudioClip callSound;
        [SerializeField] private AudioClip raiseSound;
        [SerializeField] private AudioClip allInSound;
        [SerializeField] private AudioClip foldSound;
        [SerializeField] private AudioClip winSound;
        [SerializeField] private AudioClip loseSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip timerTickSound;
        [SerializeField] private AudioClip menuMusicClip;
        [SerializeField] private AudioClip gameMusicClip;

        private Dictionary<string, Sprite> _cardSpriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeCache();
        }

        private void InitializeCache()
        {
            // Pre-cache card sprites if available
            if (cardSprites != null && cardSprites.Length == 52)
            {
                for (int i = 0; i < 52; i++)
                {
                    var card = new Card(i);
                    string key = GetCardKey(card);
                    _cardSpriteCache[key] = cardSprites[i];
                }
            }
        }

        public Sprite GetCardSprite(Card card)
        {
            string key = GetCardKey(card);
            
            if (_cardSpriteCache.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }

            // Try loading from Resources
            sprite = Resources.Load<Sprite>($"Cards/{key}");
            if (sprite != null)
            {
                _cardSpriteCache[key] = sprite;
                return sprite;
            }

            // Generate programmatically as fallback
            if (CardSpriteGenerator.Instance != null)
            {
                sprite = CardSpriteGenerator.Instance.GenerateCardSprite(card);
                _cardSpriteCache[key] = sprite;
                return sprite;
            }

            return null;
        }

        public Sprite GetCardBackSprite()
        {
            if (cardBackSprite != null) return cardBackSprite;

            var sprite = Resources.Load<Sprite>("Cards/card_back");
            if (sprite != null) return sprite;

            return CardSpriteGenerator.Instance?.GenerateCardBack();
        }

        private string GetCardKey(Card card)
        {
            string suitChar = card.Suit switch
            {
                Suit.Hearts => "h",
                Suit.Diamonds => "d",
                Suit.Clubs => "c",
                Suit.Spades => "s",
                _ => "x"
            };

            string rankChar = card.Rank switch
            {
                Rank.Ace => "A",
                Rank.King => "K",
                Rank.Queen => "Q",
                Rank.Jack => "J",
                Rank.Ten => "T",
                _ => ((int)card.Rank).ToString()
            };

            return $"{rankChar}{suitChar}";
        }

        public Sprite GetChipSprite(long value)
        {
            if (value >= 1000) return chip1000Sprite;
            if (value >= 500) return chip500Sprite;
            if (value >= 100) return chip100Sprite;
            if (value >= 25) return chip25Sprite;
            if (value >= 5) return chip5Sprite;
            return chip1Sprite;
        }

        public Sprite GetAvatar(int index)
        {
            if (defaultAvatars != null && index >= 0 && index < defaultAvatars.Length)
            {
                return defaultAvatars[index];
            }
            return Resources.Load<Sprite>($"Avatars/avatar_{index}");
        }

        public Sprite GetAvatarByName(string name)
        {
            return Resources.Load<Sprite>($"Avatars/{name}");
        }

        // Audio getters
        public AudioClip CardDealSound => cardDealSound ?? Resources.Load<AudioClip>("Audio/card_deal");
        public AudioClip CardFlipSound => cardFlipSound ?? Resources.Load<AudioClip>("Audio/card_flip");
        public AudioClip ChipMoveSound => chipMoveSound ?? Resources.Load<AudioClip>("Audio/chip_move");
        public AudioClip CheckSound => checkSound ?? Resources.Load<AudioClip>("Audio/check");
        public AudioClip CallSound => callSound ?? Resources.Load<AudioClip>("Audio/call");
        public AudioClip RaiseSound => raiseSound ?? Resources.Load<AudioClip>("Audio/raise");
        public AudioClip AllInSound => allInSound ?? Resources.Load<AudioClip>("Audio/allin");
        public AudioClip FoldSound => foldSound ?? Resources.Load<AudioClip>("Audio/fold");
        public AudioClip WinSound => winSound ?? Resources.Load<AudioClip>("Audio/win");
        public AudioClip LoseSound => loseSound ?? Resources.Load<AudioClip>("Audio/lose");
        public AudioClip ButtonClickSound => buttonClickSound ?? Resources.Load<AudioClip>("Audio/button_click");
        public AudioClip TimerTickSound => timerTickSound ?? Resources.Load<AudioClip>("Audio/timer_tick");
        public AudioClip MenuMusic => menuMusicClip ?? Resources.Load<AudioClip>("Audio/menu_music");
        public AudioClip GameMusic => gameMusicClip ?? Resources.Load<AudioClip>("Audio/game_music");
        
        public Sprite DealerButtonSprite => dealerButtonSprite ?? Resources.Load<Sprite>("UI/dealer_button");
        public Sprite TimerSprite => timerSprite ?? Resources.Load<Sprite>("UI/timer");
    }
}
