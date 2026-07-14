using UnityEngine;

public enum MonsterZoneType
{
    Detect,
    ChaseReset,
    Attack,
}

/// <summary>
/// Monster 자식 오브젝트(SphereCollider, Is Trigger)에 붙이는 범위 감지 컴포넌트.
/// DetectZone/ChaseResetZone/AttackZone 3곳에 전부 이 스크립트 하나로 재사용한다.
/// 실제로 플레이어가 들어오고 나가는 "순간"만 부모 MonsterController에 알려준다.
/// </summary>
public class MonsterDetectionZone : MonoBehaviour
{
    [SerializeField] MonsterZoneType _zoneType;
    MonsterController _controller;
    Collider _collider;

    void Awake()
    {
        _controller = GetComponentInParent<MonsterController>();
        _collider = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        _controller.OnZoneEnter(_zoneType);
        Debug.Log("Player" + _zoneType);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        _controller.OnZoneExit(_zoneType);
        Debug.Log("Player exit" + _zoneType);
    }
}