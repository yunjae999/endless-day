using UnityEngine;

/// <summary>
/// 인벤토리 UI 전체를 관장. 슬롯(UIInventorySlot) 67개를 InventoryModel과 연결하고,
/// 슬롯에서 온 이동 요청을 실제 모델 변경으로 이어준 뒤 화면을 다시 그린다.
/// </summary>
public class UIInventoryController : MonoBehaviour
{
    [Header("슬롯 프리팹 / 배치 부모")]
    [SerializeField] UIInventorySlot _slotPrefab;
    [SerializeField] Transform _inventoryGridParent;   // 60칸이 배치될 부모(그리드 레이아웃)

    [Header("장착 슬롯 (씬에 이미 만들어둔 7개를 순서대로 직접 연결)")]
    [SerializeField] UIInventorySlot[] _equipSlots;    // 0헬멧 1갑옷 2신발 3~6장신구, Instantiate 안 함

    UIInventorySlot[] _inventorySlots;

    InventoryModel _model;   // GameSession 등에서 가져올 실제 데이터

    void Awake()
    {
        _model = GameSession._instance.Inventory;

        CreateSlots();
        RefreshAll();

        GameSession._instance.RegisterInventoryUI(this);
    }

    void OnDestroy()
    {
        if (GameSession._instance != null)
            GameSession._instance.UnregisterInventoryUI(this);
    }

    /// <summary>GameSession이 열기/닫기 요청 시 호출. 보일 때는 최신 상태로 새로고침도 같이 함</summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (visible)
            RefreshAll();
    }

    void CreateSlots()
    {
        _inventorySlots = new UIInventorySlot[InventoryModel.INVENTORY_SIZE];
        for (int i = 0; i < InventoryModel.INVENTORY_SIZE; i++)
        {
            UIInventorySlot slot = Instantiate(_slotPrefab, _inventoryGridParent);
            slot.Init(this, SlotType.Inventory, i);
            _inventorySlots[i] = slot;
        }

        // 장착 슬롯은 새로 만들지 않고, 씬에 이미 배치된 것을 그대로 초기화만 함
        for (int i = 0; i < _equipSlots.Length; i++)
            _equipSlots[i].Init(this, SlotType.Equip, i);
    }

    // ─────────────────────────────────────────────
    // 슬롯(UIInventorySlot)이 드롭 발생 시 호출
    // ─────────────────────────────────────────────

    public void RequestMove(UIInventorySlot from, UIInventorySlot to)
    {
        bool success = false;

        bool isEquipRelated = false;

        if (from.SlotType == SlotType.Inventory && to.SlotType == SlotType.Inventory)
        {
            success = _model.MoveInventorySlot(from.SlotIndex, to.SlotIndex);
        }
        else if (from.SlotType == SlotType.Inventory && to.SlotType == SlotType.Equip)
        {
            success = _model.Equip(from.SlotIndex, to.SlotIndex);
            isEquipRelated = true;
        }
        else if (from.SlotType == SlotType.Equip && to.SlotType == SlotType.Inventory)
        {
            success = _model.Unequip(from.SlotIndex, to.SlotIndex);
            isEquipRelated = true;
        }
        else if (from.SlotType == SlotType.Equip && to.SlotType == SlotType.Equip)
        {
            success = _model.SwapEquipSlots(from.SlotIndex, to.SlotIndex);
            isEquipRelated = true;
        }

        if (success)
        {
            RefreshSlot(from);
            RefreshSlot(to);

            if (isEquipRelated)
                GameSession._instance.PlayerStats?.Recalculate();
        }
    }

    // ─────────────────────────────────────────────
    // 화면 갱신
    // ─────────────────────────────────────────────

    void RefreshAll()
    {
        for (int i = 0; i < _inventorySlots.Length; i++)
            RefreshInventorySlot(i);

        for (int i = 0; i < _equipSlots.Length; i++)
            RefreshEquipSlot(i);
    }

    void RefreshSlot(UIInventorySlot slot)
    {
        if (slot.SlotType == SlotType.Inventory)
            RefreshInventorySlot(slot.SlotIndex);
        else
            RefreshEquipSlot(slot.SlotIndex);
    }

    void RefreshInventorySlot(int index)
    {
        ItemStack stack = _model.InventorySlots[index];
        UIInventorySlot slot = _inventorySlots[index];

        if (stack.IsEmpty)
        {
            slot.SetEmpty();
            return;
        }

        ItemData data = ItemManager._instance.Get(stack.ItemID);
        Sprite icon = Resources.Load<Sprite>(data.IconPath);   // 아이콘 로드 방식은 프로젝트 리소스 구조에 맞게 조정
        slot.SetContent(icon, stack.Quantity, data.ItemName, data.Description);
    }

    void RefreshEquipSlot(int index)
    {
        int itemID = _model.EquippedItemIDs[index];
        UIInventorySlot slot = _equipSlots[index];

        if (itemID == ItemStack.EMPTY_ITEM_ID)
        {
            slot.SetEmpty();
            return;
        }

        ItemData data = ItemManager._instance.Get(itemID);
        Sprite icon = Resources.Load<Sprite>(data.IconPath);
        slot.SetContent(icon, 1, data.ItemName, data.Description);
    }
}