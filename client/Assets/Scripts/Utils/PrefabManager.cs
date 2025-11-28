using UnityEngine;

namespace TexasHoldem.Utils
{
    public class PrefabManager : MonoBehaviour
    {
        private static PrefabManager _instance;
        public static PrefabManager Instance => _instance;

        [Header("Game Prefabs")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private GameObject chipPrefab;
        [SerializeField] private GameObject playerHUDPrefab;
        [SerializeField] private GameObject dealerButtonPrefab;
        [SerializeField] private GameObject potDisplayPrefab;

        [Header("UI Prefabs")]
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private GameObject panelPrefab;
        [SerializeField] private GameObject sliderPrefab;
        [SerializeField] private GameObject inputFieldPrefab;
        [SerializeField] private GameObject chatMessagePrefab;
        [SerializeField] private GameObject notificationItemPrefab;
        [SerializeField] private GameObject friendItemPrefab;
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private GameObject taskItemPrefab;
        [SerializeField] private GameObject achievementItemPrefab;
        [SerializeField] private GameObject rankingItemPrefab;
        [SerializeField] private GameObject emojiButtonPrefab;
        [SerializeField] private GameObject quickChatButtonPrefab;

        [Header("Effect Prefabs")]
        [SerializeField] private GameObject chipParticlePrefab;
        [SerializeField] private GameObject winEffectPrefab;
        [SerializeField] private GameObject allInEffectPrefab;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Game prefab getters
        public GameObject CardPrefab => cardPrefab ?? LoadPrefab("Prefabs/Card");
        public GameObject ChipPrefab => chipPrefab ?? LoadPrefab("Prefabs/Chip");
        public GameObject PlayerHUDPrefab => playerHUDPrefab ?? LoadPrefab("Prefabs/PlayerHUD");
        public GameObject DealerButtonPrefab => dealerButtonPrefab ?? LoadPrefab("Prefabs/DealerButton");
        public GameObject PotDisplayPrefab => potDisplayPrefab ?? LoadPrefab("Prefabs/PotDisplay");

        // UI prefab getters
        public GameObject ButtonPrefab => buttonPrefab ?? LoadPrefab("Prefabs/UI/Button");
        public GameObject PanelPrefab => panelPrefab ?? LoadPrefab("Prefabs/UI/Panel");
        public GameObject SliderPrefab => sliderPrefab ?? LoadPrefab("Prefabs/UI/Slider");
        public GameObject InputFieldPrefab => inputFieldPrefab ?? LoadPrefab("Prefabs/UI/InputField");
        public GameObject ChatMessagePrefab => chatMessagePrefab ?? LoadPrefab("Prefabs/UI/ChatMessage");
        public GameObject NotificationItemPrefab => notificationItemPrefab ?? LoadPrefab("Prefabs/UI/NotificationItem");
        public GameObject FriendItemPrefab => friendItemPrefab ?? LoadPrefab("Prefabs/UI/FriendItem");
        public GameObject ShopItemPrefab => shopItemPrefab ?? LoadPrefab("Prefabs/UI/ShopItem");
        public GameObject TaskItemPrefab => taskItemPrefab ?? LoadPrefab("Prefabs/UI/TaskItem");
        public GameObject AchievementItemPrefab => achievementItemPrefab ?? LoadPrefab("Prefabs/UI/AchievementItem");
        public GameObject RankingItemPrefab => rankingItemPrefab ?? LoadPrefab("Prefabs/UI/RankingItem");
        public GameObject EmojiButtonPrefab => emojiButtonPrefab ?? LoadPrefab("Prefabs/UI/EmojiButton");
        public GameObject QuickChatButtonPrefab => quickChatButtonPrefab ?? LoadPrefab("Prefabs/UI/QuickChatButton");

        // Effect prefab getters
        public GameObject ChipParticlePrefab => chipParticlePrefab ?? LoadPrefab("Prefabs/Effects/ChipParticle");
        public GameObject WinEffectPrefab => winEffectPrefab ?? LoadPrefab("Prefabs/Effects/WinEffect");
        public GameObject AllInEffectPrefab => allInEffectPrefab ?? LoadPrefab("Prefabs/Effects/AllInEffect");

        private GameObject LoadPrefab(string path)
        {
            return Resources.Load<GameObject>(path);
        }

        public GameObject Instantiate(GameObject prefab, Transform parent = null)
        {
            if (prefab == null) return null;
            return parent != null 
                ? UnityEngine.Object.Instantiate(prefab, parent) 
                : UnityEngine.Object.Instantiate(prefab);
        }

        public T Instantiate<T>(GameObject prefab, Transform parent = null) where T : Component
        {
            var obj = Instantiate(prefab, parent);
            return obj?.GetComponent<T>();
        }
    }
}
