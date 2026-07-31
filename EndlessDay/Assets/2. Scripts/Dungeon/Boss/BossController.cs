using Defines;
using System.Collections.Generic;
using UnityEngine;

/// <summary>어떤 히트박스(공격)가 판정을 발생시켰는지 구분용 - 독액토하기는 투사체라 여기 없음(BossProjectile이 따로 처리)</summary>
public enum BossAttackType
{
    Melee,
    JumpSlam,
}

/// <summary>공격 하나(근접/점프내려찍기/독액토하기)의 MonsterAttackTable 데이터를 담는 구조체</summary>
public struct BossAttackData
{
    public float range;
    public float cooldown;
    public int minPhase;
    public float damagePercent;
}

/// <summary>
/// 보스 몬스터. MonsterController의 이동/추적/피격/사망 FSM은 그대로 재사용하고,
/// "시작 상태"만 다르게 오버라이드 - 스폰 시엔 완전히 비활성(배회도 감지도 안 함) 상태로 대기하다가,
/// 플레이어가 보스방에 들어오는 순간(BossRoomTrigger가 호출) 활성화되어 바로 추적 시작.
/// 페이즈 전환(HP% 구간마다 공격력/속도 배율), 다중 공격 선택(SkillIndex 애니메이터 파라미터로 구분)을 여기서 처리.
/// 공격별 사거리/쿨타임/페이즈제한/데미지%는 전부 MonsterAttackTable에서 로드 - 인스펙터엔 안 남음.
/// ActionState는 계속 ATTACK 하나만 씀 - 어떤 공격인지는 SkillIndex로만 구분하고, Animator가 그 값 보고 분기.
/// </summary>
public class BossController : MonsterController
{
    [Header("디버그 - 인스펙터 확인용 (플레이 중 값 실제로 채워지는지 확인)")]
    [SerializeField] int _debugBaseAttackDamage;
    [SerializeField] float _debugBaseChaseSpeed;
    [SerializeField] int _currentPhase = 1;
    [SerializeField] float _phase2Threshold, _phase2AtkMult, _phase2SpdMult;
    [SerializeField] float _phase3Threshold, _phase3AtkMult, _phase3SpdMult;

    int _baseAttackDamage;
    float _baseChaseSpeed;

    [Header("공격별 데이터 (MonsterAttackTable에서 로드, 디버그로 확인용)")]
    [SerializeField] BossAttackData _meleeData;
    [SerializeField] BossAttackData _jumpSlamData;
    [SerializeField] BossAttackData _poisonVomitData;

    float _meleeCooldownTimer;
    float _jumpSlamCooldownTimer;
    float _poisonVomitCooldownTimer;

    [Header("공격별 히트박스 (평소 꺼짐, Animation Event로 켜고 끔) - 근접/점프내려찍기만 해당")]
    [SerializeField] Collider _meleeHitbox;
    [SerializeField] Collider _jumpSlamHitbox;

    [Header("독액토하기 - 투사체")]
    [SerializeField] BossProjectile _poisonProjectilePrefab;
    [SerializeField] Transform _poisonProjectileSpawnPoint;

    [Header("근접/점프내려찍기 범위 경고 표시")]
    [Header("근접/점프내려찍기 범위 표시 - 실제 히트박스 콜라이더 모양 그대로 보여줌")]
    [SerializeField] ColliderVisualizer _meleeHitboxVisualizer;
    [SerializeField] ColliderVisualizer _jumpSlamHitboxVisualizer;

    [Header("좀비 소환 (SummonTable에서 로드)")]
    [SerializeField] float _summonRadius = 3f;   // 보스 주변 랜덤 반경
    int _summonMonsterID;
    int _summonCount;
    float _summonCooldown;
    int _summonMinPhase;
    float _summonCooldownTimer;
    bool _pendingSummon;   // 3페이즈 진입 시 true, 다음 공격 기회에 우선 소환하고 다시 false로

    const float ACTIVATION_GRACE_DURATION = 1f;   // 활성화 직후 이 시간 동안은 사거리 안이어도 공격 안 함
    float _activationGraceTimer;
    List<MonsterController> _summonedMonsters = new List<MonsterController>();

    protected override void EnterInitialState()
    {
        _isActive = false;   // 활성화되기 전까진 아무 것도 안 함 (배회 X, 감지 X)

        if (_meleeHitbox != null)
            _meleeHitbox.enabled = false;
        if (_jumpSlamHitbox != null)
            _jumpSlamHitbox.enabled = false;

        _baseAttackDamage = _attackDamage;
        _baseChaseSpeed = _chaseSpeed;
        _debugBaseAttackDamage = _baseAttackDamage;
        _debugBaseChaseSpeed = _baseChaseSpeed;

        LoadPhaseData();
        LoadAttackData();
        LoadSummonData();
    }

    // ─────────────────────────────────────────────
    // 데이터 로드
    // ─────────────────────────────────────────────

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

    void LoadAttackData()
    {
        _meleeData = FindAttackData(1, 0);        // AttackType 1=근접
        _jumpSlamData = FindAttackData(2, 1);      // AttackType 2=스킬, SkillIndex 1
        _poisonVomitData = FindAttackData(2, 2);   // SkillIndex 2
    }

    BossAttackData FindAttackData(int attackType, int skillIndex)
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

            int phaseID = attackTable.ToI(rowKey, "PhaseID");

            return new BossAttackData
            {
                range = attackTable.ToF(rowKey, "Range"),
                cooldown = attackTable.ToF(rowKey, "Cooldown"),
                minPhase = phaseID <= 0 ? 1 : phaseID,   // PhaseID 0 = 제한 없음(1페이즈부터 가능)
                damagePercent = attackTable.ToF(rowKey, "DamagePercent"),
            };
        }

        Debug.LogWarning("[BossController] 공격 데이터 조회 실패 - MonsterID:" + _monsterID + " AttackType:" + attackType + " SkillIndex:" + skillIndex);
        return default;
    }

    void LoadSummonData()
    {
        DataTable summonTable = TableDataManager._instance.Get(TableName.SummonTable);
        DataTable attackTable = TableDataManager._instance.Get(TableName.MonsterAttackTable);

        // 소환 스킬(AttackType 2, SkillIndex 3)의 쿨타임/페이즈 제한 먼저 조회
        BossAttackData summonAttackData = FindAttackData(2, 3);
        _summonCooldown = summonAttackData.cooldown;
        _summonMinPhase = summonAttackData.minPhase;

        // 실제로 뭘 몇 마리 소환할지는 SummonTable에서
        foreach (int rowKey in summonTable.GetAllKeys())
        {
            if (summonTable.ToI(rowKey, "MonsterID") != _monsterID)
                continue;
            if (summonTable.ToI(rowKey, "SkillIndex") != 3)
                continue;

            _summonMonsterID = summonTable.ToI(rowKey, "SummonMonsterID");
            _summonCount = summonTable.ToI(rowKey, "SummonCount");
            return;
        }

        Debug.LogWarning("[BossController] 소환 데이터 조회 실패 - MonsterID:" + _monsterID);
    }

    // ─────────────────────────────────────────────
    // 매 프레임
    // ─────────────────────────────────────────────

    protected override void Update()
    {
        base.Update();
        CheckPhaseTransition();
        UpdateBossCooldowns();
    }

    void UpdateBossCooldowns()
    {
        if (_meleeCooldownTimer > 0f)
            _meleeCooldownTimer -= Time.deltaTime;
        if (_jumpSlamCooldownTimer > 0f)
            _jumpSlamCooldownTimer -= Time.deltaTime;
        if (_poisonVomitCooldownTimer > 0f)
            _poisonVomitCooldownTimer -= Time.deltaTime;
        if (_summonCooldownTimer > 0f)
            _summonCooldownTimer -= Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    // 페이즈 전환
    // ─────────────────────────────────────────────

    void CheckPhaseTransition()
    {
        if (IsDead || MaxHP <= 0)
            return;

        float hpRatio = (float)CurrentHP / MaxHP * 100;

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

        if (phase == 3)
            _pendingSummon = true;   // 다음 공격 기회에 우선적으로 소환

        Debug.Log("[BossController] 페이즈 " + phase + " 진입 - 공격력 " + _attackDamage + ", 속도 " + _chaseSpeed);

        // TODO: 페이즈 전환 연출(포효 애니메이션, 화면 흔들림 등) 여기에 추가 가능
    }

    // ─────────────────────────────────────────────
    // 공격 선택 (MonsterController의 확장 지점 오버라이드)
    // ─────────────────────────────────────────────

    /// <summary>공격 후보 중 사거리가 제일 긴 것 기준으로 "공격권 안에 들어왔는지" 판단. 소환은 거리 무관이라 별도 처리</summary>
    protected override bool IsPlayerInAttackRange()
    {
        if (_pendingSummon && _summonCooldownTimer <= 0f && _currentPhase >= _summonMinPhase)
            return true;

        if (_target == null)
            return false;

        float distance = Vector3.Distance(transform.position, _target.position);
        float maxRange = Mathf.Max(_meleeData.range, Mathf.Max(_jumpSlamData.range, _poisonVomitData.range));
        return distance <= maxRange;
    }

    /// <summary>보스는 사거리 안이어도 준비된 공격이 없으면 멈추지 않고 계속 접근 - 준비되는 순간 끼어들어서 공격</summary>
    protected override bool ShouldStopWhenInRange()
    {
        return false;
    }

    protected override bool IsAttackReady()
    {
        bool summonReady = _pendingSummon && _summonCooldownTimer <= 0f && _currentPhase >= _summonMinPhase;
        if (summonReady)
            return true;

        if (_target == null)
            return false;

        float distance = Vector3.Distance(transform.position, _target.position);

        bool meleeReady = _meleeCooldownTimer <= 0f && distance <= _meleeData.range;
        bool jumpSlamReady = _jumpSlamCooldownTimer <= 0f && distance <= _jumpSlamData.range && _currentPhase >= _jumpSlamData.minPhase;
        bool poisonVomitReady = _poisonVomitCooldownTimer <= 0f && distance <= _poisonVomitData.range && _currentPhase >= _poisonVomitData.minPhase;

        return meleeReady || jumpSlamReady || poisonVomitReady;
    }

    /// <summary>우선순위: 소환(대기 중이면 최우선) > 점프내려찍기 > 독액토하기 > 근접</summary>
    protected override void PerformAttack()
    {
        if (_pendingSummon && _summonCooldownTimer <= 0f && _currentPhase >= _summonMinPhase)
        {
            EnterSummon();
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.position);

        if (_jumpSlamCooldownTimer <= 0f && distance <= _jumpSlamData.range && _currentPhase >= _jumpSlamData.minPhase)
        {
            EnterJumpSlam();
            return;
        }

        if (_poisonVomitCooldownTimer <= 0f && distance <= _poisonVomitData.range && _currentPhase >= _poisonVomitData.minPhase)
        {
            EnterPoisonVomit();
            return;
        }

        if (_meleeCooldownTimer <= 0f && distance <= _meleeData.range)
            EnterMelee();

        // 아무 조건도 안 맞으면 다음 프레임에 다시 시도
    }

    int _lastUsedSkillIndex = -1;   // 공격이 끝나는 시점에 이 값 기준으로 쿨타임을 리셋함

    void EnterMelee()
    {
        _lastUsedSkillIndex = 0;
        _animator.SetInteger("SkillIndex", 0);
        EnterAttack();   // 부모의 ATTACK 상태/로직 그대로 재사용

        if (_meleeHitboxVisualizer != null)
            _meleeHitboxVisualizer.Show();
    }

    void EnterJumpSlam()
    {
        _lastUsedSkillIndex = 1;
        _animator.SetInteger("SkillIndex", 1);
        EnterAttack();

        if (_jumpSlamHitboxVisualizer != null)
            _jumpSlamHitboxVisualizer.Show();
    }

    void EnterPoisonVomit()
    {
        _lastUsedSkillIndex = 2;
        _animator.SetInteger("SkillIndex", 2);
        EnterAttack();
    }

    void EnterSummon()
    {
        _lastUsedSkillIndex = 3;
        _animator.SetInteger("SkillIndex", 3);
        EnterAttack();
    }

    /// <summary>공격이 실제로 끝나는 시점에 그 스킬의 쿨타임을 여기서 리셋 - "쓰기 시작할 때"가 아니라 "다 쓰고 나서"부터 쿨타임이 돌게 하기 위함</summary>
    public override void OnAttackAnimationEnd()
    {
        if (_meleeHitboxVisualizer != null)
            _meleeHitboxVisualizer.Hide();
        if (_jumpSlamHitboxVisualizer != null)
            _jumpSlamHitboxVisualizer.Hide();

        switch (_lastUsedSkillIndex)
        {
            case 0:
                _meleeCooldownTimer = _meleeData.cooldown;
                break;
            case 1:
                _jumpSlamCooldownTimer = _jumpSlamData.cooldown;
                break;
            case 2:
                _poisonVomitCooldownTimer = _poisonVomitData.cooldown;
                break;
            case 3:
                _summonCooldownTimer = _summonCooldown;
                _pendingSummon = false;
                break;
        }

        base.OnAttackAnimationEnd();   // 기존 EnterAttackIdle() 로직 그대로 이어서 실행
    }

    // ─────────────────────────────────────────────
    // Animation Event 콜백 (히트박스 on/off) - 공격 끝나는 시점은 부모의 OnAttackAnimationEnd() 그대로 재사용
    // ─────────────────────────────────────────────

    public void OnMeleeHitboxStart()
    {
        _alreadyHitThisAttack.Clear();
        if (_meleeHitbox != null)
            _meleeHitbox.enabled = true;
    }

    public void OnMeleeHitboxEnd()
    {
        if (_meleeHitbox != null)
            _meleeHitbox.enabled = false;
    }

    public void OnJumpSlamHitboxStart()
    {
        _alreadyHitThisAttack.Clear();
        if (_jumpSlamHitbox != null)
            _jumpSlamHitbox.enabled = true;
    }

    public void OnJumpSlamHitboxEnd()
    {
        if (_jumpSlamHitbox != null)
            _jumpSlamHitbox.enabled = false;
    }

    /// <summary>독액토하기 애니메이션의 발사 프레임에 Animation Event로 연결</summary>
    public void OnPoisonVomitFire()
    {
        if (_poisonProjectilePrefab == null)
            return;

        Vector3 spawnPosition = _poisonProjectileSpawnPoint != null ? _poisonProjectileSpawnPoint.position : transform.position;
        Quaternion spawnRotation = _poisonProjectileSpawnPoint != null ? _poisonProjectileSpawnPoint.rotation : transform.rotation;

        int damage = Mathf.RoundToInt(_attackDamage * _poisonVomitData.damagePercent / 100f);

        BossProjectile projectile = Instantiate(_poisonProjectilePrefab, spawnPosition, spawnRotation);
        projectile.Init(damage);
    }

    /// <summary>좀비 소환 애니메이션의 소환 프레임에 Animation Event로 연결</summary>
    public void OnSummonTrigger()
    {
        Debug.Log("[BossController] OnSummonTrigger 호출됨 - _summonMonsterID:" + _summonMonsterID + " _summonCount:" + _summonCount);   // 임시

        DataTable monsterTable = TableDataManager._instance.Get(TableName.MonsterTable);
        string prefabPath = monsterTable.ToS(_summonMonsterID, "PrefabPath");
        GameObject prefab = Resources.Load<GameObject>(prefabPath);

        Debug.Log("[BossController] prefabPath:" + prefabPath + " prefab 찾음? " + (prefab != null));   // 임시

        if (prefab == null)
        {
            Debug.LogWarning("[BossController] 소환할 몬스터 프리팹을 찾을 수 없음 : " + prefabPath);
            return;
        }

        for (int i = 0; i < _summonCount; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * _summonRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);

            GameObject summonedObject = Instantiate(prefab, spawnPosition, Quaternion.identity);

            if (summonedObject.TryGetComponent<MonsterController>(out MonsterController summoned))
                _summonedMonsters.Add(summoned);
        }
    }

    HashSet<Collider> _alreadyHitThisAttack = new HashSet<Collider>();

    /// <summary>BossAttackHitboxTrigger가 트리거 감지 시 호출. 근접/점프내려찍기 각자의 DamagePercent 적용.
    /// OnTriggerEnter/Stay 둘 다에서 불리므로, 이번 공격에서 이미 맞춘 대상은 걸러서 중복 데미지 방지</summary>
    public void OnAttackHitboxTriggerEnter(BossAttackType attackType, Collider other)
    {
        if (_alreadyHitThisAttack.Contains(other))
            return;

        // TryGetComponent는 other 자기 자신에게만 있는 컴포넌트만 찾음 - 콜라이더가 자식에 있고
        // IDamageable(PlayerController)은 루트에 있는 구조일 수 있어 GetComponentInParent로 안전하게 탐색
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null)
            return;

        _alreadyHitThisAttack.Add(other);

        float damagePercent = attackType == BossAttackType.Melee ? _meleeData.damagePercent : _jumpSlamData.damagePercent;
        int damage = Mathf.RoundToInt(_attackDamage * damagePercent / 100f);

        int actualDamage = target.TakeDamage(damage);
        if (actualDamage > 0)
            DamagePopupSpawner._instance?.Spawn(target.DamagePopupPosition, actualDamage, false, true);
    }

    /// <summary>플레이어가 보스방 트리거에 들어오는 순간 호출 - 그제서야 추적 시작</summary>
    public void ActivateBoss()
    {
        _isActive = true;
        _isPlayerDetected = true;
        _activationGraceTimer = ACTIVATION_GRACE_DURATION;   // 이 시간 동안은 사거리 안이어도 공격 안 함
        EnterChase();   // 활성화되자마자 이미 사거리 안이어서 곧장 공격으로 넘어가는 것 방지
    }

    /// <summary>보스가 죽으면 소환했던 좀비도 전부 같이 죽음 (정상적인 죽음 처리 그대로 태워서 보상도 지급)</summary>
    public override void OnDeathAnimationEnd()
    {
        foreach (MonsterController summoned in _summonedMonsters)
        {
            if (summoned != null)
                summoned.TakeDamage(int.MaxValue);
        }
        _summonedMonsters.Clear();

        base.OnDeathAnimationEnd();
    }
}