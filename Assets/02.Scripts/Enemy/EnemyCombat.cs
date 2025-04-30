using UnityEngine;
using System.Collections;

/// <summary>
/// 적 캐릭터의 전투 관련 기능을 관리하는 클래스입니다.
/// </summary>
public class EnemyCombat : MonoBehaviour
{
    [Header("전투 설정")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private int _attackDamage = 15;
    [SerializeField] private float _attackRadius = 1.2f;

    [Header("공격 효과")]
    [SerializeField] private AudioClip _attackSound;
    [SerializeField] private GameObject _attackEffectPrefab;
    [SerializeField] private Transform _attackEffectPoint;

    /// <summary>
    /// 현재 체력
    /// </summary>
    private int _currentHealth;

    /// <summary>
    /// 공격 쿨다운 타이머
    /// </summary>
    private float _attackCooldownTimer = 0f;

    /// <summary>
    /// 사망 여부
    /// </summary>
    private bool _isDead = false;

    /// <summary>
    /// 공격 중 여부
    /// </summary>
    private bool _isAttacking = false;

    /// <summary>
    /// Enemy 컴포넌트 참조
    /// </summary>
    private Enemy _enemy;

    /// <summary>
    /// EnemyEffects 컴포넌트 참조
    /// </summary>
    private EnemyEffects _enemyEffects;

    /// <summary>
    /// AudioSource 컴포넌트 참조
    /// </summary>
    private AudioSource _audioSource;

    /// <summary>
    /// Animator 컴포넌트 참조
    /// </summary>
    private Animator _animator;

    #region 이벤트
    /// <summary>
    /// 체력 변경 시 발생하는 이벤트입니다.
    /// </summary>
    /// <param name="currentHealth">현재 체력</param>
    /// <param name="maxHealth">최대 체력</param>
    public delegate void HealthChangedHandler(float currentHealth, float maxHealth);

    /// <summary>
    /// 체력이 변경될 때 발생하는 이벤트
    /// </summary>
    public event HealthChangedHandler OnHealthChanged;
    #endregion

    #region Unity 이벤트 함수
    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _enemyEffects = GetComponent<EnemyEffects>();
        _audioSource = GetComponent<AudioSource>();
        _animator = GetComponent<Animator>();
        _currentHealth = _maxHealth;

        // AudioSource가 없으면 추가
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1f; // 3D 사운드 설정
        }

        // 공격 이펙트 포인트가 없으면 생성
        if (_attackEffectPoint == null)
        {
            GameObject effectPoint = new GameObject("AttackEffectPoint");
            effectPoint.transform.SetParent(transform);
            effectPoint.transform.localPosition = new Vector3(0, 1f, 0.5f); // 적절한 위치 조정
            _attackEffectPoint = effectPoint.transform;
        }
    }

    private void Update()
    {
        if (_enemy.GetComponent<EnemyStateMachine>().CurrentStateType == EnemyState.Attack)
        {
            _attackCooldownTimer += Time.deltaTime;

            // 공격 가능한 상태이고 플레이어가 공격 범위 내에 있으면 공격 시도
            if (CanAttack && IsPlayerInAttackRange())
            {
                TryAttack(_enemy.Player);
            }
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 데미지를 처리합니다.
    /// </summary>
    public void TakeDamage(Damage damage)
    {
        if (_isDead || _enemyEffects.IsDamaged) return;

        _currentHealth -= damage.Amount;

        // 개발 빌드에서만 로그 출력
        if (Debug.isDebugBuild)
        {
            Debug.Log($"적 피격! 체력: {_currentHealth}/{_maxHealth}");
        }

        // 체력 변경 이벤트 발생
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        // 넉백 효과 적용
        _enemyEffects.ApplyDamageEffect(damage.Source.transform.position);

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            _isDead = true;
            _enemy.SetState(EnemyState.Die);
        }
    }

    /// <summary>
    /// 공격을 시도합니다.
    /// </summary>
    /// <param name="target">공격 대상 게임 오브젝트</param>
    public void TryAttack(GameObject target)
    {
        if (_attackCooldownTimer >= _attackCooldown && !_isAttacking)
        {
            _attackCooldownTimer = 0f;
            StartCoroutine(PerformAttack(target));
        }
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 플레이어가 공격 범위 내에 있는지 확인합니다.
    /// </summary>
    /// <returns>플레이어가 공격 범위 내에 있으면 true, 그렇지 않으면 false</returns>
    private bool IsPlayerInAttackRange()
    {
        if (_enemy.Player == null) return false;

        // Enemy 클래스의 IsTargetInSight 메서드를 사용하여 플레이어가 시야 내에 있는지 확인
        bool isInSight = _enemy.IsTargetInSight(
            _enemy.Player.transform.position,
            360f, // 공격 상태에서는 전방위 감지
            _attackRadius
        );

        // 거리 체크
        float distanceToPlayer = Vector3.Distance(transform.position, _enemy.Player.transform.position);

        return isInSight && distanceToPlayer <= _attackRadius;
    }

    /// <summary>
    /// 실제 공격을 수행하는 코루틴
    /// </summary>
    private IEnumerator PerformAttack(GameObject target)
    {
        _isAttacking = true;

        // 타겟 방향으로 회전
        if (target != null)
        {
            _enemy.RotateToTarget(target.transform.position);
        }

        // 공격 애니메이션 재생 (있다면)
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");

            // 애니메이션 이벤트를 기다리는 대신, 일정 시간 후 공격 판정
            yield return new WaitForSeconds(0.3f); // 공격 판정 타이밍 조정
        }

        // 공격 사운드 재생
        if (_audioSource != null && _attackSound != null)
        {
            _audioSource.PlayOneShot(_attackSound);
        }

        // 공격 이펙트 생성
        if (_attackEffectPrefab != null && _attackEffectPoint != null)
        {
            GameObject effect = Instantiate(_attackEffectPrefab, _attackEffectPoint.position, _attackEffectPoint.rotation);
            Destroy(effect, 2f); // 2초 후 이펙트 제거
        }

        // 공격 시점에서 플레이어가 범위 내에 있는지 다시 확인
        if (target != null && IsPlayerInAttackRange())
        {
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // 데미지 객체 생성
                Damage damage = new Damage
                {
                    Amount = _attackDamage,
                    Source = gameObject
                };

                // 데미지 적용
                damageable.TakeDamage(damage);
            }
        }

        // 공격 마무리 시간
        yield return new WaitForSeconds(0.5f);

        _isAttacking = false;

        // 공격 완료 후 상태 처리
        if (_enemy != null)
        {
            _enemy.CompleteAttack();
        }
    }

    /// <summary>
    /// 공격 범위를 시각적으로 표시 (디버그용)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRadius);
    }
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 적이 사망 상태인지 여부
    /// </summary>
    public bool IsDead => _isDead;

    /// <summary>
    /// 공격이 가능한 상태인지 여부
    /// </summary>
    public bool CanAttack => _attackCooldownTimer >= _attackCooldown && !_isAttacking;

    /// <summary>
    /// 공격 쿨다운 시간
    /// </summary>
    public float AttackCooldown => _attackCooldown;

    /// <summary>
    /// 현재 공격 중인지 여부
    /// </summary>
    public bool IsAttacking => _isAttacking;

    /// <summary>
    /// 최대 체력
    /// </summary>
    public int MaxHealth => _maxHealth;

    /// <summary>
    /// 현재 체력
    /// </summary>
    public int CurrentHealth => _currentHealth;
    #endregion
}