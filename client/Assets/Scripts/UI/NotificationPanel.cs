using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.UI
{
    public class NotificationPanel : MonoBehaviour
    {
        [Header("Badge")]
        [SerializeField] private GameObject notificationBadge;
        [SerializeField] private TMP_Text badgeCountText;

        [Header("Panel")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private GameObject notificationItemPrefab;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button markAllReadButton;
        [SerializeField] private Button clearAllButton;

        [Header("Toast")]
        [SerializeField] private GameObject toastPrefab;
        [SerializeField] private Transform toastContainer;
        [SerializeField] private float toastDuration = 3f;

        private List<NotificationData> _notifications = new List<NotificationData>();
        private Queue<GameObject> _toastQueue = new Queue<GameObject>();

        public event Action<string> OnNotificationClicked;
        public event Action<string> OnNotificationDismissed;
        public event Action OnMarkAllRead;
        public event Action OnClearAll;

        private void Start()
        {
            SetupButtons();
            HidePanel();
            UpdateBadge();
        }

        private void SetupButtons()
        {
            toggleButton?.onClick.AddListener(TogglePanel);
            markAllReadButton?.onClick.AddListener(() => {
                MarkAllAsRead();
                OnMarkAllRead?.Invoke();
            });
            clearAllButton?.onClick.AddListener(() => {
                ClearAll();
                OnClearAll?.Invoke();
            });
        }

        public void AddNotification(NotificationData notification)
        {
            _notifications.Insert(0, notification);
            RefreshDisplay();
            UpdateBadge();

            // Show toast for new notification
            ShowToast(notification);
        }

        public void SetNotifications(List<NotificationData> notifications)
        {
            _notifications = notifications;
            RefreshDisplay();
            UpdateBadge();
        }

        private void RefreshDisplay()
        {
            // Clear existing items
            foreach (Transform child in notificationContainer)
            {
                Destroy(child.gameObject);
            }

            // Create new items
            foreach (var notification in _notifications)
            {
                CreateNotificationItem(notification);
            }
        }

        private void CreateNotificationItem(NotificationData notification)
        {
            var itemObj = Instantiate(notificationItemPrefab, notificationContainer);
            var notifItem = itemObj.GetComponent<NotificationItemUI>();
            
            if (notifItem != null)
            {
                notifItem.SetData(notification);
                notifItem.OnClicked += () => {
                    MarkAsRead(notification.id);
                    OnNotificationClicked?.Invoke(notification.id);
                };
                notifItem.OnDismissed += () => {
                    RemoveNotification(notification.id);
                    OnNotificationDismissed?.Invoke(notification.id);
                };
            }
        }

        private void UpdateBadge()
        {
            int unreadCount = _notifications.FindAll(n => !n.read).Count;
            
            if (notificationBadge != null)
            {
                notificationBadge.SetActive(unreadCount > 0);
            }

            if (badgeCountText != null)
            {
                badgeCountText.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
            }
        }

        public void MarkAsRead(string notificationId)
        {
            var notification = _notifications.Find(n => n.id == notificationId);
            if (notification != null)
            {
                notification.read = true;
                RefreshDisplay();
                UpdateBadge();
            }
        }

        public void MarkAllAsRead()
        {
            foreach (var notification in _notifications)
            {
                notification.read = true;
            }
            RefreshDisplay();
            UpdateBadge();
        }

        public void RemoveNotification(string notificationId)
        {
            _notifications.RemoveAll(n => n.id == notificationId);
            RefreshDisplay();
            UpdateBadge();
        }

        public void ClearAll()
        {
            _notifications.Clear();
            RefreshDisplay();
            UpdateBadge();
        }

        private void TogglePanel()
        {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(!notificationPanel.activeSelf);
            }
        }

        private void HidePanel()
        {
            notificationPanel?.SetActive(false);
        }

        public void ShowToast(NotificationData notification)
        {
            if (toastPrefab == null || toastContainer == null) return;

            var toastObj = Instantiate(toastPrefab, toastContainer);
            var toastUI = toastObj.GetComponent<ToastUI>();
            
            if (toastUI != null)
            {
                toastUI.SetData(notification.title, notification.content, notification.type);
            }

            _toastQueue.Enqueue(toastObj);
            Destroy(toastObj, toastDuration);
        }

        public void ShowToast(string title, string content, string type = "info")
        {
            ShowToast(new NotificationData
            {
                title = title,
                content = content,
                type = type
            });
        }
    }

    [Serializable]
    public class NotificationData
    {
        public string id;
        public string type;
        public string title;
        public string content;
        public bool read;
        public long timestamp;
        public Dictionary<string, object> data;
    }

    public class NotificationItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject unreadIndicator;
        [SerializeField] private Button itemButton;
        [SerializeField] private Button dismissButton;

        public event Action OnClicked;
        public event Action OnDismissed;

        private void Start()
        {
            itemButton?.onClick.AddListener(() => OnClicked?.Invoke());
            dismissButton?.onClick.AddListener(() => OnDismissed?.Invoke());
        }

        public void SetData(NotificationData data)
        {
            if (titleText != null) titleText.text = data.title;
            if (contentText != null) contentText.text = data.content;
            
            if (timeText != null)
            {
                var time = DateTimeOffset.FromUnixTimeMilliseconds(data.timestamp).LocalDateTime;
                timeText.text = FormatTime(time);
            }

            if (unreadIndicator != null)
            {
                unreadIndicator.SetActive(!data.read);
            }

            if (iconImage != null)
            {
                var sprite = Resources.Load<Sprite>($"Icons/notification_{data.type}");
                if (sprite != null) iconImage.sprite = sprite;
            }
        }

        private string FormatTime(DateTime time)
        {
            var diff = DateTime.Now - time;
            
            if (diff.TotalMinutes < 1) return "刚刚";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}分钟前";
            if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}小时前";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}天前";
            
            return time.ToString("MM-dd");
        }
    }

    public class ToastUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;

        public void SetData(string title, string content, string type)
        {
            if (titleText != null) titleText.text = title;
            if (contentText != null) contentText.text = content;

            if (backgroundImage != null)
            {
                backgroundImage.color = type switch
                {
                    "success" => new Color(0.2f, 0.6f, 0.2f, 0.9f),
                    "error" => new Color(0.6f, 0.2f, 0.2f, 0.9f),
                    "warning" => new Color(0.6f, 0.5f, 0.1f, 0.9f),
                    _ => new Color(0.2f, 0.3f, 0.5f, 0.9f)
                };
            }
        }
    }
}
