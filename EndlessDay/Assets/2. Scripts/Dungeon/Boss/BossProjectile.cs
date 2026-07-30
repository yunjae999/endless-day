using UnityEngine;

/// <summary>
/// 보스 투사체(독액 토하기 등). 앞으로 날아가다 플레이어와 부딪히면 데미지를 주고 사라진다.
/// 콜라이더(Is Trigger) 기반 판정 - 플레이어 검기(SwordWaveProjectile)와 같은 원리, 방향만 반대(적 → 플레이어).
/// </summary>
public class BossProjectile : MonoBehaviour
{
    [SerializeField] float _speed = 8f;
    [SerializeField] float _maxDistance = 15f;

    int _damage;
    Vector3 _startPosition;
    bool _hasHit;

    /// <summary>생성 직후 바로 호출해서 초기화 (BossController가 Instantiate 후 호출)</summary>
    public void Init(int damage)
    {
        _damage = damage;
        _startPosition = transform.position;
    }

    void Update()
    {
        transform.position += transform.forward * _speed * Time.deltaTime;

        if (Vector3.Distance(_startPosition, transform.position) >= _maxDistance)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hasHit)
            return;
        if (!other.CompareTag("Player"))
            return;

        if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            int actualDamage = target.TakeDamage(_damage);
            DamagePopupSpawner._instance?.Spawn(target.DamagePopupPosition, actualDamage, false, true);
        }

        _hasHit = true;
        Destroy(gameObject);
    }
}