/// <summary>
/// 데미지를 받을 수 있는 모든 대상(플레이어, 몬스터)이 구현하는 인터페이스.
/// 공격하는 쪽(무기 히트박스, 스킬 판정 등)은 상대가 플레이어인지 몬스터인지 몰라도 되고,
/// 이 인터페이스만 보고 TakeDamage를 호출하면 된다.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
    bool IsDead { get; }
}
