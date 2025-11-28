using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TexasHoldem.Core;

namespace TexasHoldem.UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text chipsText;
        [SerializeField] private TMP_Text betText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image dealerButton;
        [SerializeField] private Image blindIndicator;

        [Header("Cards")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Image card1Image;
        [SerializeField] private Image card2Image;

        [Header("Status")]
        [SerializeField] private GameObject foldOverlay;
        [SerializeField] private GameObject allInIndicator;
        [SerializeField] private TMP_Text actionText;
        [SerializeField] private Image timerBar;
        [SerializeField] private Image highlightBorder;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private Player _player;
        private bool _showCards = false;

        public void UpdatePlayer(Player player)
        {
            _player = player;

            if (playerNameText != null)
                playerNameText.text = player.Name;

            if (chipsText != null)
                chipsText.text = $"${player.Chips:N0}";

            if (betText != null)
            {
                betText.gameObject.SetActive(player.CurrentBet > 0);
                betText.text = $"${player.CurrentBet:N0}";
            }

            UpdateDealerButton(player.IsDealer);
            UpdateBlindIndicator(player.IsSmallBlind, player.IsBigBlind);
            UpdatePlayerState(player.State);
            UpdateLastAction(player.LastAction);

            if (_showCards || !player.IsBot)
            {
                ShowHoleCards(player.HoleCards);
            }
            else
            {
                ShowCardBacks();
            }
        }

        public void SetShowCards(bool show)
        {
            _showCards = show;
            if (_player != null)
            {
                if (show)
                    ShowHoleCards(_player.HoleCards);
                else
                    ShowCardBacks();
            }
        }

        private void UpdateDealerButton(bool isDealer)
        {
            if (dealerButton != null)
                dealerButton.gameObject.SetActive(isDealer);
        }

        private void UpdateBlindIndicator(bool isSmallBlind, bool isBigBlind)
        {
            if (blindIndicator == null) return;

            if (isBigBlind)
            {
                blindIndicator.gameObject.SetActive(true);
                blindIndicator.color = Color.yellow;
            }
            else if (isSmallBlind)
            {
                blindIndicator.gameObject.SetActive(true);
                blindIndicator.color = Color.white;
            }
            else
            {
                blindIndicator.gameObject.SetActive(false);
            }
        }

        private void UpdatePlayerState(PlayerState state)
        {
            if (foldOverlay != null)
                foldOverlay.SetActive(state == PlayerState.Folded);

            if (allInIndicator != null)
                allInIndicator.SetActive(state == PlayerState.AllIn);

            float alpha = state == PlayerState.Folded || state == PlayerState.SittingOut ? 0.5f : 1f;
            SetAlpha(alpha);
        }

        private void UpdateLastAction(PlayerAction action)
        {
            if (actionText == null) return;

            string text = action switch
            {
                PlayerAction.Fold => "FOLD",
                PlayerAction.Check => "CHECK",
                PlayerAction.Call => "CALL",
                PlayerAction.Raise => "RAISE",
                PlayerAction.AllIn => "ALL IN",
                PlayerAction.SmallBlind => "SB",
                PlayerAction.BigBlind => "BB",
                _ => ""
            };

            actionText.text = text;
            actionText.gameObject.SetActive(!string.IsNullOrEmpty(text));

            if (!string.IsNullOrEmpty(text))
            {
                CancelInvoke(nameof(HideActionText));
                Invoke(nameof(HideActionText), 2f);
            }
        }

        private void HideActionText()
        {
            if (actionText != null)
                actionText.gameObject.SetActive(false);
        }

        public void ShowHoleCards(Card[] cards)
        {
            if (cards == null || cards.Length < 2) return;
            if (cards[0] == null || cards[1] == null)
            {
                ShowCardBacks();
                return;
            }

            if (card1Image != null)
            {
                card1Image.sprite = GetCardSprite(cards[0]);
                card1Image.gameObject.SetActive(true);
            }

            if (card2Image != null)
            {
                card2Image.sprite = GetCardSprite(cards[1]);
                card2Image.gameObject.SetActive(true);
            }
        }

        public void ShowCardBacks()
        {
            var backSprite = GetCardBackSprite();
            
            if (card1Image != null)
            {
                card1Image.sprite = backSprite;
                card1Image.gameObject.SetActive(true);
            }

            if (card2Image != null)
            {
                card2Image.sprite = backSprite;
                card2Image.gameObject.SetActive(true);
            }
        }

        public void HideCards()
        {
            if (card1Image != null)
                card1Image.gameObject.SetActive(false);

            if (card2Image != null)
                card2Image.gameObject.SetActive(false);
        }

        private Sprite GetCardSprite(Card card)
        {
            string spriteName = card.ToShortString();
            return Resources.Load<Sprite>($"Cards/{spriteName}");
        }

        private Sprite GetCardBackSprite()
        {
            return Resources.Load<Sprite>("Cards/back");
        }

        public void SetHighlight(bool highlight)
        {
            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(highlight);
            }
        }

        public void UpdateTimer(float normalizedTime)
        {
            if (timerBar != null)
            {
                timerBar.fillAmount = normalizedTime;
                
                if (normalizedTime < 0.25f)
                    timerBar.color = Color.red;
                else if (normalizedTime < 0.5f)
                    timerBar.color = Color.yellow;
                else
                    timerBar.color = Color.green;
            }
        }

        public void SetAvatar(Sprite avatar)
        {
            if (avatarImage != null && avatar != null)
                avatarImage.sprite = avatar;
        }

        private void SetAlpha(float alpha)
        {
            var graphics = GetComponentsInChildren<Graphic>();
            foreach (var graphic in graphics)
            {
                var color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
        }

        public void PlayWinAnimation()
        {
            if (animator != null)
                animator.SetTrigger("Win");
        }

        public void PlayFoldAnimation()
        {
            if (animator != null)
                animator.SetTrigger("Fold");
        }

        public void PlayChipsAnimation()
        {
            if (animator != null)
                animator.SetTrigger("Chips");
        }
    }
}
