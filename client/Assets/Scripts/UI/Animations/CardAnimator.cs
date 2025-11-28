using System;
using System.Collections;
using UnityEngine;

namespace TexasHoldem.UI.Animations
{
    public class CardAnimator : MonoBehaviour
    {
        [Header("Deal Animation")]
        [SerializeField] private float dealDuration = 0.3f;
        [SerializeField] private AnimationCurve dealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private Vector3 deckPosition = new Vector3(0, 200, 0);

        [Header("Flip Animation")]
        [SerializeField] private float flipDuration = 0.2f;

        [Header("Win Animation")]
        [SerializeField] private float winPulseDuration = 0.5f;
        [SerializeField] private float winPulseScale = 1.2f;

        public static CardAnimator Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public void DealCard(Transform card, Vector3 targetPosition, float delay = 0, Action onComplete = null)
        {
            StartCoroutine(DealCardCoroutine(card, targetPosition, delay, onComplete));
        }

        private IEnumerator DealCardCoroutine(Transform card, Vector3 targetPosition, float delay, Action onComplete)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            Vector3 startPos = deckPosition;
            card.localPosition = startPos;
            card.gameObject.SetActive(true);

            float elapsed = 0;
            while (elapsed < dealDuration)
            {
                elapsed += Time.deltaTime;
                float t = dealCurve.Evaluate(elapsed / dealDuration);
                card.localPosition = Vector3.Lerp(startPos, targetPosition, t);
                yield return null;
            }

            card.localPosition = targetPosition;
            onComplete?.Invoke();
        }

        public void DealCards(Transform[] cards, Vector3[] targetPositions, float delayBetween = 0.1f, Action onComplete = null)
        {
            StartCoroutine(DealCardsCoroutine(cards, targetPositions, delayBetween, onComplete));
        }

        private IEnumerator DealCardsCoroutine(Transform[] cards, Vector3[] targetPositions, float delayBetween, Action onComplete)
        {
            int completed = 0;
            int total = cards.Length;

            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                DealCard(cards[i], targetPositions[i], i * delayBetween, () =>
                {
                    completed++;
                    if (completed >= total)
                    {
                        onComplete?.Invoke();
                    }
                });
            }

            yield return null;
        }

        public void FlipCard(Transform card, bool faceUp, Action onFlipMidpoint = null, Action onComplete = null)
        {
            StartCoroutine(FlipCardCoroutine(card, faceUp, onFlipMidpoint, onComplete));
        }

        private IEnumerator FlipCardCoroutine(Transform card, bool faceUp, Action onFlipMidpoint, Action onComplete)
        {
            float startAngle = faceUp ? 180f : 0f;
            float endAngle = faceUp ? 0f : 180f;
            float halfDuration = flipDuration / 2f;

            // First half - flip to 90 degrees
            float elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float angle = Mathf.Lerp(startAngle, 90f, t);
                card.localRotation = Quaternion.Euler(0, angle, 0);
                yield return null;
            }

            // Midpoint - change card face
            onFlipMidpoint?.Invoke();

            // Second half - flip to final angle
            elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float angle = Mathf.Lerp(90f, endAngle, t);
                card.localRotation = Quaternion.Euler(0, angle, 0);
                yield return null;
            }

            card.localRotation = Quaternion.Euler(0, endAngle, 0);
            onComplete?.Invoke();
        }

        public void PulseWinningCards(Transform[] cards, Action onComplete = null)
        {
            StartCoroutine(PulseWinningCardsCoroutine(cards, onComplete));
        }

        private IEnumerator PulseWinningCardsCoroutine(Transform[] cards, Action onComplete)
        {
            Vector3[] originalScales = new Vector3[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                originalScales[i] = cards[i].localScale;
            }

            // Pulse up
            float elapsed = 0;
            float halfDuration = winPulseDuration / 2f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(1f, winPulseScale, t);

                foreach (var card in cards)
                {
                    card.localScale = originalScales[0] * scale;
                }
                yield return null;
            }

            // Pulse down
            elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float scale = Mathf.Lerp(winPulseScale, 1f, t);

                foreach (var card in cards)
                {
                    card.localScale = originalScales[0] * scale;
                }
                yield return null;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].localScale = originalScales[i];
            }

            onComplete?.Invoke();
        }

        public void MoveCardToMuck(Transform card, Vector3 muckPosition, Action onComplete = null)
        {
            StartCoroutine(MoveToMuckCoroutine(card, muckPosition, onComplete));
        }

        private IEnumerator MoveToMuckCoroutine(Transform card, Vector3 muckPosition, Action onComplete)
        {
            Vector3 startPos = card.localPosition;
            float duration = 0.3f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                card.localPosition = Vector3.Lerp(startPos, muckPosition, t);
                card.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.5f, t);
                yield return null;
            }

            card.gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        public void ShakeCard(Transform card, float intensity = 5f, float duration = 0.3f)
        {
            StartCoroutine(ShakeCardCoroutine(card, intensity, duration));
        }

        private IEnumerator ShakeCardCoroutine(Transform card, float intensity, float duration)
        {
            Vector3 originalPos = card.localPosition;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = UnityEngine.Random.Range(-1f, 1f) * intensity * (1 - elapsed / duration);
                float y = UnityEngine.Random.Range(-1f, 1f) * intensity * (1 - elapsed / duration);
                card.localPosition = originalPos + new Vector3(x, y, 0);
                yield return null;
            }

            card.localPosition = originalPos;
        }
    }
}
