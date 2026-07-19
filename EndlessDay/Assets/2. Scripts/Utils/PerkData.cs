using System.Collections.Generic;

/// <summary>PerkTable + PerkStatTable + PerkSpecialTable을 PerkID 기준으로 조립한 것</summary>
public class PerkData
{
    public int PerkID;
    public string PerkName;
    public int Grade;         // 등급 (1=일반, 이후 희귀도 상승)
    public int WeaponType;    // 0=전체 공용, 그 외=특정 무기 전용
    public int MaxStack;
    public string Description;
    public string IconPath;

    public List<StatEffect> StatEffects = new List<StatEffect>();
    public SpecialEffect SpecialEffect;   // 없는 강화도 있음 (null 가능)
}

/// <summary>PerkStatTable 한 행 - 스탯 강화</summary>
public class StatEffect
{
    public int StatType;
    public int CalcType;
    public float Value;
}

/// <summary>PerkSpecialTable 한 행 - 특수효과 (조건 만족 시 발동)</summary>
public class SpecialEffect
{
    public int TriggerType;
    public int TriggerValue;
    public int DamagePercent;
    public float AreaRadius;
    public int EffectFlags;
}