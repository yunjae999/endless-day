using Defines;
using System.Collections.Generic;

/// <summary>
/// EquipmentTable / ConsumableTable을 읽어서 ItemData로 조립해두는 매니저.
/// 게임 시작 시 1회 LoadAll() 호출, 이후엔 Get(itemID)로만 조회.
/// </summary>
public class ItemManager : TSingleton<ItemManager>
{
    Dictionary<int, ItemData> _items = new Dictionary<int, ItemData>();

    public void LoadAll()
    {
        _items = new Dictionary<int, ItemData>();
        LoadEquipmentTable();
        LoadConsumableTable();
    }

    void LoadEquipmentTable()
    {
        DataTable table = TableDataManager._instance.Get(TableName.EquipmentTable);

        foreach (int itemID in table.GetAllKeys())
        {
            EquipmentData data = new EquipmentData
            {
                ItemID = itemID,
                ItemName = table.ToS(itemID, "EquipmentName"),
                Description = table.ToS(itemID, "Description"),
                IconPath = table.ToS(itemID, "IconPath"),
                EquipmentType = (EquipmentType)table.ToI(itemID, "EquipmentType"),
                StatType = (StatType)table.ToI(itemID, "StatType"),
                CalcType = (CalcType)table.ToI(itemID, "CalcType"),
                Value = table.ToF(itemID, "Value"),
            };
            _items.Add(itemID, data);
        }
    }

    void LoadConsumableTable()
    {
        DataTable table = TableDataManager._instance.Get(TableName.ConsumableTable);

        foreach (int itemID in table.GetAllKeys())
        {
            ConsumableData data = new ConsumableData
            {
                ItemID = itemID,
                ItemName = table.ToS(itemID, "ItemName"),
                Description = table.ToS(itemID, "Description"),
                IconPath = table.ToS(itemID, "IconPath"),
                EffectType = (EffectType)table.ToI(itemID, "EffectType"),
                EffectValue = table.ToI(itemID, "EffectValue"),
            };
            _items.Add(itemID, data);
        }
    }

    public ItemData Get(int itemID)
    {
        return _items.ContainsKey(itemID) ? _items[itemID] : null;
    }

    /// <summary>테이블 로드 없이 아이템을 직접 등록. 테스트/디버그용.</summary>
    public void Register(ItemData data)
    {
        _items[data.ItemID] = data;
    }
}