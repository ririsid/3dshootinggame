using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(EnemyPatrol))]
public class Enemy : MonoBehaviour
{
    #region Enums
    public enum EnemyState
    {
        Idle,
        Patrol,
        Trace,
        Return,
        Attack,
        Damaged,
        Die,
    }
    #endregion

    #region State Variables
    [Header("State")]
    [SerializeField] private EnemyState _currentState = EnemyState.Idle;
    public EnemyState CurrentState
    {
        get => _currentState;
        private set
        {
            if (_currentState == value) return;

            Debug.Log($"Enemy State Changed: {_currentState} -> {value}");
            OnStateExit(_currentState);
            _currentState = value;
            OnStateEnter(_currentState);
        }
    }
    [SerializeField] private float _idleDuration = 3f;
    #endregion

    #region References
    private GameObject _player;
    private CharacterController _characterController;
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
    #endregion

    #region Combat Settings
    [Header("Combat")]
    [SerializeField] private float _attackCooldown = 1f;
    private float _attackCooldownTimer = 0f;
    [SerializeField] private int _maxHealth = 100;
    private int _currentHealth;
    [SerializeField] private float _damagedDuration = 0.5f;
    [SerializeField] private float _deathDuration = 1f;
    #endregion

    #region Private Variables
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Coroutine _stateCoroutine;
    private bool _isDead = false;
    #endregion

    #region Unity Event Functions
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _enemyPatrol = GetComponent<EnemyPatrol>();

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
        while (CurrentState == EnemyState.Trace && _player != null)
        {
            Vector3 direction = (_player.transform.position - transform.position);
            direction.y = 0;

            if (direction.magnitude > _characterController.radius)
            {
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                }

                Vector3 move = direction.normalized * _traceSpeed * Time.deltaTime;
                _characterController.Move(move + Physics.gravity * Time.deltaTime);
            }

            yield return null;
        }
    }

    private IEnumerator Return_Coroutine()
    {
        while (CurrentState == EnemyState.Return)
        {
            Vector3 direction = (_startPosition - transform.position);
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }

            Vector3 move = direction.normalized * _returnSpeed * Time.deltaTime;
            _characterController.Move(move + Physics.gravity * Time.deltaTime);

            yield return null;
        }
        if (CurrentState != EnemyState.Trace)
        {
            transform.position = _startPosition;
            transform.rotation = _startRotation;
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
        yield return new WaitForSeconds(_damagedDuration);

        if (CurrentState == EnemyState.Damaged && !_isDead)
        {
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
    /// 적에게 데미지를 적용합니다.
    /// </summary>
    /// <param name="damage">입힐 데미지 양</param>
    public void TakeDamage(Damage damage)
    {
        if (_isDead || CurrentState == EnemyState.Damaged) return;

        _currentHealth -= damage.Value;
        Debug.Log($"Enemy Hit! Health: {_currentHealth}/{_maxHealth}");

        if (_currentHealth <= 0)
        {
            _currentHealth = 0;
            CurrentState = EnemyState.Die;
        }
        else
        {
            CurrentState = EnemyState.Damaged;
        }
    }
    #endregion
}
