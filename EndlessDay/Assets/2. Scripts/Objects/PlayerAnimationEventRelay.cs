using UnityEngine;

/// <summary>
/// Animator가 부착된 자식 오브젝트에 붙이는 중계 스크립트.
/// Animation Event는 Animator가 붙은 오브젝트에게만 전달되므로,
/// 실제 로직이 있는 부모의 PlayerController로 그대로 전달만 한다.
/// </summary>
public class PlayerAnimationEventRelay : MonoBehaviour
{
    PlayerController _controller;

    void Awake()
    {
        _controller = GetComponentInParent<PlayerController>();
    }

    // Animation Event가 이 함수를 호출 → 부모의 실제 함수로 전달
    public void OnRollAnimationEnd()
    {
        _controller.OnRollAnimationEnd();
    }

    public void OnRollInvincibleStart()
    {
        _controller.OnRollInvincibleStart();
    }

    public void OnRollInvincibleEnd()
    {
        _controller.OnRollInvincibleEnd();
    }

    public void OnAttackHitboxStart()
    {
        _controller.OnAttackHitboxStart();
    }

    public void OnAttackHitboxEnd()
    {
        _controller.OnAttackHitboxEnd();
    }

    public void OnAttackAnimationEnd()
    {
        _controller.OnAttackAnimationEnd();
    }

    public void OnSkillHitCheck()
    {
        _controller.OnSkillHitCheck();
    }

    public void OnSkillAnimationEnd()
    {
        _controller.OnSkillAnimationEnd();
    }

    public void OnHitAnimationEnd()
    {
        _controller.OnHitAnimationEnd();
    }

    public void OnDeathAnimationEnd()
    {
        _controller.OnDeathAnimationEnd();
    }
}