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
}
