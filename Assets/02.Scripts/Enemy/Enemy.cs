using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyPatrol))]
[RequireComponent(typeof(EnemyStateMachine))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(EnemyEffects))]
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

    #region 참조
    [Header("참조")]
    private GameObject _player;
    private CharacterController _characterController;
    private NavMeshAgent _agent;
    private EnemyPatrol _enemyPatrol;
    private EnemyStateMachine _stateMachine;
    private EnemyCombat _combat;
    private EnemyEffects _effects;
    #endregion

    #region 감지 및 이동 설정
    [Header("감지 및 이동")]
    [SerializeField] private float _findDistance = 10f;
    [SerializeField] private float _returnDistance = 10f;
    [SerializeField] private float _attackDistance = 1.5f;
    [SerializeField] private float _viewAngle = 120f;      // 시야 각도
    [SerializeField] private float _traceSpeed = 5f;
    [SerializeField] private float _returnSpeed = 4f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _maxTraceDuration = 15f;
    [SerializeField] private float _maxReturnDuration = 20f;
    [SerializeField] private float _idleDuration = 3f;
    #endregion

    #region 상태별 세부 설정
    [Header("대기 상태 설정")]
    [SerializeField] private float _lookAroundInterval = 2f;      // 회전 간격
    [SerializeField] private float _rotationDuration = 1f;        // 회전 지속 시간
    [SerializeField] private float _minLookDistance = 5f;         // 최소 관찰 거리
    [SerializeField] private float _maxLookDistance = 15f;        // 최대 관찰 거리
    [SerializeField] private LayerMask _obstacleLayer;            // 장애물 레이어

    [Header("추적 상태 설정")]
    [SerializeField] private float _pathUpdateInterval = 0.5f;    // 경로 업데이트 간격

    [Header("공격 상태 설정")]
    [SerializeField] private float _attackPositionUpdateInterval = 0.5f;  // 공격 위치 업데이트 간격

    [Header("사망 상태 설정")]
    [SerializeField] private float _deathDuration = 1f;           // 사망 애니메이션 지속 시간
    #endregion

    #region 비공개 변수
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    #endregion

    #region 프로퍼티
    public GameObject Player => _player;
    public CharacterController CharacterController => _characterController;
    public NavMeshAgent Agent => _agent;
    public EnemyPatrol EnemyPatrol => _enemyPatrol;
    public float FindDistance => _findDistance;
    public float ReturnDistance => _returnDistance;
    public float AttackDistance => _attackDistance;
    public float ViewAngle => _viewAngle;
    public float TraceSpeed => _traceSpeed;
    public float ReturnSpeed => _returnSpeed;
    public float RotationSpeed => _rotationSpeed;
    public float MaxTraceDuration => _maxTraceDuration;
    public float MaxReturnDuration => _maxReturnDuration;
    public float IdleDuration => _idleDuration;
    public Vector3 StartPosition => _startPosition;
    public Quaternion StartRotation => _startRotation;
    public bool IsDead => _combat.IsDead;

    public float LookAroundInterval => _lookAroundInterval;
    public float RotationDuration => _rotationDuration;
    public float MinLookDistance => _minLookDistance;
    public float MaxLookDistance => _maxLookDistance;
    public LayerMask ObstacleLayer => _obstacleLayer;
    public float PathUpdateInterval => _pathUpdateInterval;
    public float AttackPositionUpdateInterval => _attackPositionUpdateInterval;
    public float DeathDuration => _deathDuration;
    #endregion

    #region Unity 이벤트 함수
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _agent = GetComponent<NavMeshAgent>();
        _enemyPatrol = GetComponent<EnemyPatrol>();
        _stateMachine = GetComponent<EnemyStateMachine>();
        _combat = GetComponent<EnemyCombat>();
        _effects = GetComponent<EnemyEffects>();

        _startPosition = transform.position;
        _startRotation = transform.rotation;

        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }

        // 상태 머신 초기화
        InitializeStateMachine();
    }

    private void Update()
    {
        if (_effects.IsDamaged || IsDead) return;

        _stateMachine.UpdateState();
    }
    #endregion

    #region 초기화 메서드
    private void InitializeStateMachine()
    {
        Dictionary<EnemyState, IEnemyState> states = new Dictionary<EnemyState, IEnemyState>
        {
            { EnemyState.Idle, new IdleState(this) },
            { EnemyState.Patrol, new PatrolState(this) },
            { EnemyState.Trace, new TraceState(this) },
            { EnemyState.Return, new ReturnState(this) },
            { EnemyState.Attack, new AttackState(this) },
            { EnemyState.Die, new DieState(this) }
        };

        _stateMachine.Initialize(states);
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 적의 상태를 변경합니다.
    /// </summary>
    public void SetState(EnemyState newState)
    {
        _stateMachine.SetState(newState);
    }

    /// <summary>
    /// 대상을 향해 회전합니다.
    /// </summary>
    public void RotateToTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0; // 수평 회전만 적용

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 이동 방향으로 회전합니다.
    /// </summary>
    public void RotateToMoveDirection()
    {
        if (_effects.IsDamaged || !IsAgentValid() || _agent.velocity.magnitude < 0.1f) return;

        Vector3 moveDirection = _agent.velocity.normalized;
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// IDamageable 인터페이스 구현
    /// </summary>
    public void TakeDamage(Damage damage)
    {
        _combat.TakeDamage(damage);
    }

    /// <summary>
    /// 적 캐릭터의 사망 처리를 시작합니다.
    /// </summary>
    public void BeginDeath()
    {
        // 물리적 상호작용 중지
        CharacterController.enabled = false;
        GetComponent<Collider>().enabled = false;

        // 효과 컴포넌트에 사망 이펙트 요청
        _effects.PlayDeathEffect();
    }

    /// <summary>
    /// 적 캐릭터의 사망 처리를 완료하고 게임 오브젝트를 비활성화합니다.
    /// </summary>
    public void CompleteDeath()
    {
        // 오브젝트 비활성화 전에 필요한 처리
        // 예: 아이템 드롭, 경험치 지급 등

        // 오브젝트 비활성화
        gameObject.SetActive(false);
    }
    #endregion

    #region 이동 관련 메서드
    /// <summary>
    /// 지정된 위치로 NavMeshAgent 목적지를 설정합니다.
    /// </summary>
    public bool SetDestination(Vector3 destination)
    {
        return NavMeshUtility.TrySetDestination(_agent, destination);
    }

    /// <summary>
    /// 목적지까지의 거리를 계산합니다.
    /// </summary>
    public float GetDistanceToDestination(Vector3 destination, bool useNavMeshPath = true)
    {
        return NavMeshUtility.GetDistanceTo(_agent, destination, useNavMeshPath);
    }

    /// <summary>
    /// 목적지에 도달했는지 확인합니다.
    /// </summary>
    public bool HasReachedDestination(Vector3 destination, float stoppingDistance = 0.1f)
    {
        return NavMeshUtility.HasReachedDestination(_agent, destination, stoppingDistance);
    }

    /// <summary>
    /// NavMeshAgent가 유효한 상태인지 확인합니다.
    /// </summary>
    public bool IsAgentValid()
    {
        return NavMeshUtility.IsAgentValid(_agent);
    }

    /// <summary>
    /// 대상이 시야 내에 있는지 확인합니다.
    /// </summary>
    public bool IsTargetInSight(Vector3 targetPosition, float? viewAngle = null, float? maxDistance = null, LayerMask? layerMask = null)
    {
        return NavMeshUtility.IsInSight(
            transform,
            targetPosition,
            viewAngle ?? _viewAngle,
            maxDistance ?? _findDistance,
            layerMask
        );
    }
    #endregion
}
