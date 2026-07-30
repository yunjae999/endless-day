using UnityEngine;
using UnityEngine.AI;
using Defines;

public class MonsterController : MonoBehaviour, IDamageable
{
    protected Animator _animator;
    NavMeshAgent _agent;
    MonsterActionState _currentState;

    [Header("스탯 (전부 MonsterTable에서 로드, 인스펙터 값은 무시됨)")]
    [SerializeField] int _maxHP = 30;
    [SerializeField] protected int _monsterID;
    [SerializeField] protected int _attackDamage = 10;
    [SerializeField] int _defense;
    [SerializeField] int _expReward = 5;
    [SerializeField] int _goldReward = 10;

    [Header("감지 존 반경 (콜라이더 연결해두면 테이블 값으로 자동 설정, 안 해두면 무시)")]
    [SerializeField] SphereCollider _detectZoneCollider;
    [SerializeField] SphereCollider _chaseResetZoneCollider;

    [Header("비추적 상태 (Idle ↔ Patrol)")]
    [SerializeField] float _idleMinTime = 2f;
    [SerializeField] float _idleMaxTime = 4f;
    [SerializeField] float _patrolMinTime = 2f;
    [SerializeField] float _patrolMaxTime = 4f;
    [SerializeField] float _patrolSpeed = 1f;
    [SerializeField] float _patrolRadius = 5f;

    float _stateTimer;

    [Header("Chase")]
    [SerializeField] protected float _chaseSpeed = 3f;
    [SerializeField] float _destinationUpdateInterval = 0.2f;
    float _destinationTimer;

    [Header("Attack")]
    [SerializeField] float _attackCooldown = 1.5f;
    float _attackCooldownTimer;

    protected Transform _target;
    protected bool _isPlayerDetected;
    bool _isPlayerInChaseRange;
    bool _isPlayerInAttackRange;

    public int CurrentHP { get; private set; }
    public int MaxHP => _maxHP;

    [SerializeField] protected Vector3 _damagePopupOffset = Vector3.up;
    public Vector3 DamagePopupPosition => transform.position + _damagePopupOffset;
    public bool IsDead => CurrentHP <= 0;

    [SerializeField] UIWorldHealthBar _healthBar;
    [SerializeField] Collider _bodyCollider;   // 플레이어와 물리적으로 부딪히는 콜라이더 - 사망 시 꺼서 통과 가능하게

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _agent = GetComponent<NavMeshAgent>();

        LoadStatsFromTable();
        CurrentHP = _maxHP;

        if (_healthBar != null)
        {
            DataTable monsterTable = TableDataManager._instance.Get(TableName.MonsterTable);
            string monsterName = monsterTable.ToS(_monsterID, "MonsterName");
            _healthBar.Init(monsterName, this);
        }
    }

    /// <summary>MonsterTable에서 이 몬스터(_monsterID)의 스탯을 전부 읽어와 인스펙터 값을 덮어씀</summary>
    void LoadStatsFromTable()
    {
        DataTable monsterTable = TableDataManager._instance.Get(TableName.MonsterTable);

        _maxHP = monsterTable.ToI(_monsterID, "MaxHP");
        _attackDamage = monsterTable.ToI(_monsterID, "AttackPower");
        _defense = monsterTable.ToI(_monsterID, "Defense");
        _chaseSpeed = monsterTable.ToF(_monsterID, "MoveSpeed");
        _expReward = monsterTable.ToI(_monsterID, "ExpReward");
        _goldReward = monsterTable.ToI(_monsterID, "GoldDrop");

        if (_detectZoneCollider != null)
            _detectZoneCollider.radius = monsterTable.ToF(_monsterID, "DetectRange");

        if (_chaseResetZoneCollider != null)
            _chaseResetZoneCollider.radius = monsterTable.ToF(_monsterID, "ChaseResetRange");
    }

    protected virtual void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            _target = player.transform;

        EnterInitialState();
    }

    /// <summary>일반 몬스터는 Idle로 시작. 보스처럼 다르게 시작하고 싶으면 이걸 오버라이드</summary>
    protected virtual void EnterInitialState()
    {
        EnterIdle();
    }

    protected virtual void Update()
    {
        MonsterProcess();
    }

    protected bool _isActive = true;   // 보스처럼 활성화 전엔 완전히 대기시키고 싶을 때 false로 시작

    void MonsterProcess()
    {
        if (!_isActive)
            return;

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
            if (IsPlayerInAttackRange())
            {
                if (IsAttackReady())
                {
                    PerformAttack();
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

        _healthBar?.ShowTemporarily();
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

    /// <summary>공격을 실행할 준비가 됐는지. 일반 몬스터는 쿨타임 하나만 봄 - 보스는 공격별 쿨타임 여러 개를 봐야 하니 오버라이드</summary>
    protected virtual bool IsAttackReady()
    {
        return _attackCooldownTimer <= 0f;
    }

    /// <summary>실제로 어떤 공격을 실행할지. 일반 몬스터는 하나뿐 - 보스는 여러 공격 중 골라서 실행하도록 오버라이드</summary>
    protected virtual void PerformAttack()
    {
        EnterAttack();
    }

    protected void EnterAttack()
    {
        _agent.isStopped = true;   // 제자리에서 공격
        ChangeActionState(MonsterActionState.ATTACK);

        _healthBar?.ShowTemporarily();
    }

    void EnterAttackIdle()
    {
        _agent.isStopped = true;
        ChangeActionState(MonsterActionState.ATTACK_IDLE);

        _healthBar?.ShowTemporarily();
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
            int actualDamage = player.TakeDamage(_attackDamage);
            if (actualDamage > 0)
                DamagePopupSpawner._instance?.Spawn(player.DamagePopupPosition, actualDamage, false, true);
        }
    }

    /// <summary>공격 애니메이션이 끝나는 프레임에 Animation Event로 연결</summary>
    /// <summary>공격이 끝나면 일단 무조건 AttackIdle로. 다시 공격할지 추적할지는 그 다음부터 MonsterProcess()가 매 프레임 판단</summary>
    public virtual void OnAttackAnimationEnd()
    {
        EnterAttackIdle();
    }

    /// <summary>지금 공격 사거리 안에 있는지. 일반 몬스터는 Attack 존(콜라이더) 하나로 판단 - 보스는 공격별 사거리를 직접 계산해야 하니 오버라이드</summary>
    protected virtual bool IsPlayerInAttackRange()
    {
        return _isPlayerInAttackRange;
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

    public int TakeDamage(int amount)
    {
        if (IsDead)
            return 0;

        int reducedAmount = ApplyDefense(amount);
        CurrentHP = Mathf.Max(0, CurrentHP - reducedAmount);

        if (IsDead)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;   // isStopped만으론 서서히 감속되어 플레이어와 부딪힐 수 있어 즉시 0으로
            if (_bodyCollider != null)
                _bodyCollider.enabled = false;   // 죽는 애니메이션 시작하는 즉시 플레이어가 통과 가능하게
            ChangeActionState(MonsterActionState.DEATH);
        }
        else if (_currentState != MonsterActionState.ATTACK)   // 공격 중이면 애니메이션은 안 끊음 (데미지는 이미 적용됨)
        {
            _agent.isStopped = true;
            ChangeActionState(MonsterActionState.HIT);
        }

        if (IsDead)
            _healthBar?.Hide();
        else
            _healthBar?.ShowTemporarily();

        return reducedAmount;
    }

    /// <summary>방어력만큼 받는 데미지를 비율로 감소. 100 방어력 = 데미지 절반 (플레이어 쪽과 동일한 공식)</summary>
    int ApplyDefense(int rawDamage)
    {
        float reduced = rawDamage * (100f / (100f + _defense));
        return Mathf.Max(1, Mathf.RoundToInt(reduced));   // 최소 1은 들어가게
    }

    /// <summary>피격 애니메이션이 끝나는 프레임에 Animation Event로 연결</summary>
    public void OnHitAnimationEnd()
    {
        if (IsDead)
            return;
        _agent.isStopped = false;
        ChangeActionState(MonsterActionState.CHASE);
    }


    public void OnDeathAnimationEnd()
    {
        GameSession._instance.AddExp(_expReward);
        GameSession._instance.AddGold(_goldReward);
        GameSession._instance.AddMonsterKill();

        Destroy(gameObject);
    }
}