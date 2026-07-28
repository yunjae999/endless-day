using UnityEngine;

/// <summary>
/// 보스 공격 히트박스(자식 콜라이더)에 붙이는 중계 스크립트.
/// 어떤 공격인지 인스펙터에서 지정하면, 트리거 발생 시 BossController의 처리 메서드로 전달한다.
/// 근접/점프내려찍기/독액토하기 전부 이 스크립트 하나로 커버 (히트박스마다 하나씩 붙이고 종류만 다르게 지정).
/// </summary>
public class BossAttackHitboxTrigger : MonoBehaviour
{
    [SerializeField] BossAttackType _attackType;

    BossController _controller;

    void Awake()
    {
        _controller = GetComponentInParent<BossController>();
    }

    void OnTriggerEnter(Collider other)
    {
        _controller.OnAttackHitboxTriggerEnter(_attackType, other);
    }
}
