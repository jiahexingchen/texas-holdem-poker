using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.UI
{
    public class FriendsPanel : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button friendsTab;
        [SerializeField] private Button requestsTab;
        [SerializeField] private Button blockedTab;
        [SerializeField] private Button searchTab;

        [Header("Badge")]
        [SerializeField] private GameObject requestsBadge;
        [SerializeField] private TMP_Text requestsBadgeText;

        [Header("Content")]
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject friendItemPrefab;
        [SerializeField] private GameObject requestItemPrefab;

        [Header("Search")]
        [SerializeField] private GameObject searchPanel;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button searchButton;
        [SerializeField] private Transform searchResultsContainer;

        [Header("Empty State")]
        [SerializeField] private GameObject emptyStatePanel;
        [SerializeField] private TMP_Text emptyStateText;

        private List<FriendData> _friends = new List<FriendData>();
        private List<FriendRequestData> _requests = new List<FriendRequestData>();
        private List<FriendData> _blocked = new List<FriendData>();
        private string _currentTab = "friends";

        public event Action<string> OnViewProfile;
        public event Action<string> OnInviteToGame;
        public event Action<string> OnRemoveFriend;
        public event Action<string> OnAcceptRequest;
        public event Action<string> OnRejectRequest;
        public event Action<string> OnUnblock;
        public event Action<string> OnSearch;
        public event Action<string> OnSendRequest;

        private void Start()
        {
            SetupTabs();
            SetupSearch();
            ShowTab("friends");
        }

        private void SetupTabs()
        {
            friendsTab?.onClick.AddListener(() => ShowTab("friends"));
            requestsTab?.onClick.AddListener(() => ShowTab("requests"));
            blockedTab?.onClick.AddListener(() => ShowTab("blocked"));
            searchTab?.onClick.AddListener(() => ShowTab("search"));
        }

        private void SetupSearch()
        {
            searchButton?.onClick.AddListener(DoSearch);
            searchInput?.onSubmit.AddListener(_ => DoSearch());
        }

        private void ShowTab(string tab)
        {
            _currentTab = tab;
            searchPanel?.SetActive(tab == "search");
            RefreshDisplay();
            UpdateTabHighlight();
        }

        private void UpdateTabHighlight()
        {
            SetTabActive(friendsTab, _currentTab == "friends");
            SetTabActive(requestsTab, _currentTab == "requests");
            SetTabActive(blockedTab, _currentTab == "blocked");
            SetTabActive(searchTab, _currentTab == "search");
        }

        private void SetTabActive(Button tab, bool active)
        {
            if (tab == null) return;
            var colors = tab.colors;
            colors.normalColor = active ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            tab.colors = colors;
        }

        public void SetFriends(List<FriendData> friends)
        {
            _friends = friends;
            if (_currentTab == "friends") RefreshDisplay();
        }

        public void SetRequests(List<FriendRequestData> requests)
        {
            _requests = requests;
            UpdateRequestsBadge();
            if (_currentTab == "requests") RefreshDisplay();
        }

        public void SetBlocked(List<FriendData> blocked)
        {
            _blocked = blocked;
            if (_currentTab == "blocked") RefreshDisplay();
        }

        private void UpdateRequestsBadge()
        {
            int count = _requests.Count;
            requestsBadge?.SetActive(count > 0);
            if (requestsBadgeText != null)
            {
                requestsBadgeText.text = count > 99 ? "99+" : count.ToString();
            }
        }

        private void RefreshDisplay()
        {
            // Clear existing items
            foreach (Transform child in listContainer)
            {
                Destroy(child.gameObject);
            }

            switch (_currentTab)
            {
                case "friends":
                    DisplayFriends();
                    break;
                case "requests":
                    DisplayRequests();
                    break;
                case "blocked":
                    DisplayBlocked();
                    break;
                case "search":
                    // Search results are displayed separately
                    break;
            }
        }

        private void DisplayFriends()
        {
            if (_friends.Count == 0)
            {
                ShowEmptyState("暂无好友\n点击搜索添加好友");
                return;
            }

            HideEmptyState();

            foreach (var friend in _friends)
            {
                var itemObj = Instantiate(friendItemPrefab, listContainer);
                var friendItem = itemObj.GetComponent<FriendItemUI>();
                
                if (friendItem != null)
                {
                    friendItem.SetData(friend);
                    friendItem.OnViewProfile += () => OnViewProfile?.Invoke(friend.userId);
                    friendItem.OnInvite += () => OnInviteToGame?.Invoke(friend.userId);
                    friendItem.OnRemove += () => {
                        OnRemoveFriend?.Invoke(friend.userId);
                        _friends.Remove(friend);
                        RefreshDisplay();
                    };
                }
            }
        }

        private void DisplayRequests()
        {
            if (_requests.Count == 0)
            {
                ShowEmptyState("暂无好友请求");
                return;
            }

            HideEmptyState();

            foreach (var request in _requests)
            {
                var itemObj = Instantiate(requestItemPrefab, listContainer);
                var requestItem = itemObj.GetComponent<FriendRequestItemUI>();
                
                if (requestItem != null)
                {
                    requestItem.SetData(request);
                    requestItem.OnAccept += () => {
                        OnAcceptRequest?.Invoke(request.userId);
                        _requests.Remove(request);
                        RefreshDisplay();
                        UpdateRequestsBadge();
                    };
                    requestItem.OnReject += () => {
                        OnRejectRequest?.Invoke(request.userId);
                        _requests.Remove(request);
                        RefreshDisplay();
                        UpdateRequestsBadge();
                    };
                }
            }
        }

        private void DisplayBlocked()
        {
            if (_blocked.Count == 0)
            {
                ShowEmptyState("暂无黑名单用户");
                return;
            }

            HideEmptyState();

            foreach (var blocked in _blocked)
            {
                var itemObj = Instantiate(friendItemPrefab, listContainer);
                var friendItem = itemObj.GetComponent<FriendItemUI>();
                
                if (friendItem != null)
                {
                    friendItem.SetDataBlocked(blocked);
                    friendItem.OnUnblock += () => {
                        OnUnblock?.Invoke(blocked.userId);
                        _blocked.Remove(blocked);
                        RefreshDisplay();
                    };
                }
            }
        }

        private void DoSearch()
        {
            string query = searchInput?.text?.Trim();
            if (string.IsNullOrEmpty(query)) return;
            OnSearch?.Invoke(query);
        }

        public void DisplaySearchResults(List<FriendData> results)
        {
            foreach (Transform child in searchResultsContainer)
            {
                Destroy(child.gameObject);
            }

            if (results.Count == 0)
            {
                ShowEmptyState("未找到用户");
                return;
            }

            HideEmptyState();

            foreach (var user in results)
            {
                var itemObj = Instantiate(friendItemPrefab, searchResultsContainer);
                var friendItem = itemObj.GetComponent<FriendItemUI>();
                
                if (friendItem != null)
                {
                    friendItem.SetDataSearchResult(user);
                    friendItem.OnAddFriend += () => OnSendRequest?.Invoke(user.userId);
                }
            }
        }

        private void ShowEmptyState(string message)
        {
            emptyStatePanel?.SetActive(true);
            if (emptyStateText != null) emptyStateText.text = message;
        }

        private void HideEmptyState()
        {
            emptyStatePanel?.SetActive(false);
        }
    }

    [Serializable]
    public class FriendData
    {
        public string userId;
        public string nickname;
        public string avatar;
        public int level;
        public bool isOnline;
        public string status; // "playing", "lobby", "offline"
        public string roomId;
    }

    [Serializable]
    public class FriendRequestData
    {
        public string userId;
        public string nickname;
        public string avatar;
        public int level;
        public long timestamp;
    }

    public class FriendItemUI : MonoBehaviour
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject onlineIndicator;
        [SerializeField] private Button viewProfileButton;
        [SerializeField] private Button inviteButton;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button unblockButton;
        [SerializeField] private Button addFriendButton;

        public event Action OnViewProfile;
        public event Action OnInvite;
        public event Action OnRemove;
        public event Action OnUnblock;
        public event Action OnAddFriend;

        private void Start()
        {
            viewProfileButton?.onClick.AddListener(() => OnViewProfile?.Invoke());
            inviteButton?.onClick.AddListener(() => OnInvite?.Invoke());
            removeButton?.onClick.AddListener(() => OnRemove?.Invoke());
            unblockButton?.onClick.AddListener(() => OnUnblock?.Invoke());
            addFriendButton?.onClick.AddListener(() => OnAddFriend?.Invoke());
        }

        public void SetData(FriendData data)
        {
            if (nicknameText != null) nicknameText.text = data.nickname;
            if (levelText != null) levelText.text = $"Lv.{data.level}";
            if (onlineIndicator != null) onlineIndicator.SetActive(data.isOnline);
            
            if (statusText != null)
            {
                statusText.text = data.status switch
                {
                    "playing" => "游戏中",
                    "lobby" => "在线",
                    _ => "离线"
                };
            }

            inviteButton?.gameObject.SetActive(data.isOnline);
            removeButton?.gameObject.SetActive(true);
            unblockButton?.gameObject.SetActive(false);
            addFriendButton?.gameObject.SetActive(false);
        }

        public void SetDataBlocked(FriendData data)
        {
            if (nicknameText != null) nicknameText.text = data.nickname;
            if (levelText != null) levelText.text = $"Lv.{data.level}";
            if (onlineIndicator != null) onlineIndicator.SetActive(false);
            if (statusText != null) statusText.text = "已屏蔽";

            inviteButton?.gameObject.SetActive(false);
            removeButton?.gameObject.SetActive(false);
            unblockButton?.gameObject.SetActive(true);
            addFriendButton?.gameObject.SetActive(false);
        }

        public void SetDataSearchResult(FriendData data)
        {
            if (nicknameText != null) nicknameText.text = data.nickname;
            if (levelText != null) levelText.text = $"Lv.{data.level}";
            if (onlineIndicator != null) onlineIndicator.SetActive(data.isOnline);
            if (statusText != null) statusText.text = "";

            inviteButton?.gameObject.SetActive(false);
            removeButton?.gameObject.SetActive(false);
            unblockButton?.gameObject.SetActive(false);
            addFriendButton?.gameObject.SetActive(true);
        }
    }

    public class FriendRequestItemUI : MonoBehaviour
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button rejectButton;

        public event Action OnAccept;
        public event Action OnReject;

        private void Start()
        {
            acceptButton?.onClick.AddListener(() => OnAccept?.Invoke());
            rejectButton?.onClick.AddListener(() => OnReject?.Invoke());
        }

        public void SetData(FriendRequestData data)
        {
            if (nicknameText != null) nicknameText.text = data.nickname;
            if (levelText != null) levelText.text = $"Lv.{data.level}";
            
            if (timeText != null)
            {
                var time = DateTimeOffset.FromUnixTimeMilliseconds(data.timestamp).LocalDateTime;
                var diff = DateTime.Now - time;
                
                if (diff.TotalMinutes < 60)
                    timeText.text = $"{(int)diff.TotalMinutes}分钟前";
                else if (diff.TotalHours < 24)
                    timeText.text = $"{(int)diff.TotalHours}小时前";
                else
                    timeText.text = $"{(int)diff.TotalDays}天前";
            }
        }
    }
}
