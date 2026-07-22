using Defines;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IDamageable
{
    Animator _animator;
    NavMeshAgent _agent;
    PlayerActionState _currentState;
    PlayerStatManager _statManager;
    Vector3 _moveDir;
    [SerializeField] float _rotateSpeed = 2f;
    bool _runInput;
    bool _isRun;

    [Header("HP")]
    [SerializeField] int _maxHP = 100;   // 기획서 기본 스탯

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    [Header("Roll")]
    [SerializeField] float _rollDistance = 7.5f;
    [SerializeField] float _rollDuration = 1.1f;   // 속도 계산용 (_rollDistance / _rollDuration)
    [SerializeField] float _rollCooldown = 2.0f;   // 구르기가 "끝난 후" 대기시간

    Vector3 _rollDirection;
    float _rollSpeed;
    float _rollCooldownTimer;

    /// <summary>1=바로 사용 가능(꽉 참), 0=방금 씀. 쿨타임 진행에 따라 다시 채워짐. HUD 표시용</summary>
    public float RollReadyRatio => _rollCooldownTimer > 0f ? 1f - (_rollCooldownTimer / _rollCooldown) : 1f;

    public bool IsInvincible { get; private set; }

    [Header("Attack")]
    [SerializeField] Collider _attackHitbox;       // 전방 고정 BoxCollider (Is Trigger), 평소엔 꺼둠
    [SerializeField] LayerMask _monsterLayer;      // 몬스터 레이어만 판정
    HashSet<Collider> _alreadyHit = new HashSet<Collider>();

    [Header("강화 특수효과 (검기 등)")]
    [SerializeField] SwordWaveProjectile _swordWavePrefab;
    Dictionary<int, int> _specialEffectAttackCounters = new Dictionary<int, int>();

    [Header("Skill (검: 회전 베기, 반경 3m / 쿨타임 6초)")]
    [SerializeField] float _skillRadius = 3f;
    [SerializeField] float _skillCooldown = 6f;
    float _skillCooldownTimer;

    /// <summary>1=바로 사용 가능(꽉 참), 0=방금 씀. 쿨타임 진행에 따라 다시 채워짐. HUD 표시용</summary>
    public float SkillReadyRatio => _skillCooldownTimer > 0f ? 1f - (_skillCooldownTimer / _skillCooldown) : 1f;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.updateRotation = false;   // 회전은 마우스 조준 등 우리 코드가 직접 제어 (몬스터 접근/공격 로직과 충돌 방지)
        _statManager = GetComponentInChildren<PlayerStatManager>();
        _currentState = PlayerActionState.IDLE;
        CurrentHP = _maxHP;

        if (_attackHitbox != null)
            _attackHitbox.enabled = false;

        GameSession._instance.RegisterPlayer(this);
    }

    void OnDestroy()
    {
        if (GameSession._instance != null)
            GameSession._instance.UnregisterPlayer(this);
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
        // 상점 열려있는 동안엔 조작 자체를 막음 (시간은 안 멈추니 Idle 모션 등은 자연스럽게 계속 재생됨)
        if (GameSession._instance.IsShopOpen)
            return;

        // Hit/Death 중엔 Animation Event가 상태를 관리하므로 여기서 개입하지 않음
        if (_currentState == PlayerActionState.HIT || _currentState == PlayerActionState.DEATH)
            return;

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
    public void OnInventory(InputValue value)
    {
        if (!value.isPressed)
            return;
        if (IsDead)
            return;
        if (GameSession._instance.IsPerkSelectionOpen)
            return;
        if (GameSession._instance.IsShopOpen)
            return;
        GameSession._instance.ToggleInventory();
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
        float speed = _isRun ? _statManager.FinalRunSpeed : _statManager.FinalMoveSpeed;
        _agent.Move(_moveDir.normalized * speed * Time.deltaTime);
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
        if (GameSession._instance.IsShopOpen)
            return;

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
        _agent.Move(_rollDirection * _rollSpeed * Time.deltaTime);
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
        if (GameSession._instance.IsShopOpen)
            return;

        // 기획서 FSM 규칙: Attack은 Idle/Move에서만 진입 가능
        if (_currentState != PlayerActionState.IDLE && _currentState != PlayerActionState.MOVE)
            return;

        EnterAttack();
    }

    void EnterAttack()
    {
        Vector3 attackDir = GetMouseWorldDirection();
        transform.rotation = Quaternion.LookRotation(attackDir);

        ChangeActionState(PlayerActionState.ATTACK);

        CheckAttackTriggeredPerks();
    }

    /// <summary>마우스 스크린 좌표를 캐릭터 높이의 가상 바닥 평면에 투영해서 방향 계산</summary>
    Vector3 GetMouseWorldDirection()
    {
        if (Camera.main == null || Mouse.current == null)
            return transform.forward;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (!groundPlane.Raycast(ray, out float distance))
            return transform.forward;

        Vector3 hitPoint = ray.GetPoint(distance);
        Vector3 direction = hitPoint - transform.position;
        direction.y = 0f;

        return direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
    }

    /// <summary>
    /// 기본공격이 시작될 때마다 호출. "N타마다 발동" 타입(TriggerType==1)의 강화를 전부 확인해서
    /// 각자 자기 주기(TriggerValue)가 찼으면 검기를 발사한다. 강화별로 카운터를 따로 관리.
    /// </summary>
    void CheckAttackTriggeredPerks()
    {
        foreach (KeyValuePair<int, int> pair in GameSession._instance.ActivePerks)
        {
            PerkData perk = PerkManager._instance.Get(pair.Key);
            if (perk == null || perk.SpecialEffect == null)
                continue;

            if (perk.SpecialEffect.TriggerType != 1)   // 1 = 기본공격 N타마다
                continue;

            if (!_specialEffectAttackCounters.ContainsKey(perk.PerkID))
                _specialEffectAttackCounters[perk.PerkID] = 0;

            _specialEffectAttackCounters[perk.PerkID]++;

            int triggerEvery = Mathf.Max(1, perk.SpecialEffect.TriggerValue);
            if (_specialEffectAttackCounters[perk.PerkID] < triggerEvery)
                continue;

            _specialEffectAttackCounters[perk.PerkID] = 0;
            FireSwordWave(perk.SpecialEffect);
        }
    }

    void FireSwordWave(SpecialEffect effect)
    {
        if (_swordWavePrefab == null)
            return;

        SwordWaveProjectile wave = Instantiate(_swordWavePrefab, transform.position + transform.forward, transform.rotation);

        int damage = Mathf.RoundToInt(_statManager.FinalAttackPower * effect.DamagePercent / 100f);
        wave.Init(damage, effect.AreaRadius, _monsterLayer);
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
            // 최종 공격력 그대로 적용 (기본공격 계수 100%)
            target.TakeDamage(Mathf.RoundToInt(_statManager.FinalAttackPower));
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
        if (GameSession._instance.IsShopOpen)
            return;

        if (_skillCooldownTimer > 0f)
            return;

        // 기획서 FSM 규칙: Skill은 Idle/Move에서만 진입 가능
        if (_currentState != PlayerActionState.IDLE && _currentState != PlayerActionState.MOVE)
            return;

        EnterSkill();
    }

    void EnterSkill()
    {
        Vector3 skillDir = GetMouseWorldDirection();
        transform.rotation = Quaternion.LookRotation(skillDir);

        _skillCooldownTimer = _skillCooldown;
        ChangeActionState(PlayerActionState.SKILL);
    }

    /// <summary>회전 베기 판정 프레임에 Animation Event로 연결 - 반경 안 몬스터 전체를 즉시 조회</summary>
    public void OnSkillHitCheck()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _skillRadius, _monsterLayer);
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out IDamageable target))
            {
                // 검 스킬 계수 220%
                target.TakeDamage(Mathf.RoundToInt(_statManager.FinalAttackPower * 2.2f));
            }
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

    // ─────────────────────────────────────────────
    // IDamageable
    // ─────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        if (IsDead)
            return;

        if (IsInvincible)   // Roll 무적 구간이면 데미지 무시
            return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);

        ChangeActionState(IsDead ? PlayerActionState.DEATH : PlayerActionState.HIT);
    }

    /// <summary>피격 애니메이션이 끝나는 프레임에 Animation Event로 연결</summary>
    public void OnHitAnimationEnd()
    {
        if (IsDead)
            return;
        ChangeActionState(PlayerActionState.IDLE);
    }

    /// <summary>사망 애니메이션이 끝나는 프레임에 Animation Event로 연결</summary>
    public void OnDeathAnimationEnd()
    {
        // TODO: 게임 오버 처리 / 마을 복귀 등 (기획서 "반복되는 하루" 흐름과 연결 예정)
        Debug.Log("플레이어 사망 처리 필요 (TODO)");
    }
}