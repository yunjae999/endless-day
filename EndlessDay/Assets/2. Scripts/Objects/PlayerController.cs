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
    [SerializeField] int _maxHP = 100;

    public int CurrentHP { get; private set; }
    public int MaxHP => _statManager != null ? Mathf.RoundToInt(_statManager.FinalMaxHP) : _maxHP;

    [SerializeField] Vector3 _damagePopupOffset = Vector3.up;
    public Vector3 DamagePopupPosition => transform.position + _damagePopupOffset;
    public bool IsDead => CurrentHP <= 0;

    [SerializeField] GameObject _shadowObject;

    /// <summary>생성 직후 스포너가 호출 - 씬에 따라 그림자 표시 여부를 결정</summary>
    public void SetShadowVisible(bool visible)
    {
        if (_shadowObject != null)
            _shadowObject.SetActive(visible);
    }

    /// <summary>레벨업/강화 등으로 체력을 회복시킬 때 호출. 최대체력을 넘지 않게 보정</summary>
    public void Heal(int amount)
    {
        if (IsDead)
            return;

        CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        _healthBar?.ShowTemporarily();
    }

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

    [Header("기본공격/스킬 VFX")]
    [SerializeField] GameObject _basicAttackVFXPrefab;
    [SerializeField] Transform _attackVFXPoint;   // Player 프리팹 자식으로 직접 배치해서 위치/회전 눈으로 맞추기
    [SerializeField] GameObject _skillVFXPrefab;
    [SerializeField] Transform _skillVFXPoint;

    [Header("강화 특수효과 (검기 등)")]
    [SerializeField] SwordWaveProjectile _swordWavePrefab;
    [SerializeField] Vector3 _swordWaveSpawnOffset = new Vector3(0f, 0f, 1f);   // 캐릭터 기준 로컬 오프셋 (오른쪽, 위쪽, 앞쪽)
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

        if (_attackHitbox != null)
            _attackHitbox.enabled = false;

        GameSession._instance.RegisterPlayer(this);
    }

    void OnDestroy()
    {
        if (GameSession._instance != null)
            GameSession._instance.UnregisterPlayer(this);
    }

    [Header("머리 위 체력바 (자식 오브젝트)")]
    [SerializeField] UIWorldHealthBar _healthBar;

    void Start()
    {
        _statManager.InitBaseStats();
        CurrentHP = Mathf.RoundToInt(_statManager.FinalMaxHP);   // 스탯(강화/장비 반영된 최종값) 기준으로 꽉 채워서 시작

        if (_healthBar != null)
            _healthBar.Init(GameSession._instance.Nickname, this);
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
        if (IsPointerOverUI())
            return;
        TryStartAttack();
    }
    public void OnSkill(InputValue value)
    {
        if (!value.isPressed)
            return;
        if (IsPointerOverUI())
            return;
        TryStartSkill();
    }

    /// <summary>마우스가 UI(강화 카드, 버튼 등) 위에 있는지 - 클릭이 UI로 소비됐으면 공격/스킬로 안 새어나가게</summary>
    bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
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
        if (GameSession._instance.IsResultShown)
            return;
        if (GameSession._instance.IsPauseMenuOpen)
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

        if (_animator != null)
            _animator.speed = _statManager.AttackAnimatorSpeedMultiplier;   // 빨리 끝날수록 다음 공격도 그만큼 빨리 가능해짐

        _healthBar?.ShowTemporarily();
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

        Vector3 spawnPosition = transform.TransformPoint(_swordWaveSpawnOffset);   // 캐릭터 회전 반영한 위치
        SwordWaveProjectile wave = Instantiate(_swordWavePrefab, spawnPosition, transform.rotation);

        (int damage, bool isCrit) = CalculateDamage(effect.DamagePercent / 100f);
        wave.Init(damage, effect.AreaRadius, _monsterLayer, isCrit);
    }

    public void OnAttackHitboxStart()
    {
        _alreadyHit.Clear();   // 이번 공격에서 맞은 대상 기록 초기화
        if (_attackHitbox != null)
            _attackHitbox.enabled = true;

        SpawnVFX(_basicAttackVFXPrefab, _attackVFXPoint);
        CheckAttackTriggeredPerks();   // 무기가 실제로 휘둘러지는 순간에 맞춰 검기 판정
    }

    /// <summary>point가 있으면 그 위치/회전 그대로, 없으면 캐릭터 위치/회전으로 대체(안전장치)</summary>
    void SpawnVFX(GameObject prefab, Transform point)
    {
        if (prefab == null)
            return;

        Vector3 position = point != null ? point.position : transform.position;
        Quaternion rotation = point != null ? point.rotation : transform.rotation;

        Instantiate(prefab, position, rotation);
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
            (int damage, bool isCrit) = CalculateDamage(1f);   // 기본공격 계수 100%
            int actualDamage = target.TakeDamage(damage);
            if (actualDamage > 0)
                DamagePopupSpawner._instance?.Spawn(target.DamagePopupPosition, actualDamage, isCrit, false);
        }
    }

    /// <summary>치명타 판정 포함 데미지 계산. attackMultiplier: 공격력 대비 계수 (기본공격 1.0, 스킬 2.2 등)</summary>
    (int damage, bool isCrit) CalculateDamage(float attackMultiplier)
    {
        bool isCrit = Random.Range(0f, 100f) < _statManager.FinalCritChance;
        float damage = _statManager.FinalAttackPower * attackMultiplier;

        if (isCrit)
            damage *= _statManager.FinalCritDamage / 100f;   // CritDamage는 총 배율(%) - 150이면 150%

        return (Mathf.RoundToInt(damage), isCrit);
    }

    public void OnAttackAnimationEnd()
    {
        if (_animator != null)
            _animator.speed = 1f;   // 공격 중에만 빨라져야 하니 반드시 원상복귀

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

        _healthBar?.ShowTemporarily();
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
                (int damage, bool isCrit) = CalculateDamage(2.2f);
                int actualDamage = target.TakeDamage(damage);
                if (actualDamage > 0)
                    DamagePopupSpawner._instance?.Spawn(target.DamagePopupPosition, actualDamage, isCrit, false);
            }
        }

        SpawnVFX(_skillVFXPrefab, _skillVFXPoint);
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

    public int TakeDamage(int amount)
    {
        if (IsDead)
            return 0;

        if (IsInvincible)   // Roll 무적 구간이면 데미지 무시
            return 0;

        int reducedAmount = ApplyDefense(amount);
        CurrentHP = Mathf.Max(0, CurrentHP - reducedAmount);

        bool canBeInterrupted = _currentState == PlayerActionState.IDLE || _currentState == PlayerActionState.MOVE;

        if (IsDead)
        {
            if (_animator != null)
                _animator.speed = 1f;   // 공격 중 사망으로 끊길 수 있으니 안전하게 복귀
            ChangeActionState(PlayerActionState.DEATH);
        }
        else if (canBeInterrupted)
        {
            ChangeActionState(PlayerActionState.HIT);
        }
        // else: 공격/스킬 진행 중이면 데미지만 적용하고 애니메이션은 안 끊음

        DamagePopupSpawner._instance?.Spawn(DamagePopupPosition, reducedAmount, false, true);

        if (IsDead)
            _healthBar?.Hide();
        else
            _healthBar?.ShowTemporarily();

        return reducedAmount;
    }

    /// <summary>방어력만큼 받는 데미지를 비율로 감소. 100 방어력 = 데미지 절반, 무한대로 갈수록 0에 수렴 (곱연산 스탯과 자연스럽게 어울리는 공식)</summary>
    int ApplyDefense(int rawDamage)
    {
        float defense = _statManager.FinalDefense;
        float reduced = rawDamage * (100f / (100f + defense));
        return Mathf.Max(1, Mathf.RoundToInt(reduced));   // 최소 1은 들어가게 (방어력으로 완전 무적 방지)
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
        if (UIResultController._instance != null)
            UIResultController._instance.Show(false);
        else
            Debug.LogWarning("[PlayerController] 결과창 UI가 등록되어 있지 않음 (씬에 배치됐는지 확인)");
    }
}