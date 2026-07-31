using UnityEngine;

/// <summary>
/// Animator가 부착된 자식 오브젝트(보스 모델)에 붙이는 중계 스크립트.
/// Animation Event는 Animator가 붙은 오브젝트에게만 전달되므로,
/// 실제 로직이 있는 부모의 BossController로 그대로 전달만 한다.
/// 일반 몬스터 공용 이벤트(OnAttackAnimationEnd 등) + 보스 전용 이벤트(히트박스, 투사체 발사, 소환) 전부 포함.
/// </summary>
public class BossAnimationEventRelay : MonoBehaviour
{
    BossController _controller;

    void Awake()
    {
        _controller = GetComponentInParent<BossController>();
    }

    // ── 공용 (부모 MonsterController에서 상속) ──

    public void OnAttackAnimationEnd()
    {
        _controller.OnAttackAnimationEnd();
    }

    public void OnHitAnimationEnd()
    {
        _controller.OnHitAnimationEnd();
    }

    public void OnDeathAnimationEnd()
    {
        _controller.OnDeathAnimationEnd();
    }

    // ── 보스 전용 ──

    public void OnMeleeHitboxStart()
    {
        _controller.OnMeleeHitboxStart();
    }

    public void OnMeleeHitboxEnd()
    {
        _controller.OnMeleeHitboxEnd();
    }

    public void OnJumpSlamHitboxStart()
    {
        _controller.OnJumpSlamHitboxStart();
    }

    public void OnJumpSlamHitboxEnd()
    {
        _controller.OnJumpSlamHitboxEnd();
    }

    public void OnPoisonVomitFire()
    {
        _controller.OnPoisonVomitFire();
    }

    public void OnSummonTrigger()
    {
        _controller.OnSummonTrigger();
    }

    public void OnAttackIdleEnd()
    {
        //_controller.OnAttackIdleEnd();
    }
}