using Defines;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기본 스탯(PlayerBaseStatTable)에 강화(Perk)와 장비 보정값을 합산해서 최종 스탯을 계산.
/// 강화를 새로 얻거나 장비를 갈아입을 때마다 Recalculate()를 호출해서 "처음부터 다시" 합산한다
/// (보정값을 하나씩 누적하는 방식이 아니라 항상 재계산 - 장비 해제 시 반영 안 되는 버그를 방지).
/// </summary>
public class PlayerStatManager : MonoBehaviour
{
    int _baseMaxHP;
    int _baseAttackPower;
    int _baseDefense;
    int _baseMoveSpeed;
    int _baseRunSpeed;
    int _baseAttackSpeed;
    int _baseCritChance;
    int _baseCritDamage;

    // StatType별 "지금까지 다 더한" 보정값. CalcType(Percent vs PercentagePoint)에 따라 따로 관리
    Dictionary<StatType, float> _percentModifiers = new Dictionary<StatType, float>();
    Dictionary<StatType, float> _percentagePointModifiers = new Dictionary<StatType, float>();

    [Header("디버그 - 인스펙터 확인용, Recalculate() 때마다 갱신됨")]
    [SerializeField] float _debugFinalMaxHP;
    [SerializeField] float _debugFinalAttackPower;
    [SerializeField] float _debugFinalDefense;
    [SerializeField] float _debugFinalMoveSpeed;
    [SerializeField] float _debugFinalAttackSpeed;

    public int BaseMoveSpeed { get { return _baseMoveSpeed; } }
    public int BaseRunSpeed { get { return _baseRunSpeed; } }

    // ─────────────────────────────────────────────
    // 최종 스탯 - 다른 스크립트는 전부 이것만 읽으면 됨
    // ─────────────────────────────────────────────

    public float FinalMaxHP => _baseMaxHP * (1 + GetPercent(StatType.HP) / 100f);
    public float FinalAttackPower => _baseAttackPower * (1 + GetPercent(StatType.AttackPower) / 100f);
    public float FinalDefense => _baseDefense * (1 + GetPercent(StatType.Defense) / 100f);
    public float FinalMoveSpeed => _baseMoveSpeed * (1 + GetPercent(StatType.MoveSpeed) / 100f);
    public float FinalRunSpeed => _baseRunSpeed * (1 + GetPercent(StatType.MoveSpeed) / 100f);
    public float FinalAttackSpeed => _baseAttackSpeed * (1 + GetPercent(StatType.AttackSpeed) / 100f);
    public float FinalCritChance => _baseCritChance + GetPercentagePoint(StatType.CritChance);
    public float FinalCritDamage => _baseCritDamage + GetPercentagePoint(StatType.CritDamage);

    float GetPercent(StatType type)
    {
        return _percentModifiers.ContainsKey(type) ? _percentModifiers[type] : 0f;
    }

    float GetPercentagePoint(StatType type)
    {
        return _percentagePointModifiers.ContainsKey(type) ? _percentagePointModifiers[type] : 0f;
    }

    public void InitBaseStats()
    {
        DataTable baseStatTable = TableDataManager._instance.Get(TableName.PlayerBaseStatTable);

        _baseMaxHP = baseStatTable.ToI(1, "MaxHP");
        _baseAttackPower = baseStatTable.ToI(1, "AttackPower");
        _baseDefense = baseStatTable.ToI(1, "Defense");
        _baseMoveSpeed = baseStatTable.ToI(1, "MoveSpeed");
        _baseRunSpeed = baseStatTable.ToI(1, "RunSpeed");
        _baseAttackSpeed = baseStatTable.ToI(1, "AttackSpeed");
        _baseCritChance = baseStatTable.ToI(1, "CritChance");
        _baseCritDamage = baseStatTable.ToI(1, "CritDamage");

        GameSession._instance.RegisterPlayerStats(this);
        Recalculate();   // 시작 시 1회 - 강화/장비 없어도 기본값으로 세팅됨
    }

    void OnDestroy()
    {
        if (GameSession._instance != null)
            GameSession._instance.UnregisterPlayerStats(this);
    }

    /// <summary>강화를 새로 얻거나 장비를 갈아입을 때마다 호출 - GameSession의 최신 상태를 전부 다시 합산</summary>
    public void Recalculate()
    {
        _percentModifiers.Clear();
        _percentagePointModifiers.Clear();

        // 강화(Perk) 반영 - 스택 수만큼 곱해서 더함
        foreach (KeyValuePair<int, int> pair in GameSession._instance.ActivePerks)
        {
            PerkData perk = PerkManager._instance.Get(pair.Key);
            if (perk == null)
                continue;

            foreach (StatEffect effect in perk.StatEffects)
                AddModifier((StatType)effect.StatType, (CalcType)effect.CalcType, effect.Value * pair.Value);
        }

        // 장비(7슬롯) 반영
        foreach (int itemId in GameSession._instance.Inventory.EquippedItemIDs)
        {
            if (itemId == 0)
                continue;

            ItemData data = ItemManager._instance.Get(itemId);
            if (!(data is EquipmentData equip))
                continue;

            AddModifier(equip.StatType, equip.CalcType, equip.Value);
        }

        // TODO: 무기 보정 (무기 선택 시스템 완성되면 추가)

        _debugFinalMaxHP = FinalMaxHP;
        _debugFinalAttackPower = FinalAttackPower;
        _debugFinalDefense = FinalDefense;
        _debugFinalMoveSpeed = FinalMoveSpeed;
        _debugFinalAttackSpeed = FinalAttackSpeed;
    }

    void AddModifier(StatType statType, CalcType calcType, float value)
    {
        Dictionary<StatType, float> target = calcType == CalcType.Percent ? _percentModifiers : _percentagePointModifiers;

        if (!target.ContainsKey(statType))
            target[statType] = 0f;
        target[statType] += value;
    }
}