using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.UI
{
    public class RankingPanel : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button chipsRankTab;
        [SerializeField] private Button winRateTab;
        [SerializeField] private Button handsWonTab;

        [Header("Content")]
        [SerializeField] private Transform rankingContainer;
        [SerializeField] private GameObject rankingItemPrefab;
        [SerializeField] private ScrollRect scrollRect;

        [Header("My Ranking")]
        [SerializeField] private GameObject myRankingPanel;
        [SerializeField] private TMP_Text myRankText;
        [SerializeField] private TMP_Text myValueText;
        [SerializeField] private Image myAvatarImage;

        [Header("Top 3 Special")]
        [SerializeField] private GameObject top3Panel;
        [SerializeField] private RankingItemUI firstPlace;
        [SerializeField] private RankingItemUI secondPlace;
        [SerializeField] private RankingItemUI thirdPlace;

        [Header("Refresh")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private TMP_Text lastUpdateText;

        private List<RankingData> _rankings;
        private string _currentType = "chips";
        private string _myUserId;

        public event Action<string> OnRefresh;
        public event Action<string> OnViewProfile;

        private void Start()
        {
            SetupTabs();
            SetupButtons();
        }

        private void SetupTabs()
        {
            chipsRankTab?.onClick.AddListener(() => SwitchRankType("chips"));
            winRateTab?.onClick.AddListener(() => SwitchRankType("winRate"));
            handsWonTab?.onClick.AddListener(() => SwitchRankType("handsWon"));
        }

        private void SetupButtons()
        {
            refreshButton?.onClick.AddListener(() => OnRefresh?.Invoke(_currentType));
        }

        public void SetMyUserId(string userId)
        {
            _myUserId = userId;
        }

        private void SwitchRankType(string type)
        {
            _currentType = type;
            UpdateTabHighlight(type);
            OnRefresh?.Invoke(type);
        }

        private void UpdateTabHighlight(string type)
        {
            SetTabActive(chipsRankTab, type == "chips");
            SetTabActive(winRateTab, type == "winRate");
            SetTabActive(handsWonTab, type == "handsWon");
        }

        private void SetTabActive(Button tab, bool active)
        {
            if (tab == null) return;
            var colors = tab.colors;
            colors.normalColor = active ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            tab.colors = colors;
        }

        public void SetRankings(List<RankingData> rankings, string type)
        {
            _rankings = rankings;
            _currentType = type;
            RefreshDisplay();
            UpdateMyRanking();
            
            if (lastUpdateText != null)
            {
                lastUpdateText.text = $"更新时间: {DateTime.Now:HH:mm:ss}";
            }
        }

        private void RefreshDisplay()
        {
            // Clear existing items (except top 3)
            foreach (Transform child in rankingContainer)
            {
                Destroy(child.gameObject);
            }

            if (_rankings == null || _rankings.Count == 0) return;

            // Display top 3 in special panel
            if (top3Panel != null)
            {
                top3Panel.SetActive(_rankings.Count >= 3);
                
                if (_rankings.Count >= 1 && firstPlace != null)
                    firstPlace.SetData(_rankings[0], 1, _currentType);
                if (_rankings.Count >= 2 && secondPlace != null)
                    secondPlace.SetData(_rankings[1], 2, _currentType);
                if (_rankings.Count >= 3 && thirdPlace != null)
                    thirdPlace.SetData(_rankings[2], 3, _currentType);
            }

            // Display rest in scrollable list
            for (int i = 3; i < _rankings.Count; i++)
            {
                CreateRankingItem(_rankings[i], i + 1);
            }
        }

        private void CreateRankingItem(RankingData data, int rank)
        {
            var itemObj = Instantiate(rankingItemPrefab, rankingContainer);
            var rankingItem = itemObj.GetComponent<RankingItemUI>();
            
            if (rankingItem != null)
            {
                rankingItem.SetData(data, rank, _currentType);
                rankingItem.OnViewProfile += () => OnViewProfile?.Invoke(data.userId);
                
                // Highlight if this is me
                if (data.userId == _myUserId)
                {
                    rankingItem.SetHighlight(true);
                }
            }
        }

        private void UpdateMyRanking()
        {
            if (myRankingPanel == null || _rankings == null) return;

            int myRank = -1;
            RankingData myData = null;

            for (int i = 0; i < _rankings.Count; i++)
            {
                if (_rankings[i].userId == _myUserId)
                {
                    myRank = i + 1;
                    myData = _rankings[i];
                    break;
                }
            }

            if (myRank > 0 && myData != null)
            {
                myRankingPanel.SetActive(true);
                
                if (myRankText != null)
                {
                    myRankText.text = $"#{myRank}";
                }

                if (myValueText != null)
                {
                    myValueText.text = GetValueString(myData, _currentType);
                }
            }
            else
            {
                myRankingPanel.SetActive(false);
            }
        }

        private string GetValueString(RankingData data, string type)
        {
            return type switch
            {
                "chips" => data.chips.ToString("N0"),
                "winRate" => $"{data.winRate:P1}",
                "handsWon" => data.handsWon.ToString("N0"),
                _ => ""
            };
        }

        public void ScrollToMyRanking()
        {
            if (_rankings == null || scrollRect == null) return;

            int myIndex = _rankings.FindIndex(r => r.userId == _myUserId);
            if (myIndex >= 3) // If in scrollable area
            {
                float position = 1f - ((float)(myIndex - 3) / (_rankings.Count - 3));
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(position);
            }
        }
    }

    [Serializable]
    public class RankingData
    {
        public string userId;
        public string nickname;
        public string avatar;
        public int level;
        public long chips;
        public float winRate;
        public long handsWon;
        public long handsPlayed;
    }

    public class RankingItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button viewProfileButton;
        [SerializeField] private GameObject crownIcon;

        public event Action OnViewProfile;

        private void Start()
        {
            viewProfileButton?.onClick.AddListener(() => OnViewProfile?.Invoke());
        }

        public void SetData(RankingData data, int rank, string type)
        {
            if (rankText != null)
            {
                rankText.text = rank <= 3 ? "" : $"#{rank}";
            }

            if (crownIcon != null)
            {
                crownIcon.SetActive(rank <= 3);
            }

            if (nicknameText != null) nicknameText.text = data.nickname;
            if (levelText != null) levelText.text = $"Lv.{data.level}";

            if (valueText != null)
            {
                valueText.text = type switch
                {
                    "chips" => data.chips.ToString("N0"),
                    "winRate" => $"{data.winRate:P1}",
                    "handsWon" => data.handsWon.ToString("N0"),
                    _ => ""
                };
            }

            if (avatarImage != null)
            {
                var sprite = Resources.Load<Sprite>($"Avatars/{data.avatar}");
                if (sprite != null) avatarImage.sprite = sprite;
            }

            // Set rank-specific colors
            if (backgroundImage != null && rank <= 3)
            {
                backgroundImage.color = rank switch
                {
                    1 => new Color(1f, 0.84f, 0f, 0.3f),    // Gold
                    2 => new Color(0.75f, 0.75f, 0.75f, 0.3f), // Silver
                    3 => new Color(0.8f, 0.5f, 0.2f, 0.3f),  // Bronze
                    _ => Color.white
                };
            }
        }

        public void SetHighlight(bool highlight)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlight 
                    ? new Color(0.2f, 0.6f, 1f, 0.3f) 
                    : Color.white;
            }
        }
    }
}
