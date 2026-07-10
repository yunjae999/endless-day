using Defines;
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

    [Header("Roll (기획서 수치: 3.5m / 0.35s / 무적 0.2s / 쿨타임 1.2s)")]
    [SerializeField] float _rollDistance = 3.5f;
    [SerializeField] float _rollDuration = 0.35f;
    [SerializeField] float _rollInvincibleDuration = 0.2f;
    [SerializeField] float _rollCooldown = 1.2f;

    Vector3 _rollDirection;
    float _rollSpeed;
    float _rollTimer;
    float _rollCooldownTimer;

    public bool IsInvincible { get; private set; }

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _statManager = GetComponentInChildren<PlayerStatManager>();
        _currentState = PlayerActionState.IDLE;
    }
    void Start()
    {
        _statManager.InitBaseStats();
    }
    void Update()
    {
        UpdateRollCooldown();
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
                break;
            case PlayerActionState.SKILL:
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
    // 이동/무적은 내부 타이머로 계산(기획서 수치 그대로),
    // 단 "상태 종료" 시점만은 타이머가 아니라 Animation Event(OnRollAnimationEnd)가 결정한다.
    // → 애니메이션 클립 실제 길이와 코드 수치가 어긋나도 어색해지지 않음.
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
        _rollTimer = 0f;
        _rollCooldownTimer = _rollCooldown;

        transform.rotation = Quaternion.LookRotation(_rollDirection);

        ChangeActionState(PlayerActionState.ROLL);
    }

    void UpdateRoll()
    {
        _rollTimer += Time.deltaTime;

        transform.position += _rollDirection * _rollSpeed * Time.deltaTime;

        // 시작~중반(0~0.2초) 구간만 무적
        IsInvincible = _rollTimer <= _rollInvincibleDuration;

        // 주의: 여기서 더 이상 자동으로 상태를 끝내지 않음 (OnRollAnimationEnd가 담당)
    }

    void UpdateRollCooldown()
    {
        if (_rollCooldownTimer > 0f)
            _rollCooldownTimer -= Time.deltaTime;
    }

    /// <summary>Roll 애니메이션 클립이 끝나는 프레임에 Animation Event로 연결</summary>
    public void OnRollAnimationEnd()
    {
        IsInvincible = false;
        ChangeActionState(HasMoveInput() ? PlayerActionState.MOVE : PlayerActionState.IDLE);
    }
}