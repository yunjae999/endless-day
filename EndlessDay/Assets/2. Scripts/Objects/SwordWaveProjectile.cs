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
    [SerializeField] LayerMask _obstacleLayer;   // 벽 등 - 이 레이어에 닿으면 즉시 사라짐

    int _damage;
    float _hitRadius;
    LayerMask _monsterLayer;
    bool _isCrit;
    Vector3 _startPosition;
    HashSet<Collider> _alreadyHit = new HashSet<Collider>();

    /// <summary>생성 직후 바로 호출해서 초기화 (PlayerController가 Instantiate 후 호출)</summary>
    public void Init(int damage, float hitRadius, LayerMask monsterLayer, bool isCrit)
    {
        _damage = damage;
        _hitRadius = hitRadius > 0f ? hitRadius : 0.5f;
        _monsterLayer = monsterLayer;
        _isCrit = isCrit;
        _startPosition = transform.position;
    }

    void Update()
    {
        float step = _speed * Time.deltaTime;

        // 이번 프레임에 이동할 거리만큼 미리 검사 - 얇은 벽도 뚫고 지나가지 않게
        if (Physics.Raycast(transform.position, transform.forward, step, _obstacleLayer))
        {
            Destroy(gameObject);
            return;
        }

        transform.position += transform.forward * step;

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
            {
                int actualDamage = target.TakeDamage(_damage);
                if (actualDamage > 0)
                    DamagePopupSpawner._instance?.Spawn(target.DamagePopupPosition, actualDamage, _isCrit, false);
            }
        }
    }
}