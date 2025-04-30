using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 캐릭터의 핵심 기능을 관리하는 클래스입니다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStateMachine))]
[RequireComponent(typeof(EnemyCombat))]
[RequireComponent(typeof(EnemyEffects))]
public class Enemy : MonoBehaviour, IDamageable
{
    #region 참조
    [Header("참조")]
    /// <summary>
    /// 플레이어 게임 오브젝트 참조
    /// </summary>
    private GameObject _player;

    /// <summary>
    /// 캐릭터 컨트롤러 컴포넌트
    /// </summary>
    private CharacterController _characterController;

    /// <summary>
    /// 내비게이션 에이전트 컴포넌트
    /// </summary>
    private NavMeshAgent _agent;

    /// <summary>
    /// 적 상태 머신 컴포넌트
    /// </summary>
    private EnemyStateMachine _stateMachine;

    /// <summary>
    /// 적 전투 컴포넌트
    /// </summary>
    private EnemyCombat _combat;

    /// <summary>
    /// 적 효과 컴포넌트
    /// </summary>
    private EnemyEffects _effects;

    /// <summary>
    /// 적 순찰 컴포넌트
    /// </summary>
    private EnemyPatrol _enemyPatrol;
    #endregion

    #region 감지 및 이동 설정
    [Header("감지 및 이동")]
    /// <summary>
    /// 플레이어 감지 거리
    /// </summary>
    [SerializeField] private float _findDistance = 10f;

    /// <summary>
    /// 복귀 시작 거리 (시작 위치로부터)
    /// </summary>
    [SerializeField] private float _returnDistance = 10f;

    /// <summary>
    /// 공격 가능 거리
    /// </summary>
    [SerializeField] private float _attackDistance = 1.5f;

    /// <summary>
    /// 시야 각도
    /// </summary>
    [SerializeField] private float _viewAngle = 120f;

    /// <summary>
    /// 추적 속도
    /// </summary>
    [SerializeField] private float _traceSpeed = 5f;

    /// <summary>
    /// 복귀 속도
    /// </summary>
    [SerializeField] private float _returnSpeed = 4f;

    /// <summary>
    /// 회전 속도
    /// </summary>
    [SerializeField] private float _rotationSpeed = 10f;

    /// <summary>
    /// 최대 추적 지속 시간
    /// </summary>
    [SerializeField] private float _maxTraceDuration = 15f;

    /// <summary>
    /// 최대 복귀 지속 시간
    /// </summary>
    [SerializeField] private float _maxReturnDuration = 20f;

    /// <summary>
    /// 대기 상태 지속 시간
    /// </summary>
    [SerializeField] private float _idleDuration = 3f;
    #endregion

    #region 상태별 세부 설정
    [Header("대기 상태 설정")]
    /// <summary>
    /// 주변 둘러보기 간격
    /// </summary>
    [SerializeField] private float _lookAroundInterval = 2f;

    /// <summary>
    /// 회전 지속 시간
    /// </summary>
    [SerializeField] private float _rotationDuration = 1f;

    /// <summary>
    /// 최소 관찰 거리
    /// </summary>
    [SerializeField] private float _minLookDistance = 5f;

    /// <summary>
    /// 최대 관찰 거리
    /// </summary>
    [SerializeField] private float _maxLookDistance = 15f;

    /// <summary>
    /// 장애물 레이어 마스크
    /// </summary>
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("추적 상태 설정")]
    /// <summary>
    /// 추적 경로 업데이트 간격
    /// </summary>
    [SerializeField] private float _pathUpdateInterval = 0.5f;

    [Header("공격 상태 설정")]
    /// <summary>
    /// 공격 위치 업데이트 간격
    /// </summary>
    [SerializeField] private float _attackPositionUpdateInterval = 0.5f;

    [Header("사망 상태 설정")]
    /// <summary>
    /// 사망 애니메이션 지속 시간
    /// </summary>
    [SerializeField] private float _deathDuration = 1f;
    #endregion

    #region 행동 전략 설정
    [Header("행동 전략 설정")]
    /// <summary>
    /// 적의 행동 타입
    /// </summary>
    [SerializeField] private EnemyType _enemyType = EnemyType.Patrol;

    /// <summary>
    /// 현재 적용 중인 행동 전략
    /// </summary>
    private IEnemyBehaviorStrategy _behaviorStrategy;
    #endregion

    #region 비공개 변수
    /// <summary>
    /// 시작 위치
    /// </summary>
    private Vector3 _startPosition;

    /// <summary>
    /// 시작 회전값
    /// </summary>
    private Quaternion _startRotation;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 플레이어 참조 프로퍼티
    /// </summary>
    public GameObject Player => _player;

    /// <summary>
    /// 캐릭터 컨트롤러 프로퍼티
    /// </summary>
    public CharacterController CharacterController => _characterController;

    /// <summary>
    /// NavMeshAgent 프로퍼티
    /// </summary>
    public NavMeshAgent Agent => _agent;

    /// <summary>
    /// 적 순찰 컴포넌트 프로퍼티
    /// </summary>
    public EnemyPatrol EnemyPatrol => _enemyPatrol;

    /// <summary>
    /// 플레이어 감지 거리 프로퍼티
    /// </summary>
    public float FindDistance => _findDistance;

    /// <summary>
    /// 복귀 거리 프로퍼티
    /// </summary>
    public float ReturnDistance => _returnDistance;

    /// <summary>
    /// 공격 거리 프로퍼티
    /// </summary>
    public float AttackDistance => _attackDistance;

    /// <summary>
    /// 시야 각도 프로퍼티
    /// </summary>
    public float ViewAngle => _viewAngle;

    /// <summary>
    /// 추적 속도 프로퍼티
    /// </summary>
    public float TraceSpeed => _traceSpeed;

    /// <summary>
    /// 복귀 속도 프로퍼티
    /// </summary>
    public float ReturnSpeed => _returnSpeed;

    /// <summary>
    /// 회전 속도 프로퍼티
    /// </summary>
    public float RotationSpeed => _rotationSpeed;

    /// <summary>
    /// 최대 추적 지속 시간 프로퍼티
    /// </summary>
    public float MaxTraceDuration => _maxTraceDuration;

    /// <summary>
    /// 최대 복귀 지속 시간 프로퍼티
    /// </summary>
    public float MaxReturnDuration => _maxReturnDuration;

    /// <summary>
    /// 대기 상태 지속 시간 프로퍼티
    /// </summary>
    public float IdleDuration => _idleDuration;

    /// <summary>
    /// 주변 둘러보기 간격 프로퍼티
    /// </summary>
    public float LookAroundInterval => _lookAroundInterval;

    /// <summary>
    /// 회전 지속 시간 프로퍼티
    /// </summary>
    public float RotationDuration => _rotationDuration;

    /// <summary>
    /// 최소 관찰 거리 프로퍼티
    /// </summary>
    public float MinLookDistance => _minLookDistance;

    /// <summary>
    /// 최대 관찰 거리 프로퍼티
    /// </summary>
    public float MaxLookDistance => _maxLookDistance;

    /// <summary>
    /// 장애물 레이어 프로퍼티
    /// </summary>
    public LayerMask ObstacleLayer => _obstacleLayer;

    /// <summary>
    /// 경로 업데이트 간격 프로퍼티
    /// </summary>
    public float PathUpdateInterval => _pathUpdateInterval;

    /// <summary>
    /// 공격 위치 업데이트 간격 프로퍼티
    /// </summary>
    public float AttackPositionUpdateInterval => _attackPositionUpdateInterval;

    /// <summary>
    /// 사망 지속 시간 프로퍼티
    /// </summary>
    public float DeathDuration => _deathDuration;

    /// <summary>
    /// 시작 위치 프로퍼티
    /// </summary>
    public Vector3 StartPosition => _startPosition;

    /// <summary>
    /// 시작 회전값 프로퍼티
    /// </summary>
    public Quaternion StartRotation => _startRotation;

    /// <summary>
    /// 사망 여부 프로퍼티
    /// </summary>
    public bool IsDead => _combat.IsDead;

    /// <summary>
    /// 행동 전략 프로퍼티
    /// </summary>
    public IEnemyBehaviorStrategy BehaviorStrategy => _behaviorStrategy;

    /// <summary>
    /// 적 타입 프로퍼티
    /// </summary>
    public EnemyType EnemyType => _enemyType;
    #endregion

    #region Unity 이벤트 함수
    /// <summary>
    /// 컴포넌트 초기화를 수행합니다.
    /// </summary>
    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _agent = GetComponent<NavMeshAgent>();
        _stateMachine = GetComponent<EnemyStateMachine>();
        _combat = GetComponent<EnemyCombat>();
        _effects = GetComponent<EnemyEffects>();
        _enemyPatrol = GetComponent<EnemyPatrol>(); // 존재하지 않을 수 있음

        _startPosition = transform.position;
        _startRotation = transform.rotation;

        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player == null && Debug.isDebugBuild)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }

        // 행동 전략 초기화
        InitializeBehaviorStrategy();

        // 상태 머신 초기화
        InitializeStateMachine();
    }

    /// <summary>
    /// 매 프레임 상태를 업데이트합니다.
    /// </summary>
    private void Update()
    {
        if (_effects.IsDamaged || IsDead) return;

        _stateMachine.UpdateState();
    }
    #endregion

    #region 초기화 메서드
    /// <summary>
    /// 적 타입에 맞는 행동 전략을 초기화합니다.
    /// </summary>
    private void InitializeBehaviorStrategy()
    {
        switch (_enemyType)
        {
            case EnemyType.Patrol:
                _behaviorStrategy = new PatrolBehaviorStrategy();
                break;
            case EnemyType.Chase:
                _behaviorStrategy = new ChaseBehaviorStrategy();
                break;
            default:
                _behaviorStrategy = new PatrolBehaviorStrategy();
                break;
        }
    }

    /// <summary>
    /// 상태 머신을 초기화합니다.
    /// </summary>
    private void InitializeStateMachine()
    {
        Dictionary<EnemyState, IEnemyState> states = new Dictionary<EnemyState, IEnemyState>
        {
            { EnemyState.Idle, new IdleState(this) },
            { EnemyState.Trace, new TraceState(this) },
            { EnemyState.Return, new ReturnState(this) },
            { EnemyState.Attack, new AttackState(this) },
            { EnemyState.Die, new DieState(this) }
        };

        _stateMachine.Initialize(states);

        // 전략에서 제공하는 초기 상태로 설정
        EnemyState initialState = _behaviorStrategy.GetInitialState();
        _stateMachine.SetState(initialState);
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 적의 상태를 변경합니다.
    /// </summary>
    /// <param name="newState">새로운 상태</param>
    public void SetState(EnemyState newState)
    {
        _stateMachine.SetState(newState);
    }

    /// <summary>
    /// 적의 행동 전략을 변경합니다.
    /// </summary>
    /// <param name="newType">새로운 적 타입</param>
    public void SetEnemyType(EnemyType newType)
    {
        if (_enemyType == newType) return;

        _enemyType = newType;
        InitializeBehaviorStrategy();

        // 현재 상태가 Die가 아닐 경우 새로운 초기 상태로 설정
        if (_stateMachine.CurrentStateType != EnemyState.Die)
        {
            _stateMachine.SetState(_behaviorStrategy.GetInitialState());
        }
    }

    /// <summary>
    /// 대상을 향해 회전합니다.
    /// </summary>
    /// <param name="targetPosition">목표 위치</param>
    public void RotateToTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
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
    /// 플레이어 감지 처리를 수행합니다.
    /// </summary>
    public void DetectPlayer()
    {
        if (Player == null || IsDead) return;

        float distanceToPlayer = GetDistanceToDestination(Player.transform.position, false);

        // 행동 전략에 플레이어 감지 처리 위임
        EnemyState nextState = _behaviorStrategy.OnPlayerDetected(this, Player.transform.position, distanceToPlayer);

        // 현재 상태와 다르면 상태 변경
        if (nextState != _stateMachine.CurrentStateType)
        {
            SetState(nextState);
        }
    }

    /// <summary>
    /// 플레이어 놓침 처리를 수행합니다.
    /// </summary>
    public void LosePlayer()
    {
        if (IsDead) return;

        // 행동 전략에 플레이어 놓침 처리 위임
        EnemyState nextState = _behaviorStrategy.OnPlayerLost(this);
        SetState(nextState);
    }

    /// <summary>
    /// 공격 완료 후 처리를 수행합니다.
    /// </summary>
    public void CompleteAttack()
    {
        if (IsDead) return;

        // 행동 전략에 공격 완료 처리 위임
        EnemyState nextState = _behaviorStrategy.OnAttackComplete(this);
        SetState(nextState);
    }

    /// <summary>
    /// IDamageable 인터페이스 구현 - 데미지를 받습니다.
    /// </summary>
    /// <param name="damage">받은 데미지 정보</param>
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
        // 사망 이벤트 발생
        OnDeath?.Invoke(gameObject);

        // 오브젝트 비활성화
        gameObject.SetActive(false);
    }
    #endregion

    #region 이동 관련 메서드
    /// <summary>
    /// 지정된 위치로 NavMeshAgent 목적지를 설정합니다.
    /// </summary>
    /// <param name="destination">목적지</param>
    /// <returns>목적지 설정 성공 여부</returns>
    public bool SetDestination(Vector3 destination)
    {
        return NavMeshUtility.TrySetDestination(_agent, destination);
    }

    /// <summary>
    /// 목적지까지의 거리를 계산합니다.
    /// </summary>
    /// <param name="destination">목적지</param>
    /// <param name="useNavMeshPath">NavMesh 경로 사용 여부</param>
    /// <returns>목적지까지의 거리</returns>
    public float GetDistanceToDestination(Vector3 destination, bool useNavMeshPath = true)
    {
        return NavMeshUtility.GetDistanceTo(_agent, destination, useNavMeshPath);
    }

    /// <summary>
    /// 목적지에 도달했는지 확인합니다.
    /// </summary>
    /// <param name="destination">목적지</param>
    /// <param name="stoppingDistance">정지 거리</param>
    /// <returns>도달 여부</returns>
    public bool HasReachedDestination(Vector3 destination, float stoppingDistance = 0.1f)
    {
        return NavMeshUtility.HasReachedDestination(_agent, destination, stoppingDistance);
    }

    /// <summary>
    /// NavMeshAgent가 유효한 상태인지 확인합니다.
    /// </summary>
    /// <returns>유효성 여부</returns>
    public bool IsAgentValid()
    {
        return NavMeshUtility.IsAgentValid(_agent);
    }

    /// <summary>
    /// 대상이 시야 내에 있는지 확인합니다.
    /// </summary>
    /// <param name="targetPosition">대상 위치</param>
    /// <param name="viewAngle">시야각 (기본값 null이면 _viewAngle 사용)</param>
    /// <param name="maxDistance">최대 거리 (기본값 null이면 _findDistance 사용)</param>
    /// <param name="layerMask">레이어 마스크 (기본값 null)</param>
    /// <returns>시야 내 존재 여부</returns>
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

    #region 이벤트
    /// <summary>
    /// 적 사망 시 발생하는 이벤트 델리게이트
    /// </summary>
    /// <param name="enemy">사망한 적 게임오브젝트</param>
    public delegate void EnemyDeathHandler(GameObject enemy);

    /// <summary>
    /// 적 사망 이벤트
    /// </summary>
    public event EnemyDeathHandler OnDeath;
    #endregion
}
