using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 캐릭터의 시각적/물리적 효과를 관리하는 클래스입니다.
/// </summary>
public class EnemyEffects : MonoBehaviour
{
    [Header("피격 효과")]
    [SerializeField] private float _knockbackForce = 5f;
    [SerializeField] private float _knockbackDuration = 0.2f;
    [SerializeField] private float _damagedDuration = 0.5f;

    [Header("사망 효과")]
    [SerializeField] private GameObject _deathParticle;
    [SerializeField] private AudioClip _deathSound;

    private Animator _animator;
    private bool _isDamaged = false;
    private Coroutine _damageCoroutine;
    private Vector3 _knockbackDirection;

    private CharacterController _characterController;
    private NavMeshAgent _agent;
    private Enemy _enemy;

    #region Unity 이벤트 함수
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _agent = GetComponent<NavMeshAgent>();
        _enemy = GetComponent<Enemy>();
        _animator = GetComponent<Animator>();
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 피격 효과를 적용합니다.
    /// </summary>
    public void ApplyDamageEffect(Vector3 damageSourcePosition)
    {
        if (_enemy.GetComponent<EnemyCombat>().IsDead || _isDamaged) return;

        // 넉백 방향 계산
        _knockbackDirection = (transform.position - damageSourcePosition).normalized;
        _knockbackDirection.y = 0;

        if (_damageCoroutine != null)
        {
            StopCoroutine(_damageCoroutine);
        }

        _damageCoroutine = StartCoroutine(DamageEffectCoroutine());
    }

    /// <summary>
    /// 사망 이펙트를 재생합니다.
    /// </summary>
    public void PlayDeathEffect()
    {
        // 사망 이펙트 파티클 재생
        if (_deathParticle != null)
        {
            Instantiate(_deathParticle, transform.position, Quaternion.identity);
        }

        // 사망 사운드 재생
        if (_deathSound != null)
        {
            AudioSource.PlayClipAtPoint(_deathSound, transform.position);
        }

        // 사망 애니메이션 재생
        if (_animator != null)
        {
            _animator.SetTrigger("Die");
        }
    }
    #endregion

    #region 비공개 메서드
    private IEnumerator DamageEffectCoroutine()
    {
        _isDamaged = true;
        bool wasAgentStopped = false;

        // NavMeshAgent 상태 확인 후 저장
        if (NavMeshUtility.IsAgentValid(_agent))
        {
            wasAgentStopped = _agent.isStopped;
        }

        // 피격 방향으로 회전
        if (_knockbackDirection != Vector3.zero)
        {
            // 회전해야 할 방향 계산 (피격 반대 방향)
            Vector3 lookPosition = transform.position - _knockbackDirection;
            _enemy.RotateToTarget(lookPosition);
        }

        // 넉백 적용
        float timer = 0f;
        while (timer < _knockbackDuration)
        {
            if (_characterController.enabled && !_enemy.GetComponent<EnemyCombat>().IsDead)
            {
                // NavMeshAgent가 유효한 경우에만 조작
                if (NavMeshUtility.IsAgentValid(_agent))
                {
                    _agent.isStopped = true;
                }

                Vector3 move = _knockbackDirection * _knockbackForce * Time.deltaTime + Physics.gravity * Time.deltaTime;
                _characterController.Move(move);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 대기 시간
        float remainingDuration = _damagedDuration - _knockbackDuration;
        if (remainingDuration > 0)
        {
            // NavMeshAgent가 유효한 경우에만 조작
            if (NavMeshUtility.IsAgentValid(_agent))
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
            yield return new WaitForSeconds(remainingDuration);
        }

        // 상태 복원 (NavMeshAgent가 유효한 경우에만)
        if (NavMeshUtility.IsAgentValid(_agent))
        {
            _agent.isStopped = wasAgentStopped;
        }

        _isDamaged = false;
        _damageCoroutine = null;
    }
    #endregion

    #region 프로퍼티
    public bool IsDamaged => _isDamaged;
    #endregion
}