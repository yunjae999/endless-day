using UnityEngine;
using UnityEngine.AI;
using Defines;

public class MonsterController : MonoBehaviour, IDamageable
{
    Animator _animator;
    NavMeshAgent _agent;
    MonsterActionState _currentState;

    [Header("스탯")]
    [SerializeField] int _maxHP = 30;
    [SerializeField] int _attackDamage = 10;
    [SerializeField] int _expReward = 5;

    [Header("비추적 상태 (Idle ↔ Patrol)")]
    [SerializeField] float _idleMinTime = 2f;
    [SerializeField] float _idleMaxTime = 4f;
    [SerializeField] float _patrolMinTime = 2f;
    [SerializeField] float _patrolMaxTime = 4f;
    [SerializeField] float _patrolSpeed = 1f;
    [SerializeField] float _patrolRadius = 5f;

    float _stateTimer;

    [Header("Chase")]
    [SerializeField] float _chaseSpeed = 3f;
    [SerializeField] float _destinationUpdateInterval = 0.2f;
    float _destinationTimer;

    [Header("Attack")]
    [SerializeField] float _attackCooldown = 1.5f;
    float _attackCooldownTimer;

    Transform _target;
    bool _isPlayerDetected;
    bool _isPlayerInChaseRange;
    bool _isPlayerInAttackRange;

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        CurrentHP = _maxHP;
    }

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            _target = player.transform;

        EnterIdle();
    }

    void Update()
    {
        MonsterProcess();
    }

    void MonsterProcess()
    {
        _stateTimer -= Time.deltaTime;
        UpdateAttackCooldown();

        // Attack/Hit/Death 중엔 Animation Event가 상태를 관리하므로 Chase 판단이 끼어들지 않게 막음
        if (_currentState == MonsterActionState.ATTACK ||
            _currentState == MonsterActionState.HIT ||
            _currentState == MonsterActionState.DEATH)
            return;

        bool shouldChase = _isPlayerDetected || _isPlayerInChaseRange;

        if (shouldChase)
        {
            if (_isPlayerInAttackRange)
            {
                if (_attackCooldownTimer <= 0f)
                {
                    EnterAttack();
                    return;
                }

                if (_currentState != MonsterActionState.ATTACK_IDLE)
                    EnterAttackIdle();
                return;   // 대기 중이므로 이동 갱신 없음
            }

            if (_currentState != MonsterActionState.CHASE)
                EnterChase();

            UpdateChaseDestination();
            return;
        }

        switch (_currentState)
        {
            case MonsterActionState.IDLE:
                if (_stateTimer <= 0f)
                    EnterPatrol();
                break;

            case MonsterActionState.PATROL:
                if (_stateTimer <= 0f || (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance))
                    EnterIdle();
                break;

            case MonsterActionState.CHASE:
            case MonsterActionState.ATTACK_IDLE:
                EnterIdle();
                break;
        }
    }

    public void ChangeActionState(MonsterActionState state)
    {
        if (_currentState == state)
            return;
        _currentState = state;

        if (_animator != null)
            _animator.SetInteger("ActionState", (int)_currentState);
    }

    // ─────────────────────────────────────────────
    // Idle / Patrol
    // ─────────────────────────────────────────────

    void EnterIdle()
    {
        _stateTimer = Random.Range(_idleMinTime, _idleMaxTime);
        _agent.isStopped = true;
        ChangeActionState(MonsterActionState.IDLE);
    }

    void EnterPatrol()
    {
        _stateTimer = Random.Range(_patrolMinTime, _patrolMaxTime);
        _agent.speed = _patrolSpeed;
        _agent.isStopped = false;

        if (TryGetRandomPatrolPoint(out Vector3 point))
            _agent.SetDestination(point);

        ChangeActionState(MonsterActionState.PATROL);
    }

    bool TryGetRandomPatrolPoint(out Vector3 result)
    {
        Vector2 randomCircle = Random.insideUnitCircle * _patrolRadius;
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = transform.position;
        return false;
    }

    // ─────────────────────────────────────────────
    // Chase
    // ─────────────────────────────────────────────

    void EnterChase()
    {
        _agent.speed = _chaseSpeed;
        _agent.isStopped = false;
        _destinationTimer = 0f;
        ChangeActionState(MonsterActionState.CHASE);
    }

    void UpdateChaseDestination()
    {
        if (_target == null)
            return;

        _destinationTimer -= Time.deltaTime;
        if (_destinationTimer > 0f)
            return;

        _destinationTimer = _destinationUpdateInterval;
        _agent.SetDestination(_target.position);
    }

    // ─────────────────────────────────────────────
    // Attack
    // ─────────────────────────────────────────────

    void EnterAttack()
    {
        _agent.isStopped = true;   // 제자리에서 공격
        ChangeActionState(MonsterActionState.ATTACK);
    }

    void EnterAttackIdle()
    {
        _agent.isStopped = true;
        ChangeActionState(MonsterActionState.ATTACK_IDLE);
    }

    void UpdateAttackCooldown()
    {
        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer -= Time.deltaTime;
    }

    /// <summary>공격 판정 프레임에 Animation Event로 연결</summary>
    public void OnAttackHitCheck()
    {
        _attackCooldownTimer = _attackCooldown;

        if (_isPlayerInAttackRange && _target != null && _target.TryGetComponent<IDamageable>(out IDamageable player))
        {
            player.TakeDamage(_attackDamage);
        }
    }

    /// <summary>공격 애니메이션이 끝나는 프레임에 Animation Event로 연결</summary>
    public void OnAttackAnimationEnd()
    {
        if (_isPlayerInAttackRange)
            EnterAttackIdle();
        else
            EnterChase();
    }

    // ─────────────────────────────────────────────
    // MonsterDetectionZone(자식)이 호출
    // ─────────────────────────────────────────────

    public void OnZoneEnter(MonsterZoneType zone)
    {
        switch (zone)
        {
            case MonsterZoneType.Detect:
                _isPlayerDetected = true;
                break;
            case MonsterZoneType.ChaseReset:
                _isPlayerInChaseRange = true;
                break;
            case MonsterZoneType.Attack:
                _isPlayerInAttackRange = true;
                break;
        }
    }

    public void OnZoneExit(MonsterZoneType zone)
    {
        switch (zone)
        {
            case MonsterZoneType.Detect:
                _isPlayerDetected = false;
                break;
            case MonsterZoneType.ChaseReset:
                _isPlayerInChaseRange = false;
                break;
            case MonsterZoneType.Attack:
                _isPlayerInAttackRange = false;
                break;
        }
    }

    // ─────────────────────────────────────────────
    // IDamageable
    // ─────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        if (IsDead)
            return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        _agent.isStopped = true;

        ChangeActionState(IsDead ? MonsterActionState.DEATH : MonsterActionState.HIT);
    }

    /// <summary>피격 애니메이션이 끝나는 프레임에 Animation Event로 연결</summary>
    public void OnHitAnimationEnd()
    {
        if (IsDead)
            return;
        _agent.isStopped = false;
        ChangeActionState(MonsterActionState.CHASE);
    }

    /// <summary>사망 애니메이션이 끝나는 프레임에 Animation Event로 연결</summary>
    public void OnDeathAnimationEnd()
    {
        GameSession._instance.AddExp(_expReward);

        // TODO: 골드 드랍
        Destroy(gameObject);
    }
}