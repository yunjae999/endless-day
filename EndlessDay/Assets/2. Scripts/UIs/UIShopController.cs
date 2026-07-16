using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 상점 UI 전체를 관장. 왼쪽(구매 목록)은 ItemManager 전체 카탈로그, 오른쪽(보유 아이템)은
/// GameSession.Inventory에서 채운다. 슬롯 클릭으로 선택하고, 하단 버튼으로 구매/판매를 요청한다.
/// </summary>
public class UIShopController : MonoBehaviour
{
    [Header("슬롯 프리팹 / 배치 부모")]
    [SerializeField] UIShopSlot _slotPrefab;
    [SerializeField] Transform _buyGridParent;
    [SerializeField] Transform _sellGridParent;

    [Header("버튼")]
    [SerializeField] Button _buyButton;
    [SerializeField] Button _sellButton;

    [Header("임시 - NPC 상호작용 존 만들기 전까지, E키로 바로 열고 닫기")]
    [SerializeField] GameObject _shopPanelRoot;   // 이 오브젝트를 켜고 끔 (보통 이 스크립트가 붙은 오브젝트 자신)

    List<UIShopSlot> _buySlots = new List<UIShopSlot>();
    List<UIShopSlot> _sellSlots = new List<UIShopSlot>();

    UIShopSlot _selectedBuySlot;
    UIShopSlot _selectedSellSlot;

    void Awake()
    {
        _buyButton.onClick.AddListener(OnClickBuy);
        _sellButton.onClick.AddListener(OnClickSell);

        NetworkManager._instance.OnBuyResult += OnBuyResult;
        NetworkManager._instance.OnSellResult += OnSellResult;

        CreateBuySlots();
        RefreshSellSlots();

        if (_shopPanelRoot != null)
            _shopPanelRoot.SetActive(false);   // 시작할 땐 닫혀있게
    }

    void Update()
    {
        // TODO: NPC 상호작용 존 만들면 이 부분을 "범위 안 + E" 조건으로 교체
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ToggleShopPanel();
    }

    public void ToggleShopPanel()
    {
        if (_shopPanelRoot == null)
            return;

        bool isOpen = _shopPanelRoot.activeSelf;
        _shopPanelRoot.SetActive(!isOpen);

        if (!isOpen)
            RefreshSellSlots();   // 열릴 때마다 보유 목록 최신화 (그 사이 던전 등에서 아이템이 바뀌었을 수 있음)
    }

    void OnDestroy()
    {
        if (NetworkManager._instance != null)
        {
            NetworkManager._instance.OnBuyResult -= OnBuyResult;
            NetworkManager._instance.OnSellResult -= OnSellResult;
        }
    }

    // ─────────────────────────────────────────────
    // 목록 생성
    // ─────────────────────────────────────────────

    /// <summary>구매 목록은 카탈로그(ItemManager 전체)라서 한 번만 생성하면 됨</summary>
    void CreateBuySlots()
    {
        foreach (ItemData data in ItemManager._instance.GetAll())
        {
            UIShopSlot slot = Instantiate(_slotPrefab, _buyGridParent);
            slot.Init(this, ShopSlotContext.Buy);

            Sprite icon = Resources.Load<Sprite>(data.IconPath);
            slot.SetContent(data.ItemID, data.ItemName, data.Description, data.Price, icon);

            _buySlots.Add(slot);
        }
    }

    /// <summary>보유 목록은 거래할 때마다 바뀌므로, 매번 지우고 다시 생성</summary>
    void RefreshSellSlots()
    {
        foreach (UIShopSlot slot in _sellSlots)
            Destroy(slot.gameObject);
        _sellSlots.Clear();
        _selectedSellSlot = null;

        InventoryModel inventory = GameSession._instance.Inventory;
        for (int i = 0; i < InventoryModel.INVENTORY_SIZE; i++)
        {
            ItemStack stack = inventory.InventorySlots[i];
            if (stack.IsEmpty)
                continue;

            ItemData data = ItemManager._instance.Get(stack.ItemID);
            if (data == null)
                continue;

            UIShopSlot slot = Instantiate(_slotPrefab, _sellGridParent);
            slot.Init(this, ShopSlotContext.Sell);

            Sprite icon = Resources.Load<Sprite>(data.IconPath);
            int sellPrice = data.Price / 2;   // 판매가 = 구매가의 50%, 서버 계산과 동일하게 표시만 함
            slot.SetContent(data.ItemID, data.ItemName, data.Description, sellPrice, icon, stack.Quantity);

            _sellSlots.Add(slot);
        }
    }

    // ─────────────────────────────────────────────
    // 슬롯(UIShopSlot)이 클릭 시 호출
    // ─────────────────────────────────────────────

    public void OnSlotClicked(UIShopSlot slot)
    {
        if (slot.Context == ShopSlotContext.Buy)
        {
            if (_selectedBuySlot != null)
                _selectedBuySlot.SetSelected(false);
            _selectedBuySlot = slot;
        }
        else
        {
            if (_selectedSellSlot != null)
                _selectedSellSlot.SetSelected(false);
            _selectedSellSlot = slot;
        }

        slot.SetSelected(true);
    }

    // ─────────────────────────────────────────────
    // 버튼
    // ─────────────────────────────────────────────

    void OnClickBuy()
    {
        if (_selectedBuySlot == null)
            return;
        NetworkManager._instance.SendBuyItem(_selectedBuySlot.ItemId);
    }

    void OnClickSell()
    {
        if (_selectedSellSlot == null)
            return;
        NetworkManager._instance.SendSellItem(_selectedSellSlot.ItemId);
    }

    // ─────────────────────────────────────────────
    // 서버 응답
    // ─────────────────────────────────────────────

    void OnBuyResult(bool success, int itemId, int newGold)
    {
        GameSession._instance.ApplyBuyResult(success, itemId, newGold);

        if (success)
            RefreshSellSlots();   // 구매 성공 시 보유 목록에 새로 추가되므로 갱신
    }

    void OnSellResult(bool success, int itemId, int newGold)
    {
        GameSession._instance.ApplySellResult(success, itemId, newGold);

        if (success)
            RefreshSellSlots();
    }
}