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

    private int _currentHealth;
    private float _attackCooldownTimer = 0f;
    private bool _isDead = false;

    private Enemy _enemy;
    private EnemyEffects _enemyEffects;

    #region Unity 이벤트 함수
    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _enemyEffects = GetComponent<EnemyEffects>();
        _currentHealth = _maxHealth;
    }

    private void Update()
    {
        if (_enemy.GetComponent<EnemyStateMachine>().CurrentStateType == EnemyState.Attack)
        {
            _attackCooldownTimer += Time.deltaTime;
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

        _currentHealth -= damage.Value;

        // 개발 빌드에서만 로그 출력
        if (Debug.isDebugBuild)
        {
            Debug.Log($"적 피격! 체력: {_currentHealth}/{_maxHealth}");
        }

        // 넉백 효과 적용
        _enemyEffects.ApplyDamageEffect(damage.From.transform.position);

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
        if (_attackCooldownTimer >= _attackCooldown)
        {
            _attackCooldownTimer = 0f;

            // 실제 공격 로직 구현
            // TODO: 여기에 실제 공격 처리 코드 구현 필요
        }
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
    public bool CanAttack => _attackCooldownTimer >= _attackCooldown;

    /// <summary>
    /// 공격 쿨다운 시간
    /// </summary>
    public float AttackCooldown => _attackCooldown;
    #endregion
}