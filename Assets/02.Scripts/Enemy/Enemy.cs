using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(EnemyPatrol))]
public class Enemy : MonoBehaviour, IDamageable
{
    #region 열거형
    /// <summary>
    /// 적의 상태를 나타내는 열거형입니다.
    /// </summary>
    public enum EnemyState
    {
        Idle,    // 대기
        Patrol,  // 순찰
        Trace,   // 추적
        Return,  // 복귀
        Attack,  // 공격
        Die,     // 사망
    }
    #endregion

    #region 상태 변수
    [Header("상태")]
    [SerializeField] private EnemyState _currentState = EnemyState.Idle; // 현재 적의 상태
    /// <summary>
    /// 현재 적의 상태입니다. 상태 변경 시 관련 로직을 처리합니다.
    /// </summary>
    public EnemyState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState == value) return; // 상태가 같으면 변경하지 않음

            Debug.Log($"적 상태 변경: {_currentState} -> {value}");
            OnStateExit(_currentState); // 이전 상태 종료 처리
            _currentState = value;      // 상태 변경
            OnStateEnter(_currentState); // 새 상태 진입 처리
        }
    }
    [SerializeField] private float _idleDuration = 3f;
    #endregion

    #region 참조
    private GameObject _player;
    private CharacterController _characterController;
    private NavMeshAgent _agent;
    private EnemyPatrol _enemyPatrol;
    #endregion

    #region 감지 및 이동 설정
    [Header("감지 및 이동")]
    [SerializeField] private float _findDistance = 10f;
    [SerializeField] private float _returnDistance = 10f;
    [SerializeField] private float _attackDistance = 1.5f;
    [SerializeField] private float _traceSpeed = 5f;
    [SerializeField] private float _returnSpeed = 4f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _maxTraceDuration = 15f;
    [SerializeField] private float _maxReturnDuration = 20f;
    #endregion

    #region 전투 설정
    [Header("전투")]
    [SerializeField] private float _attackCooldown = 1f;
    private float _attackCooldownTimer = 0f;
    [SerializeField] private int _maxHealth = 100;
    private int _currentHealth;
    [SerializeField] private float _damagedDuration = 0.5f; // 피격 시 경직 시간
    [SerializeField] private float _deathDuration = 1f;
    #endregion

    #region 넉백 설정
    [Header("넉백")]
    [SerializeField] private float _knockbackForce = 5f;
    [SerializeField] private float _knockbackDuration = 0.2f; // _damagedDuration보다 짧아야 함
    #endregion

    #region 비공개 변수
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Coroutine _stateCoroutine;
    private bool _isDead = false;
    private Vector3 _knockbackDirection; // 넉백 방향 저장
    private bool _isDamaged = false; // 피격 상태 플래그
    private Coroutine _damageCoroutine; // 피격 처리 코루틴
    #endregion

    #region Unity 이벤트 함수
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _enemyPatrol = GetComponent<EnemyPatrol>();

        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _traceSpeed;
        _agent.angularSpeed = _rotationSpeed;

        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _currentHealth = _maxHealth;

        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }

    private void Start()
    {
        OnStateEnter(_currentState);
    }

    private void Update()
    {
        CheckStateTransitions();

        if (CurrentState == EnemyState.Attack)
        {
            _attackCooldownTimer += Time.deltaTime;
        }
    }
    #endregion

    #region 상태 머신 로직
    private void CheckStateTransitions()
    {
        if (_isDead || _player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        float distanceToStart = Vector3.Distance(transform.position, _startPosition);

        switch (CurrentState)
        {
            case EnemyState.Idle:
                CheckIdleTransitions(distanceToPlayer);
                break;
            case EnemyState.Patrol:
                CheckPatrolTransitions(distanceToPlayer);
                break;
            case EnemyState.Trace:
                CheckTraceTransitions(distanceToPlayer);
                break;
            case EnemyState.Attack:
                CheckAttackTransitions(distanceToPlayer);
                break;
            case EnemyState.Return:
                CheckReturnTransitions(distanceToPlayer, distanceToStart);
                break;
            case EnemyState.Die:
                break;
        }
    }

    private void CheckIdleTransitions(float distanceToPlayer)
    {
        if (distanceToPlayer < _findDistance)
        {
            CurrentState = EnemyState.Trace;
        }
    }

    private void CheckPatrolTransitions(float distanceToPlayer)
    {
        if (distanceToPlayer < _findDistance)
        {
            CurrentState = EnemyState.Trace;
        }
    }

    private void CheckTraceTransitions(float distanceToPlayer)
    {
        if (distanceToPlayer > _returnDistance)
        {
            CurrentState = EnemyState.Return;
        }
        else if (distanceToPlayer < _attackDistance)
        {
            CurrentState = EnemyState.Attack;
        }
    }

    private void CheckAttackTransitions(float distanceToPlayer)
    {
        if (distanceToPlayer > _attackDistance)
        {
            CurrentState = EnemyState.Trace;
        }
    }

    private void CheckReturnTransitions(float distanceToPlayer, float distanceToStart)
    {
        if (distanceToStart < _characterController.radius && _characterController.isGrounded)
        {
            CurrentState = EnemyState.Idle;
        }
        else if (distanceToPlayer < _findDistance)
        {
            CurrentState = EnemyState.Trace;
        }
    }

    private void OnStateEnter(EnemyState newState)
    {
        if (_stateCoroutine != null)
        {
            StopCoroutine(_stateCoroutine);
            _stateCoroutine = null;
        }

        switch (newState)
        {
            case EnemyState.Idle:
                _stateCoroutine = StartCoroutine(Idle_Coroutine());
                break;
            case EnemyState.Patrol:
                Vector3? startPosOverride = null;
                if (!_enemyPatrol.HasWaypoints)
                {
                    startPosOverride = _startPosition;
                }
                _enemyPatrol.StartPatrol(_characterController, _returnSpeed, startPosOverride);
                break;
            case EnemyState.Trace:
                _stateCoroutine = StartCoroutine(Trace_Coroutine());
                break;
            case EnemyState.Return:
                _stateCoroutine = StartCoroutine(Return_Coroutine());
                break;
            case EnemyState.Attack:
                _attackCooldownTimer = _attackCooldown;
                _stateCoroutine = StartCoroutine(Attack_Coroutine());
                break;
            case EnemyState.Die:
                _stateCoroutine = StartCoroutine(Die_Coroutine());
                break;
        }
    }

    private void OnStateExit(EnemyState oldState)
    {
        switch (oldState)
        {
            case EnemyState.Idle:
                if (_stateCoroutine != null)
                {
                    StopCoroutine(_stateCoroutine);
                    _stateCoroutine = null;
                }
                break;
            case EnemyState.Patrol:
                _enemyPatrol.StopPatrol();
                break;
            case EnemyState.Attack:
                _attackCooldownTimer = 0f;
                break;
        }
    }
    #endregion

    #region 상태 코루틴
    private IEnumerator Idle_Coroutine()
    {
        yield return new WaitForSeconds(_idleDuration);

        if (CurrentState == EnemyState.Idle && !_isDead)
        {
            CurrentState = EnemyState.Patrol;
        }
    }

    private IEnumerator Trace_Coroutine()
    {
        float traceTimer = 0f; // 추적 시간 타이머
        _agent.speed = _traceSpeed; // 에이전트 속도 설정

        while (CurrentState == EnemyState.Trace && _player != null)
        {
            traceTimer += Time.deltaTime; // 타이머 증가

            // 최대 추적 시간 초과 시 Return 상태로 전환
            // 잠재적인 무한 루프 방지
            if (traceTimer > _maxTraceDuration)
            {
                Debug.LogWarning($"최대 추적 시간({_maxTraceDuration}초) 초과. Return 상태로 전환합니다.");
                CurrentState = EnemyState.Return;
                yield break; // 코루틴 종료
            }

            // 유틸리티 메서드를 사용하여 NavMeshAgent 상태 확인 및 이동 처리
            if (NavMeshUtility.IsAgentValid(_agent))
            {
                // 플레이어 위치로 이동 설정
                NavMeshUtility.TrySetDestination(_agent, _player.transform.position);

                // 이동 방향으로 회전
                RotateToMoveDirection();
            }
            else
            {
                Debug.LogWarning("Enemy가 NavMesh 위에 없습니다. 추적을 중단하고 Return 상태로 전환합니다.");
                CurrentState = EnemyState.Return;
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator Return_Coroutine()
    {
        float returnTimer = 0f; // 복귀 시간 타이머
        _agent.speed = _returnSpeed;
        NavMeshUtility.TrySetDestination(_agent, _startPosition);

        while (CurrentState == EnemyState.Return)
        {
            returnTimer += Time.deltaTime; // 타이머 증가

            // 최대 복귀 시간 초과 시 안전 장치 발동
            if (returnTimer > _maxReturnDuration)
            {
                Debug.LogWarning($"최대 복귀 시간({_maxReturnDuration}초) 초과. 강제로 시작 위치로 이동합니다.");
                _agent.isStopped = true; // 에이전트 정지
                _agent.ResetPath();      // 경로 초기화
                transform.position = _startPosition; // 위치 강제 설정
                transform.rotation = _startRotation; // 회전 강제 설정
                CurrentState = EnemyState.Idle; // Idle 상태로 전환
                yield break; // 코루틴 종료
            }

            // 이동 방향으로 회전
            RotateToMoveDirection();

            // 유틸리티 메서드를 사용하여 목적지 도달 여부 확인
            if (NavMeshUtility.HasReachedDestination(_agent, _startPosition, _agent.stoppingDistance))
            {
                CurrentState = EnemyState.Idle; // Idle 상태로 전환
                yield break; // 코루틴 종료
            }

            yield return null;
        }
    }

    private IEnumerator Attack_Coroutine()
    {
        while (CurrentState == EnemyState.Attack && _player != null)
        {
            // 타겟 방향으로 회전
            RotateToTarget(_player.transform.position);

            if (_attackCooldownTimer >= _attackCooldown)
            {
                _attackCooldownTimer = 0f;
                Debug.Log("플레이어 공격!");
            }

            yield return null;
        }
    }

    private IEnumerator Die_Coroutine()
    {
        _isDead = true;
        _characterController.enabled = false;
        GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(_deathDuration);

        gameObject.SetActive(false);
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 적에게 데미지를 적용하고 넉백 효과를 발생시킵니다.
    /// </summary>
    /// <param name="damage">입힐 데미지 정보 (From: 데미지 발생 위치)</param>
    public void TakeDamage(Damage damage)
    {
        if (_isDead || _isDamaged) return; // 이미 피격 상태거나 죽은 상태면 무시

        _currentHealth -= damage.Value;
        Debug.Log($"적 피격! 체력: {_currentHealth}/{_maxHealth}");

        // 넉백 방향 계산 (데미지 발생 위치로부터 멀어지는 방향)
        _knockbackDirection = (transform.position - damage.From.transform.position).normalized;
        _knockbackDirection.y = 0; // 넉백을 수평으로 유지

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            CurrentState = EnemyState.Die;
        }
        else
        {
            // 피격 처리를 현재 상태와 별개로 처리
            if (_damageCoroutine != null)
            {
                StopCoroutine(_damageCoroutine);
            }
            _damageCoroutine = StartCoroutine(ApplyDamageEffect());
        }
    }

    /// <summary>
    /// 피격 효과(넉백, 경직)를 적용하는 코루틴입니다.
    /// </summary>
    private IEnumerator ApplyDamageEffect()
    {
        _isDamaged = true; // 피격 상태 설정

        // 현재 에이전트 상태 백업
        bool wasAgentStopped = _agent.isStopped;

        // 피격 방향으로 즉시 회전
        if (_knockbackDirection != Vector3.zero)
        {
            Vector3 lookDirection = -_knockbackDirection; // 넉백 방향의 반대 = 공격자 방향
            lookDirection.y = 0; // 수평 방향만 고려

            if (lookDirection != Vector3.zero) // 안전 검사
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = targetRotation; // 즉시 회전
            }
        }

        float timer = 0f;
        // _knockbackDuration 동안 넉백 적용
        while (timer < _knockbackDuration)
        {
            if (_characterController.enabled && !_isDead) // 컨트롤러가 활성화되어 있고 적이 죽지 않았는지 확인
            {
                _agent.isStopped = true; // 이동 중지
                // 넉백 힘 + 중력 적용
                Vector3 move = _knockbackDirection * _knockbackForce * Time.deltaTime + Physics.gravity * Time.deltaTime;
                _characterController.Move(move);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 남은 피격 지속 시간 동안 대기
        float remainingDuration = _damagedDuration - _knockbackDuration;
        if (remainingDuration > 0)
        {
            _agent.isStopped = true; // 이동 중지
            _agent.ResetPath(); // 경로 초기화
            yield return new WaitForSeconds(remainingDuration);
        }

        // 이전 상태로 복원
        _agent.isStopped = wasAgentStopped;

        if (!_isDead)
        {
            // 에이전트가 멈춰있었다면 현재 상태에 따라 목적지 갱신
            if (CurrentState == EnemyState.Trace && _player != null)
            {
                _agent.SetDestination(_player.transform.position);
            }
            else if (CurrentState == EnemyState.Return)
            {
                _agent.SetDestination(_startPosition);
            }
        }

        _isDamaged = false; // 피격 상태 해제
        _damageCoroutine = null;
    }
    #endregion

    #region 헬퍼 메서드
    /// <summary>
    /// NavMeshAgent의 이동 방향으로 회전합니다.
    /// </summary>
    private void RotateToMoveDirection()
    {
        // 피격 상태이거나 유효한 속도가 없으면 회전하지 않음
        if (_isDamaged || _agent.velocity.magnitude < 0.1f) return;

        Vector3 moveDirection = _agent.velocity.normalized;
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 지정된 대상 위치를 향해 회전합니다.
    /// </summary>
    /// <param name="targetPosition">바라볼 대상 위치</param>
    private void RotateToTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0; // 수평 회전만 적용

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }
    #endregion
}
