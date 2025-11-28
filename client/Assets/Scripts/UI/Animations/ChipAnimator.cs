using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TexasHoldem.Utils;

namespace TexasHoldem.UI.Animations
{
    public class ChipAnimator : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject chipPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float chipMoveDuration = 0.4f;
        [SerializeField] private float chipSpawnDelay = 0.05f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float arcHeight = 50f;

        [Header("Positions")]
        [SerializeField] private Transform potPosition;
        [SerializeField] private Transform[] playerChipPositions;

        private GameObjectPool _chipPool;

        public static ChipAnimator Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            if (chipPrefab != null)
            {
                _chipPool = new GameObjectPool(chipPrefab, transform, 20, 100);
            }
        }

        public void AnimateBetToPot(int seatIndex, long amount, Action onComplete = null)
        {
            if (seatIndex < 0 || seatIndex >= playerChipPositions.Length)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 startPos = playerChipPositions[seatIndex].position;
            Vector3 endPos = potPosition.position;

            int chipCount = CalculateChipCount(amount);
            StartCoroutine(AnimateChipsCoroutine(startPos, endPos, chipCount, onComplete));
        }

        public void AnimatePotToPlayer(int seatIndex, long amount, Action onComplete = null)
        {
            if (seatIndex < 0 || seatIndex >= playerChipPositions.Length)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 startPos = potPosition.position;
            Vector3 endPos = playerChipPositions[seatIndex].position;

            int chipCount = CalculateChipCount(amount);
            StartCoroutine(AnimateChipsCoroutine(startPos, endPos, chipCount, onComplete));
        }

        public void AnimatePlayerToPlayer(int fromSeat, int toSeat, long amount, Action onComplete = null)
        {
            if (fromSeat < 0 || fromSeat >= playerChipPositions.Length ||
                toSeat < 0 || toSeat >= playerChipPositions.Length)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 startPos = playerChipPositions[fromSeat].position;
            Vector3 endPos = playerChipPositions[toSeat].position;

            int chipCount = CalculateChipCount(amount);
            StartCoroutine(AnimateChipsCoroutine(startPos, endPos, chipCount, onComplete));
        }

        private IEnumerator AnimateChipsCoroutine(Vector3 startPos, Vector3 endPos, int chipCount, Action onComplete)
        {
            List<GameObject> chips = new List<GameObject>();
            int completedCount = 0;

            for (int i = 0; i < chipCount; i++)
            {
                var chip = _chipPool?.Get();
                if (chip == null) continue;

                chips.Add(chip);
                chip.transform.position = startPos;

                float randomOffset = UnityEngine.Random.Range(-10f, 10f);
                Vector3 offset = new Vector3(randomOffset, randomOffset, 0);

                StartCoroutine(MoveChipCoroutine(chip, startPos + offset, endPos + offset, () =>
                {
                    completedCount++;
                    _chipPool?.Return(chip);

                    if (completedCount >= chips.Count)
                    {
                        onComplete?.Invoke();
                    }
                }));

                yield return new WaitForSeconds(chipSpawnDelay);
            }

            if (chipCount == 0)
            {
                onComplete?.Invoke();
            }
        }

        private IEnumerator MoveChipCoroutine(GameObject chip, Vector3 startPos, Vector3 endPos, Action onComplete)
        {
            float elapsed = 0;

            while (elapsed < chipMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = moveCurve.Evaluate(elapsed / chipMoveDuration);

                Vector3 pos = Vector3.Lerp(startPos, endPos, t);
                
                // Add arc
                float arcProgress = Mathf.Sin(t * Mathf.PI);
                pos.y += arcHeight * arcProgress;

                chip.transform.position = pos;

                // Rotate chip
                chip.transform.Rotate(0, 0, 360 * Time.deltaTime);

                yield return null;
            }

            chip.transform.position = endPos;
            onComplete?.Invoke();
        }

        private int CalculateChipCount(long amount)
        {
            if (amount <= 0) return 0;
            if (amount < 100) return 3;
            if (amount < 500) return 5;
            if (amount < 1000) return 7;
            if (amount < 5000) return 10;
            return 15;
        }

        public void SetPlayerChipPosition(int seatIndex, Transform position)
        {
            if (seatIndex >= 0 && seatIndex < playerChipPositions.Length)
            {
                playerChipPositions[seatIndex] = position;
            }
        }

        public void SetPotPosition(Transform position)
        {
            potPosition = position;
        }

        public void ShowChipStack(int seatIndex, long amount)
        {
            // TODO: Show static chip stack at player position
        }

        public void HideChipStack(int seatIndex)
        {
            // TODO: Hide chip stack at player position
        }

        public void ClearAllChips()
        {
            _chipPool?.ReturnAll();
        }

        private void OnDestroy()
        {
            _chipPool?.Clear();
        }
    }
}
