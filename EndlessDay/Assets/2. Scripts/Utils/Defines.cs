using UnityEngine;

namespace Defines
{
    public enum TableName
    {
        PerkTable,
        PerkStatTable,
        PerkSpecialTable,
        WeaponAttackTable,
        WeaponModifierTable,
        MonsterTable,
        MonsterAttackTable,
        ProjectileAttackTable,
        SummonTable,
        DungeonTable,
        FloorTable,
        DungeonRoomTable,
        RoomSpawnTable,
        EquipmentTable,
        ConsumableTable,
        PlayerBaseStatTable,

        max
    }
    public enum PlayerActionState
    {
        IDLE,
        MOVE,
        ROLL,
        ATTACK,
        SKILL,
        HIT,

        DEATH = 50
    }

    public enum MonsterActionState
    {
        IDLE,
        PATROL,
        CHASE,
        ATTACK,
        ATTACK_IDLE,
        HIT,

        DEATH = 50
    }

    public enum ItemCategory
    {
        Equipment,
        Consumable,
    }

    public enum EquipmentType
    {
        Helmet = 1,
        Armor = 2,
        Shoes = 3,
        Accessory = 4,
    }

    public enum StatType
    {
        HP = 1,
        AttackPower = 2,
        Defense = 3,
        MoveSpeed = 4,
        AttackSpeed = 5,
        CritChance = 6,
        CritDamage = 7,
    }

    public enum CalcType
    {
        Percent = 1,          // HP/공격력/방어력/이속/공속 - 곱연산
        PercentagePoint = 2,  // 치명타확률/피해 - 그냥 더함
    }

    public enum EffectType
    {
        Heal = 1,
        // 추후 확장: Buff, CureDebuff 등
    }
}
