using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.UI
{
    public class ChatPanel : MonoBehaviour
    {
        [Header("Chat Messages")]
        [SerializeField] private Transform messagesContainer;
        [SerializeField] private GameObject messagePrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private int maxMessages = 50;

        [Header("Input")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        [Header("Quick Chat")]
        [SerializeField] private Transform quickChatContainer;
        [SerializeField] private GameObject quickChatButtonPrefab;
        [SerializeField] private Button toggleQuickChatButton;
        [SerializeField] private GameObject quickChatPanel;

        [Header("Emoji")]
        [SerializeField] private Transform emojiContainer;
        [SerializeField] private GameObject emojiButtonPrefab;
        [SerializeField] private Button toggleEmojiButton;
        [SerializeField] private GameObject emojiPanel;

        [Header("Settings")]
        [SerializeField] private Button toggleChatButton;
        [SerializeField] private GameObject chatContent;

        private List<GameObject> _messageObjects = new List<GameObject>();
        private bool _isChatVisible = true;

        public event Action<string> OnSendMessage;
        public event Action<string> OnSendQuickChat;
        public event Action<string> OnSendEmoji;

        private void Start()
        {
            SetupButtons();
            SetupQuickChats();
            SetupEmojis();
            HidePanels();
        }

        private void SetupButtons()
        {
            sendButton?.onClick.AddListener(SendMessage);
            inputField?.onSubmit.AddListener(_ => SendMessage());
            
            toggleChatButton?.onClick.AddListener(ToggleChat);
            toggleQuickChatButton?.onClick.AddListener(ToggleQuickChat);
            toggleEmojiButton?.onClick.AddListener(ToggleEmoji);
        }

        private void SetupQuickChats()
        {
            if (quickChatContainer == null || quickChatButtonPrefab == null) return;

            var quickChats = new List<(string id, string text)>
            {
                ("hello", "大家好！"),
                ("gl", "祝你好运！"),
                ("gg", "打得好！"),
                ("nh", "好牌！"),
                ("ty", "谢谢！"),
                ("hurry", "快点啊！"),
                ("wp", "打得漂亮！"),
                ("bluff", "你在诈唬！"),
                ("sorry", "抱歉！"),
                ("bye", "再见！")
            };

            foreach (var (id, text) in quickChats)
            {
                var buttonObj = Instantiate(quickChatButtonPrefab, quickChatContainer);
                var button = buttonObj.GetComponent<Button>();
                var textComp = buttonObj.GetComponentInChildren<TMP_Text>();
                
                if (textComp != null) textComp.text = text;
                
                string chatId = id;
                button?.onClick.AddListener(() => {
                    OnSendQuickChat?.Invoke(chatId);
                    HidePanels();
                });
            }
        }

        private void SetupEmojis()
        {
            if (emojiContainer == null || emojiButtonPrefab == null) return;

            var emojis = new List<(string id, string display)>
            {
                ("smile", "😊"),
                ("laugh", "😂"),
                ("cry", "😢"),
                ("angry", "😠"),
                ("cool", "😎"),
                ("think", "🤔"),
                ("sweat", "😅"),
                ("shock", "😱"),
                ("chips", "🎰"),
                ("cards", "🃏"),
                ("allin", "💰"),
                ("fold", "🏳️")
            };

            foreach (var (id, display) in emojis)
            {
                var buttonObj = Instantiate(emojiButtonPrefab, emojiContainer);
                var button = buttonObj.GetComponent<Button>();
                var textComp = buttonObj.GetComponentInChildren<TMP_Text>();
                
                if (textComp != null) textComp.text = display;
                
                string emojiId = id;
                button?.onClick.AddListener(() => {
                    OnSendEmoji?.Invoke(emojiId);
                    HidePanels();
                });
            }
        }

        private void SendMessage()
        {
            if (inputField == null) return;
            
            string message = inputField.text?.Trim();
            if (string.IsNullOrEmpty(message)) return;

            OnSendMessage?.Invoke(message);
            inputField.text = "";
            inputField.ActivateInputField();
        }

        public void AddMessage(string senderName, string content, bool isSystem = false, bool isEmoji = false)
        {
            if (messagesContainer == null || messagePrefab == null) return;

            var messageObj = Instantiate(messagePrefab, messagesContainer);
            var messageUI = messageObj.GetComponent<ChatMessageUI>();
            
            if (messageUI != null)
            {
                messageUI.SetMessage(senderName, content, isSystem, isEmoji);
            }

            _messageObjects.Add(messageObj);

            // Trim old messages
            while (_messageObjects.Count > maxMessages)
            {
                var oldMessage = _messageObjects[0];
                _messageObjects.RemoveAt(0);
                Destroy(oldMessage);
            }

            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0;
            }
        }

        public void AddSystemMessage(string content)
        {
            AddMessage("系统", content, true);
        }

        private void ToggleChat()
        {
            _isChatVisible = !_isChatVisible;
            chatContent?.SetActive(_isChatVisible);
        }

        private void ToggleQuickChat()
        {
            bool isActive = quickChatPanel != null && quickChatPanel.activeSelf;
            HidePanels();
            quickChatPanel?.SetActive(!isActive);
        }

        private void ToggleEmoji()
        {
            bool isActive = emojiPanel != null && emojiPanel.activeSelf;
            HidePanels();
            emojiPanel?.SetActive(!isActive);
        }

        private void HidePanels()
        {
            quickChatPanel?.SetActive(false);
            emojiPanel?.SetActive(false);
        }

        public void ClearMessages()
        {
            foreach (var obj in _messageObjects)
            {
                Destroy(obj);
            }
            _messageObjects.Clear();
        }

        public void SetInputEnabled(bool enabled)
        {
            if (inputField != null) inputField.interactable = enabled;
            if (sendButton != null) sendButton.interactable = enabled;
        }
    }

    public class ChatMessageUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text senderText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image emojiImage;

        public void SetMessage(string sender, string content, bool isSystem, bool isEmoji)
        {
            if (senderText != null)
            {
                senderText.text = sender + ":";
                senderText.color = isSystem ? Color.yellow : Color.white;
            }

            if (isEmoji && emojiImage != null)
            {
                contentText?.gameObject.SetActive(false);
                emojiImage.gameObject.SetActive(true);
                
                var sprite = Resources.Load<Sprite>($"Emojis/{content}");
                if (sprite != null)
                {
                    emojiImage.sprite = sprite;
                }
            }
            else if (contentText != null)
            {
                contentText.text = content;
                emojiImage?.gameObject.SetActive(false);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isSystem 
                    ? new Color(0.3f, 0.3f, 0.1f, 0.8f) 
                    : new Color(0.1f, 0.1f, 0.1f, 0.8f);
            }
        }
    }
}
