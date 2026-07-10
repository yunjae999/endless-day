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
        PlayerProcess();
    }

    void PlayerProcess()
    {
        switch (_currentState)
        {
            case PlayerActionState.IDLE:
                // Move로 전환
                if(HasMoveInput())
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
        Debug.Log(_runInput);
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
        if(_moveDir.sqrMagnitude < 0.01f)
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
}
