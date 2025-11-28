using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.UI
{
    public class TaskPanel : MonoBehaviour
    {
        [Header("Daily Tasks")]
        [SerializeField] private Transform dailyTasksContainer;
        [SerializeField] private GameObject taskItemPrefab;

        [Header("Refresh Timer")]
        [SerializeField] private TMP_Text refreshTimerText;

        [Header("Summary")]
        [SerializeField] private TMP_Text completedCountText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Button claimAllButton;

        private List<TaskData> _tasks;
        private DateTime _nextRefresh;

        public event Action<string> OnClaimReward;
        public event Action OnClaimAll;

        private void Start()
        {
            SetupButtons();
            CalculateNextRefresh();
        }

        private void Update()
        {
            UpdateRefreshTimer();
        }

        private void SetupButtons()
        {
            claimAllButton?.onClick.AddListener(() => OnClaimAll?.Invoke());
        }

        private void CalculateNextRefresh()
        {
            var now = DateTime.Now;
            _nextRefresh = now.Date.AddDays(1);
        }

        private void UpdateRefreshTimer()
        {
            if (refreshTimerText == null) return;

            var remaining = _nextRefresh - DateTime.Now;
            if (remaining.TotalSeconds <= 0)
            {
                refreshTimerText.text = "刷新中...";
                // Request new tasks from server
                return;
            }

            refreshTimerText.text = $"刷新: {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        public void SetTasks(List<TaskData> tasks)
        {
            _tasks = tasks;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            // Clear existing items
            foreach (Transform child in dailyTasksContainer)
            {
                Destroy(child.gameObject);
            }

            int completed = 0;
            int total = _tasks?.Count ?? 0;

            if (_tasks != null)
            {
                foreach (var task in _tasks)
                {
                    CreateTaskItem(task);
                    if (task.completed) completed++;
                }
            }

            // Update summary
            if (completedCountText != null)
            {
                completedCountText.text = $"{completed}/{total}";
            }

            if (progressSlider != null)
            {
                progressSlider.value = total > 0 ? (float)completed / total : 0;
            }

            // Enable claim all button if there are unclaimed completed tasks
            if (claimAllButton != null)
            {
                bool hasUnclaimedCompleted = _tasks?.Exists(t => t.completed && !t.claimed) ?? false;
                claimAllButton.interactable = hasUnclaimedCompleted;
            }
        }

        private void CreateTaskItem(TaskData task)
        {
            var itemObj = Instantiate(taskItemPrefab, dailyTasksContainer);
            var taskItem = itemObj.GetComponent<TaskItemUI>();
            
            if (taskItem != null)
            {
                taskItem.SetData(task);
                taskItem.OnClaim += () => OnClaimReward?.Invoke(task.id);
            }
        }

        public void UpdateTaskProgress(string taskId, int progress)
        {
            var task = _tasks?.Find(t => t.id == taskId);
            if (task != null)
            {
                task.progress = progress;
                if (progress >= task.target)
                {
                    task.completed = true;
                }
                RefreshDisplay();
            }
        }

        public void OnRewardClaimed(string taskId, long reward)
        {
            var task = _tasks?.Find(t => t.id == taskId);
            if (task != null)
            {
                task.claimed = true;
                RefreshDisplay();
            }
        }
    }

    [Serializable]
    public class TaskData
    {
        public string id;
        public string name;
        public string description;
        public string taskType;
        public int target;
        public int progress;
        public long reward;
        public string rewardType;
        public bool completed;
        public bool claimed;
    }

    public class TaskItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject completedIcon;
        [SerializeField] private GameObject claimedOverlay;

        private TaskData _data;
        public event Action OnClaim;

        private void Start()
        {
            claimButton?.onClick.AddListener(() => OnClaim?.Invoke());
        }

        public void SetData(TaskData data)
        {
            _data = data;

            if (nameText != null) nameText.text = data.name;
            if (descriptionText != null) descriptionText.text = data.description;
            
            if (progressText != null)
            {
                progressText.text = $"{data.progress}/{data.target}";
            }

            if (progressSlider != null)
            {
                progressSlider.value = data.target > 0 ? (float)data.progress / data.target : 0;
            }

            if (rewardText != null)
            {
                string icon = data.rewardType == "diamonds" ? "💎" : "🪙";
                rewardText.text = $"{icon}{data.reward}";
            }

            if (completedIcon != null)
            {
                completedIcon.SetActive(data.completed);
            }

            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(data.completed && !data.claimed);
            }

            if (claimedOverlay != null)
            {
                claimedOverlay.SetActive(data.claimed);
            }
        }
    }
}
