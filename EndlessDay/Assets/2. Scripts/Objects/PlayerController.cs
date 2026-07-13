using Defines;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Animator _animator;
    PlayerActionState _currentState;
    PlayerStatManager _statManager;
    Vector3 _moveDir;
    [SerializeField] float _rotateSpeed = 2f;
    bool _runInput;
    bool _isRun;

    [Header("Roll")]
    [SerializeField] float _rollDistance = 7.5f;
    [SerializeField] float _rollDuration = 1.1f;   // 속도 계산용 (_rollDistance / _rollDuration)
    [SerializeField] float _rollCooldown = 2.0f;   // 구르기가 "끝난 후" 대기시간

    Vector3 _rollDirection;
    float _rollSpeed;
    float _rollCooldownTimer;

    public bool IsInvincible { get; private set; }

    [Header("Attack")]
    [SerializeField] Collider _attackHitbox;       // 전방 고정 BoxCollider (Is Trigger), 평소엔 꺼둠
    [SerializeField] LayerMask _monsterLayer;      // 몬스터 레이어만 판정
    HashSet<Collider> _alreadyHit = new HashSet<Collider>();

    [Header("Skill (검: 회전 베기, 반경 3m / 쿨타임 6초)")]
    [SerializeField] float _skillRadius = 3f;
    [SerializeField] float _skillCooldown = 6f;
    float _skillCooldownTimer;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _statManager = GetComponentInChildren<PlayerStatManager>();
        _currentState = PlayerActionState.IDLE;

        if (_attackHitbox != null)
            _attackHitbox.enabled = false;
    }
    void Start()
    {
        _statManager.InitBaseStats();
    }
    void Update()
    {
        UpdateRollCooldown();
        UpdateSkillCooldown();
        PlayerProcess();
    }
    void PlayerProcess()
    {
        switch (_currentState)
        {
            case PlayerActionState.IDLE:
                // Move로 전환
                if (HasMoveInput())
                    ChangeActionState(PlayerActionState.MOVE);
                break;
            case PlayerActionState.MOVE:
                // Idle로 전환
                if (!HasMoveInput())
                {
                    SetRun(false);
                    ChangeActionState(PlayerActionState.IDLE);
                    return;
                }
                UpdateRun();
                // 이동
                Move();
                //회전
                Rotate();
                break;
            case PlayerActionState.ROLL:
                UpdateRoll();
                break;
            case PlayerActionState.ATTACK:
                // 판정/종료는 Animation Event가 담당, 여기선 할 일 없음
                break;
            case PlayerActionState.SKILL:
                // 판정/종료는 Animation Event가 담당, 여기선 할 일 없음
                break;
        }
    }
    public void ChangeActionState(PlayerActionState state)
    {
        if (_currentState == state)
            return;
        _currentState = state;
        _animator.SetInteger("ActionState", (int)_currentState);
    }
    public void OnMove(InputValue value)
    {
        Vector2 moveDir = value.Get<Vector2>();
        _moveDir = new Vector3(
            moveDir.x,
            0f,
            moveDir.y
        );
    }
    public void OnRun(InputValue value)
    {
        _runInput = value.isPressed;
    }
    public void OnRoll(InputValue value)
    {
        if (!value.isPressed)
            return;
        TryStartRoll();
    }
    public void OnAttack(InputValue value)
    {
        if (!value.isPressed)
            return;
        TryStartAttack();
    }
    public void OnSkill(InputValue value)
    {
        if (!value.isPressed)
            return;
        TryStartSkill();
    }
    void SetRun(bool isRun)
    {
        if (_isRun == isRun)
            return;
        _isRun = isRun;
        _animator.SetBool("IsRun", _isRun);
    }
    void UpdateRun()
    {
        bool shouldRun =
            _runInput &&
            _currentState == PlayerActionState.MOVE &&
            HasMoveInput();
        SetRun(shouldRun);
    }
    void Move()
    {
        float speed = _isRun ? _statManager.BaseRunSpeed : _statManager.BaseMoveSpeed;
        transform.position += _moveDir.normalized * speed * Time.deltaTime;
    }
    void Rotate()
    {
        if (_moveDir.sqrMagnitude < 0.01f)
            return;
        Quaternion targetRotation =
            Quaternion.LookRotation(_moveDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotateSpeed * Time.deltaTime
        );
    }
    bool HasMoveInput()
    {
        return _moveDir.sqrMagnitude > 0.01f;
    }

    // ─────────────────────────────────────────────
    // Roll
    // ─────────────────────────────────────────────

    void TryStartRoll()
    {
        if (_rollCooldownTimer > 0f)
            return;

        // 기획서 FSM 규칙: Roll은 Idle/Move에서만 진입 가능
        if (_currentState != PlayerActionState.IDLE && _currentState != PlayerActionState.MOVE)
            return;

        EnterRoll();
    }

    void EnterRoll()
    {
        _rollDirection = HasMoveInput() ? _moveDir.normalized : transform.forward;
        _rollSpeed = _rollDistance / _rollDuration;

        transform.rotation = Quaternion.LookRotation(_rollDirection);

        ChangeActionState(PlayerActionState.ROLL);
    }

    void UpdateRoll()
    {
        transform.position += _rollDirection * _rollSpeed * Time.deltaTime;
    }

    void UpdateRollCooldown()
    {
        if (_rollCooldownTimer > 0f)
            _rollCooldownTimer -= Time.deltaTime;
    }

    public void OnRollInvincibleStart()
    {
        IsInvincible = true;
    }

    public void OnRollInvincibleEnd()
    {
        IsInvincible = false;
    }

    public void OnRollAnimationEnd()
    {
        IsInvincible = false;
        _rollCooldownTimer = _rollCooldown;
        ChangeActionState(HasMoveInput() ? PlayerActionState.MOVE : PlayerActionState.IDLE);
    }

    // ─────────────────────────────────────────────
    // Attack
    // 쿨타임 없음(기획서 방침) - 애니메이션 자체 길이가 곧 공격 간격 역할을 함.
    // 판정 콜라이더는 실제로 무기가 지나가는 구간에만 Animation Event로 켜고 끈다.
    // ─────────────────────────────────────────────

    void TryStartAttack()
    {
        // 기획서 FSM 규칙: Attack은 Idle/Move에서만 진입 가능
        if (_currentState != PlayerActionState.IDLE && _currentState != PlayerActionState.MOVE)
            return;

        EnterAttack();
    }

    void EnterAttack()
    {
        // 공격 시작 시점 방향으로 고정 (추후 마우스 조준 방향으로 교체 예정)
        Vector3 attackDir = HasMoveInput() ? _moveDir.normalized : transform.forward;
        transform.rotation = Quaternion.LookRotation(attackDir);

        ChangeActionState(PlayerActionState.ATTACK);
    }

    public void OnAttackHitboxStart()
    {
        _alreadyHit.Clear();   // 이번 공격에서 맞은 대상 기록 초기화
        if (_attackHitbox != null)
            _attackHitbox.enabled = true;
    }

    public void OnAttackHitboxEnd()
    {
        if (_attackHitbox != null)
            _attackHitbox.enabled = false;
    }

    /// <summary>AttackHitboxTrigger(자식)가 트리거 진입을 감지하면 호출</summary>
    public void OnAttackHitboxTriggerEnter(Collider other)
    {
        // 몬스터 레이어가 아니면 무시
        if (((1 << other.gameObject.layer) & _monsterLayer) == 0)
            return;

        // 같은 스윙에서 이미 맞춘 대상이면 무시 (중복 데미지 방지)
        if (_alreadyHit.Contains(other))
            return;
        _alreadyHit.Add(other);

        if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            // TODO: PlayerStatManager의 최종 공격력으로 교체
            target.TakeDamage(20);
        }
    }

    public void OnAttackAnimationEnd()
    {
        ChangeActionState(HasMoveInput() ? PlayerActionState.MOVE : PlayerActionState.IDLE);
    }

    // ─────────────────────────────────────────────
    // Skill (검: 회전 베기)
    // Attack과 달리 쿨타임 있음, 전방 Box가 아니라 자기 자신 중심 원형(OverlapSphere) 판정.
    // ─────────────────────────────────────────────

    void TryStartSkill()
    {
        if (_skillCooldownTimer > 0f)
            return;

        // 기획서 FSM 규칙: Skill은 Idle/Move에서만 진입 가능
        if (_currentState != PlayerActionState.IDLE && _currentState != PlayerActionState.MOVE)
            return;

        EnterSkill();
    }

    void EnterSkill()
    {
        _skillCooldownTimer = _skillCooldown;
        ChangeActionState(PlayerActionState.SKILL);
    }

    /// <summary>회전 베기 판정 프레임에 Animation Event로 연결 - 반경 안 몬스터 전체를 즉시 조회</summary>
    public void OnSkillHitCheck()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _skillRadius, _monsterLayer);
        foreach (Collider hit in hits)
        {
            // TODO: 몬스터 쪽 TakeDamage 인터페이스 완성되면 교체
            Debug.Log("스킬 판정 성공 : " + hit.name);
        }
    }

    public void OnSkillAnimationEnd()
    {
        ChangeActionState(HasMoveInput() ? PlayerActionState.MOVE : PlayerActionState.IDLE);
    }

    void UpdateSkillCooldown()
    {
        if (_skillCooldownTimer > 0f)
            _skillCooldownTimer -= Time.deltaTime;
    }
}