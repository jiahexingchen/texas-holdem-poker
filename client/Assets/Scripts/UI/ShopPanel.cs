using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TexasHoldem.UI
{
    public class ShopPanel : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button avatarsTab;
        [SerializeField] private Button cardBacksTab;
        [SerializeField] private Button tablesTab;
        [SerializeField] private Button chipsTab;
        [SerializeField] private Button vipTab;

        [Header("Content")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject shopItemPrefab;

        [Header("User Info")]
        [SerializeField] private TMP_Text chipsText;
        [SerializeField] private TMP_Text diamondsText;

        [Header("Item Detail")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailDescription;
        [SerializeField] private TMP_Text detailPrice;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button closeDetailButton;

        [Header("Confirm Purchase")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        private List<ShopItemData> _allItems;
        private ShopItemData _selectedItem;
        private string _currentCategory = "all";

        public event Action<string> OnPurchase;
        public event Action<string> OnEquip;

        private void Start()
        {
            SetupTabs();
            SetupButtons();
            LoadItems();
            ShowCategory("avatars");
        }

        private void SetupTabs()
        {
            avatarsTab?.onClick.AddListener(() => ShowCategory("avatars"));
            cardBacksTab?.onClick.AddListener(() => ShowCategory("card_backs"));
            tablesTab?.onClick.AddListener(() => ShowCategory("tables"));
            chipsTab?.onClick.AddListener(() => ShowCategory("chips"));
            vipTab?.onClick.AddListener(() => ShowCategory("vip"));
        }

        private void SetupButtons()
        {
            purchaseButton?.onClick.AddListener(OnPurchaseClicked);
            closeDetailButton?.onClick.AddListener(() => detailPanel?.SetActive(false));
            confirmYesButton?.onClick.AddListener(ConfirmPurchase);
            confirmNoButton?.onClick.AddListener(() => confirmPanel?.SetActive(false));
        }

        private void LoadItems()
        {
            // In real implementation, load from server
            _allItems = new List<ShopItemData>
            {
                // Avatars
                new ShopItemData { id = "avatar_classic", name = "经典头像", description = "经典扑克玩家头像", category = "avatars", price = 500, currency = "chips", rarity = "common" },
                new ShopItemData { id = "avatar_vip", name = "VIP头像", description = "尊贵VIP专属头像", category = "avatars", price = 100, currency = "diamonds", rarity = "epic" },
                
                // Card backs
                new ShopItemData { id = "card_blue", name = "深邃蓝色", description = "深邃蓝色卡背", category = "card_backs", price = 1000, currency = "chips", rarity = "common" },
                new ShopItemData { id = "card_gold", name = "奢华金色", description = "奢华金色卡背", category = "card_backs", price = 5000, currency = "chips", rarity = "rare" },
                new ShopItemData { id = "card_dragon", name = "龙纹卡背", description = "神秘龙纹卡背", category = "card_backs", price = 500, currency = "diamonds", rarity = "legendary" },
                
                // Tables
                new ShopItemData { id = "table_blue", name = "深蓝牌桌", description = "深蓝色豪华牌桌", category = "tables", price = 3000, currency = "chips", rarity = "rare" },
                new ShopItemData { id = "table_red", name = "皇家红色", description = "皇家红色牌桌", category = "tables", price = 5000, currency = "chips", rarity = "rare" },
                
                // Chips
                new ShopItemData { id = "chips_small", name = "小额筹码", description = "获得10,000筹码", category = "chips", price = 10, currency = "diamonds", rarity = "common" },
                new ShopItemData { id = "chips_medium", name = "中额筹码", description = "获得50,000筹码", category = "chips", price = 45, currency = "diamonds", rarity = "common" },
                new ShopItemData { id = "chips_large", name = "大额筹码", description = "获得120,000筹码", category = "chips", price = 100, currency = "diamonds", rarity = "common" },
            };
        }

        private void ShowCategory(string category)
        {
            _currentCategory = category;
            RefreshItemsDisplay();
            UpdateTabHighlight(category);
        }

        private void UpdateTabHighlight(string category)
        {
            // Reset all tabs
            SetTabActive(avatarsTab, category == "avatars");
            SetTabActive(cardBacksTab, category == "card_backs");
            SetTabActive(tablesTab, category == "tables");
            SetTabActive(chipsTab, category == "chips");
            SetTabActive(vipTab, category == "vip");
        }

        private void SetTabActive(Button tab, bool active)
        {
            if (tab == null) return;
            var colors = tab.colors;
            colors.normalColor = active ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            tab.colors = colors;
        }

        private void RefreshItemsDisplay()
        {
            // Clear existing items
            foreach (Transform child in itemsContainer)
            {
                Destroy(child.gameObject);
            }

            // Filter and display items
            var filteredItems = _allItems.FindAll(item => item.category == _currentCategory);
            
            foreach (var item in filteredItems)
            {
                CreateItemDisplay(item);
            }
        }

        private void CreateItemDisplay(ShopItemData item)
        {
            var itemObj = Instantiate(shopItemPrefab, itemsContainer);
            var shopItem = itemObj.GetComponent<ShopItemUI>();
            
            if (shopItem != null)
            {
                shopItem.SetData(item);
                shopItem.OnClicked += () => ShowItemDetail(item);
            }
        }

        private void ShowItemDetail(ShopItemData item)
        {
            _selectedItem = item;
            
            if (detailName != null) detailName.text = item.name;
            if (detailDescription != null) detailDescription.text = item.description;
            if (detailPrice != null) 
            {
                string currencyIcon = item.currency == "diamonds" ? "💎" : "🪙";
                detailPrice.text = $"{currencyIcon} {item.price:N0}";
            }
            
            // Load icon
            if (detailIcon != null)
            {
                var sprite = Resources.Load<Sprite>($"Shop/{item.id}");
                if (sprite != null) detailIcon.sprite = sprite;
            }

            if (purchaseButton != null)
            {
                var buttonText = purchaseButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = item.owned ? "已拥有" : "购买";
                }
                purchaseButton.interactable = !item.owned;
            }

            detailPanel?.SetActive(true);
        }

        private void OnPurchaseClicked()
        {
            if (_selectedItem == null) return;
            
            if (confirmText != null)
            {
                string currencyName = _selectedItem.currency == "diamonds" ? "钻石" : "筹码";
                confirmText.text = $"确定要花费 {_selectedItem.price} {currencyName} 购买 {_selectedItem.name} 吗？";
            }
            
            confirmPanel?.SetActive(true);
        }

        private void ConfirmPurchase()
        {
            if (_selectedItem == null) return;
            
            OnPurchase?.Invoke(_selectedItem.id);
            confirmPanel?.SetActive(false);
            detailPanel?.SetActive(false);
        }

        public void UpdateBalance(long chips, long diamonds)
        {
            if (chipsText != null) chipsText.text = chips.ToString("N0");
            if (diamondsText != null) diamondsText.text = diamonds.ToString("N0");
        }

        public void OnPurchaseSuccess(string itemId)
        {
            var item = _allItems.Find(i => i.id == itemId);
            if (item != null)
            {
                item.owned = true;
            }
            RefreshItemsDisplay();
        }

        public void OnPurchaseFailed(string error)
        {
            Debug.LogError($"Purchase failed: {error}");
            // Show error message to user
        }
    }

    [Serializable]
    public class ShopItemData
    {
        public string id;
        public string name;
        public string description;
        public string category;
        public long price;
        public string currency;
        public string rarity;
        public bool owned;
        public int discount;
    }

    public class ShopItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private GameObject ownedBadge;
        [SerializeField] private GameObject discountBadge;
        [SerializeField] private TMP_Text discountText;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Button button;

        private ShopItemData _data;
        public event Action OnClicked;

        private void Start()
        {
            button?.onClick.AddListener(() => OnClicked?.Invoke());
        }

        public void SetData(ShopItemData data)
        {
            _data = data;

            if (nameText != null) nameText.text = data.name;
            
            if (priceText != null)
            {
                string icon = data.currency == "diamonds" ? "💎" : "🪙";
                priceText.text = $"{icon}{data.price:N0}";
            }

            if (ownedBadge != null) ownedBadge.SetActive(data.owned);

            if (discountBadge != null && discountText != null)
            {
                bool hasDiscount = data.discount > 0;
                discountBadge.SetActive(hasDiscount);
                if (hasDiscount)
                {
                    discountText.text = $"-{data.discount}%";
                }
            }

            if (rarityBorder != null)
            {
                rarityBorder.color = GetRarityColor(data.rarity);
            }

            if (iconImage != null)
            {
                var sprite = Resources.Load<Sprite>($"Shop/{data.id}");
                if (sprite != null) iconImage.sprite = sprite;
            }
        }

        private Color GetRarityColor(string rarity)
        {
            return rarity switch
            {
                "common" => Color.gray,
                "rare" => Color.blue,
                "epic" => new Color(0.5f, 0, 0.5f),
                "legendary" => new Color(1f, 0.8f, 0),
                _ => Color.white
            };
        }
    }
}
