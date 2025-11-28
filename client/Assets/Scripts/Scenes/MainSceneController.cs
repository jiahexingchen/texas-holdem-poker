using UnityEngine;
using UnityEngine.SceneManagement;
using TexasHoldem.Game;
using TexasHoldem.UI;
using TexasHoldem.Audio;

namespace TexasHoldem.Scenes
{
    public class MainSceneController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Components")]
        [SerializeField] private LoginPanel loginComponent;
        [SerializeField] private LobbyPanel lobbyComponent;

        private AppManager _appManager;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            InitializeManagers();
            ShowLoginPanel();
            PlayBackgroundMusic();
        }

        private void InitializeManagers()
        {
            _appManager = AppManager.Instance;
            if (_appManager == null)
            {
                var go = new GameObject("AppManager");
                _appManager = go.AddComponent<AppManager>();
            }

            if (AudioManager.Instance == null)
            {
                var audioGo = new GameObject("AudioManager");
                audioGo.AddComponent<AudioManager>();
            }
        }

        private void PlayBackgroundMusic()
        {
            AudioManager.Instance?.PlayMenuMusic();
        }

        public void ShowLoginPanel()
        {
            HideAllPanels();
            loginPanel?.SetActive(true);
        }

        public void ShowLobbyPanel()
        {
            HideAllPanels();
            lobbyPanel?.SetActive(true);
        }

        public void ShowLoading(bool show)
        {
            loadingPanel?.SetActive(show);
        }

        private void HideAllPanels()
        {
            loginPanel?.SetActive(false);
            lobbyPanel?.SetActive(false);
            loadingPanel?.SetActive(false);
        }

        public void LoadGameScene()
        {
            ShowLoading(true);
            SceneManager.LoadSceneAsync("GameScene");
        }

        public void OnLoginSuccess()
        {
            ShowLobbyPanel();
        }

        public void OnStartGame()
        {
            LoadGameScene();
        }

        public void OnQuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
