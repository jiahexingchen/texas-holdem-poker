using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TexasHoldem.Game;
using TexasHoldem.UI;
using TexasHoldem.Audio;
using TexasHoldem.Core;
using TexasHoldem.AI;

namespace TexasHoldem.Scenes
{
    public class GameSceneController : MonoBehaviour
    {
        [Header("Game Table")]
        [SerializeField] private Transform tableCenter;
        [SerializeField] private Transform[] playerPositions;
        [SerializeField] private Transform communityCardsPosition;
        [SerializeField] private Transform potPosition;
        [SerializeField] private Transform deckPosition;

        [Header("UI References")]
        [SerializeField] private UIManager uiManager;
        [SerializeField] private ChatPanel chatPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject resultPanel;

        [Header("Prefabs")]
        [SerializeField] private GameObject playerHUDPrefab;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private GameObject chipPrefab;

        [Header("Game Settings")]
        [SerializeField] private bool isOfflineMode = true;
        [SerializeField] private int aiPlayerCount = 3;
        [SerializeField] private AIDifficulty aiDifficulty = AIDifficulty.Medium;
        [SerializeField] private long startingChips = 1000;

        private GameController _gameController;
        private GameManager _gameManager;

        private void Start()
        {
            InitializeGame();
            PlayGameMusic();
        }

        private void InitializeGame()
        {
            _gameController = GameController.Instance;
            if (_gameController == null)
            {
                var go = new GameObject("GameController");
                _gameController = go.AddComponent<GameController>();
            }

            if (uiManager != null)
            {
                _gameManager = _gameController.GameManager;
                uiManager.Initialize(_gameManager);
            }

            SetupPlayerPositions();

            if (isOfflineMode)
            {
                StartOfflineGame();
            }
        }

        private void SetupPlayerPositions()
        {
            // Ensure we have 9 player positions
            if (playerPositions == null || playerPositions.Length < 9)
            {
                Debug.LogWarning("Player positions not fully configured");
            }
        }

        private void StartOfflineGame()
        {
            string playerName = AppManager.Instance?.CurrentUser?.nickname ?? "Player";
            _gameController.StartOfflineGame(playerName, aiPlayerCount, aiDifficulty);
        }

        private void PlayGameMusic()
        {
            AudioManager.Instance?.PlayGameMusic();
        }

        public void OnPauseClicked()
        {
            Time.timeScale = 0;
            pausePanel?.SetActive(true);
        }

        public void OnResumeClicked()
        {
            Time.timeScale = 1;
            pausePanel?.SetActive(false);
        }

        public void OnLeaveGame()
        {
            Time.timeScale = 1;
            _gameController?.LeaveGame();
            SceneManager.LoadScene("MainScene");
        }

        public void OnSettingsClicked()
        {
            // Open settings panel
        }

        public void ShowResult(string winnerName, long amount, string handType)
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
                // Update result panel content
            }

            StartCoroutine(HideResultAfterDelay(3f));
        }

        private IEnumerator HideResultAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            resultPanel?.SetActive(false);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1;
        }
    }
}
