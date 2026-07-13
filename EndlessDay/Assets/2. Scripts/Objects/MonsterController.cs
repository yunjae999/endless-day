using UnityEngine;
using UnityEngine.AI;
using Defines;

public class MonsterController : MonoBehaviour
{
    Animator _animator;
    NavMeshAgent _agent;
    MonsterActionState _currentState;

    [Header("비추적 상태 (Idle ↔ Patrol)")]
    [SerializeField] float _idleMinTime = 2f;
    [SerializeField] float _idleMaxTime = 4f;
    [SerializeField] float _patrolMinTime = 2f;
    [SerializeField] float _patrolMaxTime = 4f;
    [SerializeField] float _patrolSpeed = 1f;
    [SerializeField] float _patrolRadius = 5f;   // 랜덤 방향 대신 반경 내 랜덤 지점

    float _stateTimer;

    [Header("Chase")]
    [SerializeField] float _chaseSpeed = 3f;
    [SerializeField] float _destinationUpdateInterval = 0.2f;
    float _destinationTimer;

    Transform _target;
    bool _isPlayerDetected;
    bool _isPlayerInChaseRange;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _agent = GetComponent<NavMeshAgent>();
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

        bool shouldChase = _isPlayerDetected || _isPlayerInChaseRange;

        if (shouldChase)
        {
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
                // 목적지에 도착했거나 시간이 다 되면 Idle로
                if (_stateTimer <= 0f || (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance))
                    EnterIdle();
                break;

            case MonsterActionState.CHASE:
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
        }
    }
}