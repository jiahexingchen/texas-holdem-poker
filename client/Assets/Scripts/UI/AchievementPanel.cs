using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.UI
{
    public class AchievementPanel : MonoBehaviour
    {
        [Header("Category Tabs")]
        [SerializeField] private Button allTab;
        [SerializeField] private Button beginnerTab;
        [SerializeField] private Button handTab;
        [SerializeField] private Button wealthTab;
        [SerializeField] private Button socialTab;

        [Header("Content")]
        [SerializeField] private Transform achievementsContainer;
        [SerializeField] private GameObject achievementItemPrefab;

        [Header("Summary")]
        [SerializeField] private TMP_Text unlockedCountText;
        [SerializeField] private TMP_Text totalCountText;
        [SerializeField] private Slider overallProgressSlider;
        [SerializeField] private TMP_Text totalRewardsText;

        [Header("Detail")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailDescription;
        [SerializeField] private TMP_Text detailReward;
        [SerializeField] private Slider detailProgress;
        [SerializeField] private TMP_Text detailProgressText;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;

        private List<AchievementData> _achievements;
        private AchievementData _selectedAchievement;
        private string _currentCategory = "all";

        public event Action<string> OnClaimReward;

        private void Start()
        {
            SetupTabs();
            SetupButtons();
        }

        private void SetupTabs()
        {
            allTab?.onClick.AddListener(() => ShowCategory("all"));
            beginnerTab?.onClick.AddListener(() => ShowCategory("beginner"));
            handTab?.onClick.AddListener(() => ShowCategory("hand"));
            wealthTab?.onClick.AddListener(() => ShowCategory("wealth"));
            socialTab?.onClick.AddListener(() => ShowCategory("social"));
        }

        private void SetupButtons()
        {
            claimButton?.onClick.AddListener(ClaimReward);
            closeButton?.onClick.AddListener(() => detailPanel?.SetActive(false));
        }

        public void SetAchievements(List<AchievementData> achievements)
        {
            _achievements = achievements;
            RefreshDisplay();
            UpdateSummary();
        }

        private void ShowCategory(string category)
        {
            _currentCategory = category;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            // Clear existing items
            foreach (Transform child in achievementsContainer)
            {
                Destroy(child.gameObject);
            }

            if (_achievements == null) return;

            var filtered = _currentCategory == "all" 
                ? _achievements 
                : _achievements.FindAll(a => a.category == _currentCategory);

            // Sort: unlocked first, then by progress
            filtered.Sort((a, b) => 
            {
                if (a.unlocked != b.unlocked) return b.unlocked.CompareTo(a.unlocked);
                return b.progress.CompareTo(a.progress);
            });

            foreach (var achievement in filtered)
            {
                CreateAchievementItem(achievement);
            }
        }

        private void CreateAchievementItem(AchievementData achievement)
        {
            var itemObj = Instantiate(achievementItemPrefab, achievementsContainer);
            var achievementItem = itemObj.GetComponent<AchievementItemUI>();
            
            if (achievementItem != null)
            {
                achievementItem.SetData(achievement);
                achievementItem.OnClicked += () => ShowDetail(achievement);
            }
        }

        private void UpdateSummary()
        {
            if (_achievements == null) return;

            int unlocked = _achievements.FindAll(a => a.unlocked).Count;
            int total = _achievements.Count;
            long totalRewards = 0;

            foreach (var a in _achievements)
            {
                if (a.unlocked && a.claimed)
                {
                    totalRewards += a.reward;
                }
            }

            if (unlockedCountText != null) unlockedCountText.text = unlocked.ToString();
            if (totalCountText != null) totalCountText.text = total.ToString();
            
            if (overallProgressSlider != null)
            {
                overallProgressSlider.value = total > 0 ? (float)unlocked / total : 0;
            }

            if (totalRewardsText != null)
            {
                totalRewardsText.text = $"已获得奖励: {totalRewards:N0}";
            }
        }

        private void ShowDetail(AchievementData achievement)
        {
            _selectedAchievement = achievement;

            if (detailName != null) detailName.text = achievement.name;
            if (detailDescription != null) detailDescription.text = achievement.description;
            if (detailReward != null) detailReward.text = $"奖励: {achievement.reward:N0} 筹码";

            if (detailProgress != null)
            {
                detailProgress.value = achievement.progress;
            }

            if (detailProgressText != null)
            {
                if (achievement.unlocked)
                {
                    detailProgressText.text = "已完成";
                }
                else
                {
                    detailProgressText.text = $"进度: {achievement.progress:P0}";
                }
            }

            if (detailIcon != null)
            {
                var sprite = Resources.Load<Sprite>($"Achievements/{achievement.icon}");
                if (sprite != null) detailIcon.sprite = sprite;
                
                // Gray out if not unlocked
                detailIcon.color = achievement.unlocked ? Color.white : Color.gray;
            }

            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(achievement.unlocked && !achievement.claimed);
            }

            detailPanel?.SetActive(true);
        }

        private void ClaimReward()
        {
            if (_selectedAchievement == null || _selectedAchievement.claimed) return;
            OnClaimReward?.Invoke(_selectedAchievement.id);
        }

        public void OnRewardClaimed(string achievementId, long reward)
        {
            var achievement = _achievements?.Find(a => a.id == achievementId);
            if (achievement != null)
            {
                achievement.claimed = true;
                RefreshDisplay();
                UpdateSummary();
                
                if (_selectedAchievement?.id == achievementId)
                {
                    ShowDetail(achievement);
                }
            }
        }

        public void OnAchievementUnlocked(string achievementId)
        {
            var achievement = _achievements?.Find(a => a.id == achievementId);
            if (achievement != null)
            {
                achievement.unlocked = true;
                achievement.progress = 1f;
                RefreshDisplay();
                UpdateSummary();
                
                // Show notification
                Debug.Log($"Achievement unlocked: {achievement.name}");
            }
        }
    }

    [Serializable]
    public class AchievementData
    {
        public string id;
        public string name;
        public string description;
        public string category;
        public string icon;
        public long reward;
        public float progress;
        public bool unlocked;
        public bool claimed;
        public bool hidden;
    }

    public class AchievementItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private GameObject unlockedBadge;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private Button button;

        private AchievementData _data;
        public event Action OnClicked;

        private void Start()
        {
            button?.onClick.AddListener(() => OnClicked?.Invoke());
        }

        public void SetData(AchievementData data)
        {
            _data = data;

            if (nameText != null) 
            {
                nameText.text = data.hidden && !data.unlocked ? "???" : data.name;
            }

            if (rewardText != null)
            {
                rewardText.text = $"🪙{data.reward}";
            }

            if (progressSlider != null)
            {
                progressSlider.value = data.progress;
                progressSlider.gameObject.SetActive(!data.unlocked);
            }

            if (unlockedBadge != null)
            {
                unlockedBadge.SetActive(data.unlocked);
            }

            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!data.unlocked);
            }

            if (iconImage != null)
            {
                var sprite = Resources.Load<Sprite>($"Achievements/{data.icon}");
                if (sprite != null) iconImage.sprite = sprite;
                iconImage.color = data.unlocked ? Color.white : Color.gray;
            }
        }
    }
}
