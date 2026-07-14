using Defines;
using UnityEngine;

/// <summary>
/// 드래그 앤 드롭 테스트 전용 부트스트랩.
/// ItemManager에 가짜 아이템을 등록하고, GameSession의 인벤토리에 몇 개 채워 넣는다.
/// 실제 로그인/서버와 완전히 무관 - 테스트 끝나면 이 컴포넌트를 씬에서 빼면 된다.
/// </summary>
public class InventoryTestSetup : MonoBehaviour
{
    void Awake()
    {
        RegisterTestItems();
        FillTestInventory();
    }

    void RegisterTestItems()
    {
        ItemManager._instance.Register(new ConsumableData
        {
            ItemID = 9001,
            ItemName = "테스트 포션",
            Description = "테스트용 아이템",
            IconPath = "Icons/TestIcon",   // 실제 프로젝트에 존재하는 스프라이트 경로로 바꿔주세요
            EffectType = EffectType.Heal,
            EffectValue = 30,
        });

        ItemManager._instance.Register(new EquipmentData
        {
            ItemID = 9002,
            ItemName = "테스트 투구",
            Description = "테스트용 아이템",
            IconPath = "Icons/TestIcon",
            EquipmentType = EquipmentType.Helmet,
            StatType = StatType.Defense,
            CalcType = CalcType.Percent,
            Value = 10,
        });

        ItemManager._instance.Register(new EquipmentData
        {
            ItemID = 9003,
            ItemName = "테스트 반지",
            Description = "테스트용 아이템",
            IconPath = "Icons/TestIcon",
            EquipmentType = EquipmentType.Accessory,
            StatType = StatType.AttackPower,
            CalcType = CalcType.Percent,
            Value = 5,
        });
    }

    void FillTestInventory()
    {
        InventoryModel inventory = GameSession._instance.Inventory;
        inventory.AddItem(9001, 5);   // 포션 5개 (같은 칸에 드래그해서 합쳐지는지 확인용)
        inventory.AddItem(9002, 1);   // 투구 (헬멧 슬롯에 장착되는지 확인용)
        inventory.AddItem(9003, 1);   // 반지 (장신구 슬롯에 장착되는지 확인용)
    }
}
