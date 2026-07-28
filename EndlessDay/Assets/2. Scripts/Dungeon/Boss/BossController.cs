using Defines;
using UnityEngine;

/// <summary>어떤 히트박스(공격)가 판정을 발생시켰는지 구분용 - 독액토하기는 투사체라 여기 없음(BossProjectile이 따로 처리)</summary>
public enum BossAttackType
{
    Melee,
    JumpSlam,
}

/// <summary>
/// 보스 몬스터. MonsterController의 이동/추적/피격/사망 FSM은 그대로 재사용하고,
/// "시작 상태"만 다르게 오버라이드 - 스폰 시엔 완전히 비활성(배회도 감지도 안 함) 상태로 대기하다가,
/// 플레이어가 보스방에 들어오는 순간(BossRoomTrigger가 호출) 활성화되어 바로 추적 시작.
/// 페이즈 전환(HP% 구간마다 공격력/속도 배율), 다중 공격 선택(SkillIndex 애니메이터 파라미터로 구분)을 여기서 처리.
/// ActionState는 계속 ATTACK 하나만 씀 - 어떤 공격인지는 SkillIndex로만 구분하고, Animator가 그 값 보고 분기.
/// </summary>
public class BossController : MonsterController
{
    [SerializeField] int _monsterID;   // MonsterTable 조회용 (예: 모르가스 = 3001)

    [Header("디버그 - 인스펙터 확인용 (플레이 중 값 실제로 채워지는지 확인)")]
    [SerializeField] int _debugBaseAttackDamage;
    [SerializeField] float _debugBaseChaseSpeed;
    [SerializeField] int _currentPhase = 1;
    [SerializeField] float _phase2Threshold, _phase2AtkMult, _phase2SpdMult;
    [SerializeField] float _phase3Threshold, _phase3AtkMult, _phase3SpdMult;

    int _baseAttackDamage;
    float _baseChaseSpeed;

    [Header("공격별 쿨타임 / 최소 페이즈 (SkillIndex: 0=근접, 1=점프내려찍기, 2=독액토하기)")]
    [SerializeField] float _meleeCooldown = 1.5f;
    [SerializeField] float _jumpSlamCooldown = 8f;
    [SerializeField] int _jumpSlamMinPhase = 1;
    [SerializeField] float _poisonVomitCooldown = 6f;
    [SerializeField] int _poisonVomitMinPhase = 2;

    [Header("공격별 사거리 (MonsterAttackTable에서 로드, 디버그로 확인 가능)")]
    [SerializeField] float _meleeRange;
    [SerializeField] float _jumpSlamRange;
    [SerializeField] float _poisonVomitRange;

    float _meleeCooldownTimer;
    float _jumpSlamCooldownTimer;
    float _poisonVomitCooldownTimer;

    [Header("공격별 히트박스 (평소 꺼짐, Animation Event로 켜고 끔) - 근접/점프내려찍기만 해당")]
    [SerializeField] Collider _meleeHitbox;
    [SerializeField] Collider _jumpSlamHitbox;

    [Header("독액토하기 - 투사체")]
    [SerializeField] BossProjectile _poisonProjectilePrefab;
    [SerializeField] Transform _poisonProjectileSpawnPoint;

    protected override void EnterInitialState()
    {
        _isActive = false;   // 활성화되기 전까진 아무 것도 안 함 (배회 X, 감지 X)

        _baseAttackDamage = _attackDamage;
        _baseChaseSpeed = _chaseSpeed;
        _debugBaseAttackDamage = _baseAttackDamage;
        _debugBaseChaseSpeed = _baseChaseSpeed;
        LoadPhaseData();
        LoadAttackRanges();
    }

    /// <summary>MonsterAttackTable에서 공격별 사거리 조회 (MonsterID+AttackType+SkillIndex로 행을 찾음)</summary>
    void LoadAttackRanges()
    {
        _meleeRange = FindAttackRange(1, 0);        // AttackType 1=근접
        _jumpSlamRange = FindAttackRange(2, 1);      // AttackType 2=스킬, SkillIndex 1
        _poisonVomitRange = FindAttackRange(2, 2);   // SkillIndex 2
    }

    float FindAttackRange(int attackType, int skillIndex)
    {
        DataTable attackTable = TableDataManager._instance.Get(TableName.MonsterAttackTable);

        foreach (int rowKey in attackTable.GetAllKeys())
        {
            if (attackTable.ToI(rowKey, "MonsterID") != _monsterID)
                continue;
            if (attackTable.ToI(rowKey, "AttackType") != attackType)
                continue;
            if (attackTable.ToI(rowKey, "SkillIndex") != skillIndex)
                continue;

            return attackTable.ToF(rowKey, "Range");
        }

        Debug.LogWarning("[BossController] 사거리 조회 실패 - MonsterID:" + _monsterID + " AttackType:" + attackType + " SkillIndex:" + skillIndex);
        return 0f;
    }

    void LoadPhaseData()
    {
        DataTable monsterTable = TableDataManager._instance.Get(TableName.MonsterTable);

        _phase2Threshold = monsterTable.ToF(_monsterID, "Phase2Threshold");
        _phase2AtkMult = monsterTable.ToF(_monsterID, "Phase2AtkMult");
        _phase2SpdMult = monsterTable.ToF(_monsterID, "Phase2SpdMult");

        _phase3Threshold = monsterTable.ToF(_monsterID, "Phase3Threshold");
        _phase3AtkMult = monsterTable.ToF(_monsterID, "Phase3AtkMult");
        _phase3SpdMult = monsterTable.ToF(_monsterID, "Phase3SpdMult");
    }

    protected override void Update()
    {
        base.Update();
        CheckPhaseTransition();
        UpdateBossCooldowns();
    }

    void UpdateBossCooldowns()
    {
        if (_meleeCooldownTimer > 0f) _meleeCooldownTimer -= Time.deltaTime;
        if (_jumpSlamCooldownTimer > 0f) _jumpSlamCooldownTimer -= Time.deltaTime;
        if (_poisonVomitCooldownTimer > 0f) _poisonVomitCooldownTimer -= Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    // 페이즈 전환
    // ─────────────────────────────────────────────

    void CheckPhaseTransition()
    {
        if (IsDead || MaxHP <= 0)
            return;

        float hpRatio = (float)CurrentHP / MaxHP;

        if (_currentPhase < 3 && hpRatio <= _phase3Threshold)
            EnterPhase(3, _phase3AtkMult, _phase3SpdMult);
        else if (_currentPhase < 2 && hpRatio <= _phase2Threshold)
            EnterPhase(2, _phase2AtkMult, _phase2SpdMult);
    }

    void EnterPhase(int phase, float atkMult, float spdMult)
    {
        _currentPhase = phase;

        _attackDamage = Mathf.RoundToInt(_baseAttackDamage * atkMult);
        _chaseSpeed = _baseChaseSpeed * spdMult;

        Debug.Log("[BossController] 페이즈 " + phase + " 진입 - 공격력 " + _attackDamage + ", 속도 " + _chaseSpeed);

        // TODO: 페이즈 전환 연출(포효 애니메이션, 화면 흔들림 등) 여기에 추가 가능
    }

    // ─────────────────────────────────────────────
    // 공격 선택 (MonsterController의 확장 지점 오버라이드)
    // ─────────────────────────────────────────────

    /// <summary>공격 후보 중 사거리가 제일 긴 것 기준으로 "공격권 안에 들어왔는지" 판단</summary>
    protected override bool IsPlayerInAttackRange()
    {
        if (_target == null)
            return false;

        float distance = Vector3.Distance(transform.position, _target.position);
        float maxRange = Mathf.Max(_meleeRange, Mathf.Max(_jumpSlamRange, _poisonVomitRange));
        return distance <= maxRange;
    }

    protected override bool IsAttackReady()
    {
        if (_target == null)
            return false;

        float distance = Vector3.Distance(transform.position, _target.position);

        bool meleeReady = _meleeCooldownTimer <= 0f && distance <= _meleeRange;
        bool jumpSlamReady = _jumpSlamCooldownTimer <= 0f && distance <= _jumpSlamRange && _currentPhase >= _jumpSlamMinPhase;
        bool poisonVomitReady = _poisonVomitCooldownTimer <= 0f && distance <= _poisonVomitRange && _currentPhase >= _poisonVomitMinPhase;

        return meleeReady || jumpSlamReady || poisonVomitReady;
    }

    /// <summary>우선순위: 점프내려찍기 > 독액토하기 > 근접. 쿨타임+페이즈+사거리 조건 다 맞는 것만 후보</summary>
    protected override void PerformAttack()
    {
        float distance = Vector3.Distance(transform.position, _target.position);

        if (_jumpSlamCooldownTimer <= 0f && distance <= _jumpSlamRange && _currentPhase >= _jumpSlamMinPhase)
        {
            EnterJumpSlam();
            return;
        }

        if (_poisonVomitCooldownTimer <= 0f && distance <= _poisonVomitRange && _currentPhase >= _poisonVomitMinPhase)
        {
            EnterPoisonVomit();
            return;
        }

        if (_meleeCooldownTimer <= 0f && distance <= _meleeRange)
            EnterMelee();

        // 셋 다 조건 안 맞으면(예: IsAttackReady 이후 미세하게 움직여서 사거리 벗어남) 아무 것도 안 함 - 다음 프레임에 다시 시도
    }

    void EnterMelee()
    {
        _meleeCooldownTimer = _meleeCooldown;
        _animator.SetInteger("SkillIndex", 0);
        EnterAttack();   // 부모의 ATTACK 상태/로직 그대로 재사용
    }

    void EnterJumpSlam()
    {
        _jumpSlamCooldownTimer = _jumpSlamCooldown;
        _animator.SetInteger("SkillIndex", 1);
        EnterAttack();
    }

    void EnterPoisonVomit()
    {
        _poisonVomitCooldownTimer = _poisonVomitCooldown;
        _animator.SetInteger("SkillIndex", 2);
        EnterAttack();
    }

    // ─────────────────────────────────────────────
    // Animation Event 콜백 (히트박스 on/off) - 공격 끝나는 시점은 부모의 OnAttackAnimationEnd() 그대로 재사용
    // ─────────────────────────────────────────────

    public void OnMeleeHitboxStart() { if (_meleeHitbox != null) _meleeHitbox.enabled = true; }
    public void OnMeleeHitboxEnd() { if (_meleeHitbox != null) _meleeHitbox.enabled = false; }

    public void OnJumpSlamHitboxStart() { if (_jumpSlamHitbox != null) _jumpSlamHitbox.enabled = true; }
    public void OnJumpSlamHitboxEnd() { if (_jumpSlamHitbox != null) _jumpSlamHitbox.enabled = false; }

    /// <summary>독액토하기 애니메이션의 발사 프레임에 Animation Event로 연결</summary>
    public void OnPoisonVomitFire()
    {
        if (_poisonProjectilePrefab == null)
            return;

        Vector3 spawnPosition = _poisonProjectileSpawnPoint != null ? _poisonProjectileSpawnPoint.position : transform.position;
        Quaternion spawnRotation = _poisonProjectileSpawnPoint != null ? _poisonProjectileSpawnPoint.rotation : transform.rotation;

        BossProjectile projectile = Instantiate(_poisonProjectilePrefab, spawnPosition, spawnRotation);
        projectile.Init(_attackDamage);
    }

    /// <summary>BossAttackHitboxTrigger가 트리거 감지 시 호출 - 어떤 공격이든 지금 페이즈 공격력 그대로 적용</summary>
    public void OnAttackHitboxTriggerEnter(BossAttackType attackType, Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(_attackDamage);
            DamagePopupSpawner._instance?.Spawn(target.DamagePopupPosition, _attackDamage, false, true);
        }
    }

    /// <summary>플레이어가 보스방 트리거에 들어오는 순간 호출 - 그제서야 추적 시작</summary>
    public void ActivateBoss()
    {
        _isActive = true;
        _isPlayerDetected = true;
    }
}