using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 강화 특수효과(검기)로 발생하는 투사체. 앞으로 날아가며 지나가는 몬스터에게 데미지를 준다.
/// 여러 몬스터를 관통 가능(한 대상당 한 번만), 최대 사거리 도달 시 스스로 사라짐.
/// </summary>
public class SwordWaveProjectile : MonoBehaviour
{
    [SerializeField] float _speed = 12f;
    [SerializeField] float _maxDistance = 8f;

    int _damage;
    float _hitRadius;
    LayerMask _monsterLayer;
    Vector3 _startPosition;
    HashSet<Collider> _alreadyHit = new HashSet<Collider>();

    /// <summary>생성 직후 바로 호출해서 초기화 (PlayerController가 Instantiate 후 호출)</summary>
    public void Init(int damage, float hitRadius, LayerMask monsterLayer)
    {
        _damage = damage;
        _hitRadius = hitRadius > 0f ? hitRadius : 0.5f;
        _monsterLayer = monsterLayer;
        _startPosition = transform.position;
    }

    void Update()
    {
        transform.position += transform.forward * _speed * Time.deltaTime;

        CheckHit();

        if (Vector3.Distance(_startPosition, transform.position) >= _maxDistance)
            Destroy(gameObject);
    }

    void CheckHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _hitRadius, _monsterLayer);
        foreach (Collider hit in hits)
        {
            if (_alreadyHit.Contains(hit))
                continue;
            _alreadyHit.Add(hit);

            if (hit.TryGetComponent<IDamageable>(out IDamageable target))
                target.TakeDamage(_damage);
        }
    }
}
