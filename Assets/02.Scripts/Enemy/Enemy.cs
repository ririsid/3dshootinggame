using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(EnemyPatrol))]
public class Enemy : MonoBehaviour, IDamageable
{
    #region Enums
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
        Damaged, // 피격
        Die,     // 사망
    }
    #endregion

    #region State Variables
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

    #region References
    private GameObject _player;
    private CharacterController _characterController;
    private NavMeshAgent _agent;
    private EnemyPatrol _enemyPatrol;
    #endregion

    #region Detection & Movement Settings
    [Header("Detection & Movement")]
    [SerializeField] private float _findDistance = 10f;
    [SerializeField] private float _returnDistance = 10f;
    [SerializeField] private float _attackDistance = 1.5f;
    [SerializeField] private float _traceSpeed = 5f;
    [SerializeField] private float _returnSpeed = 4f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _maxTraceDuration = 15f;
    [SerializeField] private float _maxReturnDuration = 20f;
    #endregion

    #region Combat Settings
    [Header("Combat")]
    [SerializeField] private float _attackCooldown = 1f;
    private float _attackCooldownTimer = 0f;
    [SerializeField] private int _maxHealth = 100;
    private int _currentHealth;
    [SerializeField] private float _damagedDuration = 0.5f; // 피격 시 경직 시간
    [SerializeField] private float _deathDuration = 1f;
    #endregion

    #region Knockback Settings
    [Header("Knockback")]
    [SerializeField] private float _knockbackForce = 5f;
    [SerializeField] private float _knockbackDuration = 0.2f; // _damagedDuration보다 짧아야 함
    #endregion

    #region Private Variables
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Coroutine _stateCoroutine;
    private bool _isDead = false;
    private Vector3 _knockbackDirection; // 넉백 방향 저장
    #endregion

    #region Unity Event Functions
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

    #region State Machine Logic
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
            case EnemyState.Damaged:
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
            case EnemyState.Damaged:
                _stateCoroutine = StartCoroutine(Damaged_Coroutine());
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

    #region State Coroutines
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

            // 플레이어 위치로 이동 설정 (NavMeshAgent 사용)
            if (_agent.isOnNavMesh && _agent.enabled) // 에이전트가 활성화되어 있고 NavMesh 위에 있는지 확인
            {
                _agent.SetDestination(_player.transform.position);
            }
            else if (!_agent.isOnNavMesh)
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
        _agent.SetDestination(_startPosition);

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

            // 목표 지점 도달 여부 확인 (NavMeshAgent의 remainingDistance 사용)
            if (_agent.isOnNavMesh && _agent.enabled && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                // 목적지에 도달했거나 더 이상 이동할 수 없을 때
                if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
                {
                    // 실제 목적지에 도달했는지 추가 확인 (선택 사항)
                    if (Vector3.Distance(transform.position, _startPosition) <= _agent.stoppingDistance + 0.1f) // 약간의 오차 허용
                    {
                        CurrentState = EnemyState.Idle; // Idle 상태로 전환
                        yield break; // 코루틴 종료
                    }
                }
            }


            yield return null;
        }
    }

    private IEnumerator Attack_Coroutine()
    {
        while (CurrentState == EnemyState.Attack && _player != null)
        {
            Vector3 direction = (_player.transform.position - transform.position);
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }

            if (_attackCooldownTimer >= _attackCooldown)
            {
                _attackCooldownTimer = 0f;
                Debug.Log("플레이어 공격!");
            }

            yield return null;
        }
    }

    private IEnumerator Damaged_Coroutine()
    {
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
            _agent.isStopped = false; // 이동 재개
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

        if (CurrentState == EnemyState.Damaged && !_isDead)
        {
            // 넉백 및 대기 후, 추적 상태로 전환될 가능성이 높음
            CurrentState = EnemyState.Trace;
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

    #region Public Methods
    /// <summary>
    /// 적에게 데미지를 적용하고 넉백 효과를 발생시킵니다.
    /// </summary>
    /// <param name="damage">입힐 데미지 정보 (From: 데미지 발생 위치)</param>
    public void TakeDamage(Damage damage)
    {
        if (_isDead || CurrentState == EnemyState.Damaged) return;

        _currentHealth -= damage.Value;
        Debug.Log($"Enemy Hit! Health: {_currentHealth}/{_maxHealth}");

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
            CurrentState = EnemyState.Damaged; // 넉백을 적용하기 위해 Damaged 상태로 진입
        }
    }
    #endregion
}
