using Defines;
using Newtonsoft.Json.Linq;

/// <summary>
/// 인벤토리(60칸)와 장착 슬롯(7칸)의 실제 상태와 이동/합치기/장착 규칙을 담당.
/// UI는 이 클래스의 메서드를 호출해 "옮겨줘"라고 요청만 하고, 실제 허용 여부/결과는 여기서 결정한다.
/// MonoBehaviour 아님 - GameSession 등 상위 계층이 인스턴스를 들고 있다가 씬 넘어서도 유지하는 형태로 사용.
/// </summary>
public class InventoryModel
{
    public const int INVENTORY_SIZE = 60;
    public const int EQUIP_SLOT_COUNT = 7;   // 0:헬멧 1:갑옷 2:신발 3~6:장신구

    public ItemStack[] InventorySlots = new ItemStack[INVENTORY_SIZE];
    public int[] EquippedItemIDs = new int[EQUIP_SLOT_COUNT];   // 0 = 빈 슬롯

    public InventoryModel()
    {
        for (int i = 0; i < INVENTORY_SIZE; i++)
            InventorySlots[i] = ItemStack.Empty;
    }

    // ─────────────────────────────────────────────
    // 아이템 획득 (던전에서 주울 때 등)
    // ─────────────────────────────────────────────

    /// <summary>기존 스택에 최대한 합치고, 남으면 빈 칸에 채운다. 다 못 담으면 남은 개수를 반환(0=전부 성공)</summary>
    public int AddItem(int itemID, int quantity)
    {
        ItemData data = ItemManager._instance.Get(itemID);
        bool stackable = data != null && data.Category == ItemCategory.Consumable;

        if (stackable)
        {
            for (int i = 0; i < INVENTORY_SIZE && quantity > 0; i++)
            {
                if (InventorySlots[i].ItemID != itemID)
                    continue;

                int space = ItemStack.MAX_STACK - InventorySlots[i].Quantity;
                if (space <= 0)
                    continue;

                int add = quantity < space ? quantity : space;
                InventorySlots[i].Quantity += add;
                quantity -= add;
            }
        }

        for (int i = 0; i < INVENTORY_SIZE && quantity > 0; i++)
        {
            if (!InventorySlots[i].IsEmpty)
                continue;

            int add = stackable ? (quantity < ItemStack.MAX_STACK ? quantity : ItemStack.MAX_STACK) : 1;
            InventorySlots[i] = new ItemStack(itemID, add);
            quantity -= add;
        }

        return quantity;   // 0이면 전부 담김, 남으면 인벤토리 꽉 찬 것
    }

    /// <summary>인벤토리 칸에서 아이템을 제거 (판매 시 사용). 장착 슬롯은 대상이 아님 - 장착 중인 건 판매 불가</summary>
    public bool RemoveItem(int itemID, int quantity)
    {
        // 실제로 빼기 전에 개수가 충분한지 먼저 확인 (중간에 실패해서 일부만 빠지는 걸 방지)
        int totalOwned = 0;
        for (int i = 0; i < INVENTORY_SIZE; i++)
        {
            if (InventorySlots[i].ItemID == itemID)
                totalOwned += InventorySlots[i].Quantity;
        }

        if (totalOwned < quantity)
            return false;

        int remaining = quantity;
        for (int i = 0; i < INVENTORY_SIZE && remaining > 0; i++)
        {
            if (InventorySlots[i].ItemID != itemID)
                continue;

            int take = remaining < InventorySlots[i].Quantity ? remaining : InventorySlots[i].Quantity;
            InventorySlots[i].Quantity -= take;
            remaining -= take;

            if (InventorySlots[i].Quantity <= 0)
                InventorySlots[i] = ItemStack.Empty;
        }

        return true;
    }

    // ─────────────────────────────────────────────
    // 인벤토리 내부 이동 (드래그 앤 드롭)
    // ─────────────────────────────────────────────

    /// <summary>인벤토리 칸끼리 이동. 같은 소비 아이템이면 합치고, 다르면 서로 자리를 바꾼다</summary>
    public bool MoveInventorySlot(int fromIndex, int toIndex)
    {
        if (!IsValidInventoryIndex(fromIndex) || !IsValidInventoryIndex(toIndex) || fromIndex == toIndex)
            return false;

        ItemStack from = InventorySlots[fromIndex];
        ItemStack to = InventorySlots[toIndex];

        if (from.IsEmpty)
            return false;

        // 목적지가 비어있으면 그냥 이동
        if (to.IsEmpty)
        {
            InventorySlots[toIndex] = from;
            InventorySlots[fromIndex] = ItemStack.Empty;
            return true;
        }

        // 같은 아이템(소비 아이템)이면 합치기 시도
        if (to.ItemID == from.ItemID)
        {
            ItemData data = ItemManager._instance.Get(from.ItemID);
            bool stackable = data != null && data.Category == ItemCategory.Consumable;

            if (stackable)
            {
                int space = ItemStack.MAX_STACK - to.Quantity;
                int move = from.Quantity < space ? from.Quantity : space;

                to.Quantity += move;
                from.Quantity -= move;

                if (from.Quantity <= 0)
                    InventorySlots[fromIndex] = ItemStack.Empty;

                return true;
            }
        }

        // 서로 다른 아이템이면 자리 교환
        InventorySlots[fromIndex] = to;
        InventorySlots[toIndex] = from;
        return true;
    }

    // ─────────────────────────────────────────────
    // 장착 / 해제
    // ─────────────────────────────────────────────

    /// <summary>인벤토리의 장비를 장착 슬롯으로. 기존에 장착 중이던 게 있으면 원래 칸으로 교환</summary>
    public bool Equip(int inventoryIndex, int equipSlotIndex)
    {
        if (!IsValidInventoryIndex(inventoryIndex) || !IsValidEquipIndex(equipSlotIndex))
            return false;

        ItemStack invStack = InventorySlots[inventoryIndex];
        if (invStack.IsEmpty)
            return false;

        ItemData data = ItemManager._instance.Get(invStack.ItemID);
        if (!(data is EquipmentData equipData))
            return false;

        if (!IsMatchingEquipSlot(equipSlotIndex, equipData.EquipmentType))
            return false;

        int previousItemID = EquippedItemIDs[equipSlotIndex];

        EquippedItemIDs[equipSlotIndex] = invStack.ItemID;

        // 원래 그 슬롯에 장착돼 있던 아이템은 방금 비워진 인벤토리 칸으로
        InventorySlots[inventoryIndex] = previousItemID == 0
            ? ItemStack.Empty
            : new ItemStack(previousItemID, 1);

        return true;
    }

    /// <summary>장착 슬롯의 아이템을 지정한 인벤토리 칸(비어있어야 함)으로 해제</summary>
    public bool Unequip(int equipSlotIndex, int inventoryIndex)
    {
        if (!IsValidEquipIndex(equipSlotIndex) || !IsValidInventoryIndex(inventoryIndex))
            return false;

        int itemID = EquippedItemIDs[equipSlotIndex];
        if (itemID == 0)
            return false;

        if (!InventorySlots[inventoryIndex].IsEmpty)
            return false;

        InventorySlots[inventoryIndex] = new ItemStack(itemID, 1);
        EquippedItemIDs[equipSlotIndex] = 0;
        return true;
    }

    /// <summary>장착 슬롯끼리 서로 교환. 각 아이템이 상대 슬롯 타입에도 맞아야 허용 (주로 장신구 4칸끼리)</summary>
    public bool SwapEquipSlots(int indexA, int indexB)
    {
        if (!IsValidEquipIndex(indexA) || !IsValidEquipIndex(indexB) || indexA == indexB)
            return false;

        int itemA = EquippedItemIDs[indexA];
        int itemB = EquippedItemIDs[indexB];

        if (itemA != 0 && !IsMatchingEquipSlot(indexB, GetEquipmentTypeOf(itemA)))
            return false;
        if (itemB != 0 && !IsMatchingEquipSlot(indexA, GetEquipmentTypeOf(itemB)))
            return false;

        EquippedItemIDs[indexA] = itemB;
        EquippedItemIDs[indexB] = itemA;
        return true;
    }

    EquipmentType GetEquipmentTypeOf(int itemID)
    {
        ItemData data = ItemManager._instance.Get(itemID);
        EquipmentData equipData = data as EquipmentData;
        return equipData != null ? equipData.EquipmentType : default;
    }

    // ─────────────────────────────────────────────
    // 검증 헬퍼
    // ─────────────────────────────────────────────

    bool IsValidInventoryIndex(int index) => index >= 0 && index < INVENTORY_SIZE;
    bool IsValidEquipIndex(int index) => index >= 0 && index < EQUIP_SLOT_COUNT;

    /// <summary>장착 슬롯 인덱스(0~6)와 장비 타입이 서로 맞는 조합인지 확인</summary>
    bool IsMatchingEquipSlot(int equipSlotIndex, EquipmentType type)
    {
        switch (equipSlotIndex)
        {
            case 0: return type == EquipmentType.Helmet;
            case 1: return type == EquipmentType.Armor;
            case 2: return type == EquipmentType.Shoes;
            default: return type == EquipmentType.Accessory;   // 3~6
        }
    }

    // ─────────────────────────────────────────────
    // 저장 / 로드 (서버 통신용 - 위치 정보 포함)
    // ─────────────────────────────────────────────

    /// <summary>인벤토리 창 닫을 때 서버로 보낼 JSON. [[슬롯,타입,아이템ID,개수], ...] 형태</summary>
    public string SerializeItemsToJson()
    {
        JArray array = new JArray();

        for (int i = 0; i < INVENTORY_SIZE; i++)
        {
            ItemStack stack = InventorySlots[i];
            if (stack.IsEmpty)
                continue;

            ItemData data = ItemManager._instance.Get(stack.ItemID);
            int itemType = (data != null && data.Category == ItemCategory.Equipment) ? 1 : 2;

            array.Add(new JArray(i, itemType, stack.ItemID, stack.Quantity));
        }

        return array.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>장착 슬롯 7칸을 서버로 보낼 JSON. [0,0,0,1002,0,0,0] 형태</summary>
    public string SerializeEquippedToJson()
    {
        JArray array = new JArray();
        for (int i = 0; i < EQUIP_SLOT_COUNT; i++)
            array.Add(EquippedItemIDs[i]);

        return array.ToString(Newtonsoft.Json.Formatting.None);
    }

    /// <summary>
    /// 로그인 시 서버가 보내준 저장된 위치 그대로 배치. AddItem()과 달리 합치기/빈칸탐색 없이
    /// 지정한 슬롯에 정확히 놓는다 - 유저가 정리해둔 배치를 그대로 복원하기 위함.
    /// </summary>
    public void SetSlotDirectly(int slotIndex, int itemId, int quantity)
    {
        if (!IsValidInventoryIndex(slotIndex))
            return;

        InventorySlots[slotIndex] = new ItemStack(itemId, quantity);
    }
}