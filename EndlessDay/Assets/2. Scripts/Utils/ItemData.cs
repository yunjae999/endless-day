using Defines;

/// <summary>
/// 아이템 하나의 "정의" 정보 (테이블에서 그대로 오는 고정 데이터).
/// 플레이어가 몇 개 가지고 있는지 같은 가변 정보는 여기 없음 (그건 ItemStack의 역할).
/// </summary>
public abstract class ItemData
{
    public int ItemID;
    public string ItemName;
    public string IconPath;
    public string Description;
    public int Price;   // 구매가. 판매가는 ShopManager에서 이 값의 비율로 계산

    public abstract ItemCategory Category { get; }
}

/// <summary>EquipmentTable 한 행에 대응</summary>
public class EquipmentData : ItemData
{
    public override ItemCategory Category => ItemCategory.Equipment;

    public EquipmentType EquipmentType;
    public StatType StatType;
    public CalcType CalcType;
    public float Value;
}

/// <summary>ConsumableTable 한 행에 대응</summary>
public class ConsumableData : ItemData
{
    public override ItemCategory Category => ItemCategory.Consumable;

    public EffectType EffectType;
    public int EffectValue;
}