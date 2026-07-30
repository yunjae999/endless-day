using UnityEngine;

/// <summary>
/// 데미지를 받을 수 있는 모든 대상(플레이어, 몬스터)이 구현하는 인터페이스.
/// 공격하는 쪽(무기 히트박스, 스킬 판정 등)은 상대가 플레이어인지 몬스터인지 몰라도 되고,
/// 이 인터페이스만 보고 TakeDamage를 호출하면 된다.
/// </summary>
public interface IDamageable
{
    /// <summary>방어력 적용 후 실제로 깎인 데미지를 반환 (팝업에 정확한 숫자를 보여주기 위함)</summary>
    int TakeDamage(int amount);
    bool IsDead { get; }
    int CurrentHP { get; }
    int MaxHP { get; }

    /// <summary>데미지 팝업이 떠야 할 위치. 대상마다 크기가 달라서(보스 등) 고정 오프셋 하나로는 안 맞으니, 각자 자기 위치를 알려줌</summary>
    Vector3 DamagePopupPosition { get; }
}