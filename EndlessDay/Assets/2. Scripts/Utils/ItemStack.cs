/// <summary>
/// 인벤토리 한 칸의 실제 상태. "이 칸에 어떤 아이템이 몇 개 있는가"만 담당.
/// 아이템 자체의 정의(이름/아이콘/효과)는 ItemData 쪽에 있고, 여기선 ItemID로만 참조한다
/// (참조 대신 ID로 들고 있는 이유: 저장/네트워크 전송에 유리하고, ItemData는 어차피 ItemManager가 항상 갖고 있음).
/// </summary>
public class ItemStack
{
    public const int EMPTY_ITEM_ID = 0;
    public const int MAX_STACK = 99;

    public int ItemID;
    public int Quantity;

    public bool IsEmpty => ItemID == EMPTY_ITEM_ID || Quantity <= 0;

    public static ItemStack Empty => new ItemStack { ItemID = EMPTY_ITEM_ID, Quantity = 0 };

    public ItemStack() { }

    public ItemStack(int itemID, int quantity)
    {
        ItemID = itemID;
        Quantity = quantity;
    }

    public void Clear()
    {
        ItemID = EMPTY_ITEM_ID;
        Quantity = 0;
    }
}