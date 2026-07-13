using UnityEngine;

/// <summary>
/// Animator가 부착된 자식 오브젝트에 붙이는 중계 스크립트.
/// Animation Event는 Animator가 붙은 오브젝트에게만 전달되므로,
/// 실제 로직이 있는 부모의 MonsterController로 그대로 전달만 한다.
/// </summary>
public class MonsterAnimationEventRelay : MonoBehaviour
{
    MonsterController _controller;

    void Awake()
    {
        _controller = GetComponentInParent<MonsterController>();
    }

    //public void OnAttackHitCheck()
    //{
    //    _controller.OnAttackHitCheck();
    //}

    //public void OnAttackAnimationEnd()
    //{
    //    _controller.OnAttackAnimationEnd();
    //}

    //public void OnHitAnimationEnd()
    //{
    //    _controller.OnHitAnimationEnd();
    //}

    //public void OnDeathAnimationEnd()
    //{
    //    _controller.OnDeathAnimationEnd();
    //}
}
