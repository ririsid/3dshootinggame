using UnityEngine;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    #region 직렬화 필드
    [Header("순찰 설정")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waitTime = 2f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _stoppingDistance = 0.1f;
    #endregion

    #region 비공개 필드
    private NavMeshAgent _navMeshAgent;
    private int _currentWaypointIndex = 0;
    private bool _isPatrolling = false;
    private Coroutine _patrolCoroutine;
    private Vector3 _targetPosition;
    private bool _isSinglePointPatrol = false;
    #endregion

    #region 프로퍼티
    public bool IsPatrolling => _isPatrolling;
    public bool HasWaypoints => _waypoints != null && _waypoints.Length > 0;
    #endregion

    #region 코루틴
    private IEnumerator PatrolCoroutine()
    {
        while (_isPatrolling)
        {
            // NavMeshAgent 유효성 검사
            if (!NavMeshUtility.IsAgentValid(_navMeshAgent))
            {
                StopPatrolInternal();
                yield break;
            }

            // 목표 지점 도달 체크
            if (NavMeshUtility.HasReachedDestination(_navMeshAgent, _targetPosition, _stoppingDistance))
            {
                // 대기 시간 전에 순찰 중지 여부 확인
                if (!_isPatrolling) yield break;
                yield return new WaitForSeconds(_waitTime);
                // 대기 시간 후에도 순찰 중지 여부 확인
                if (!_isPatrolling) yield break;

                // 목표 지점 도달 후 처리
                if (_isSinglePointPatrol)
                {
                    // 단일 지점 순찰인 경우 제자리에서 회전 또는 대기
                    _navMeshAgent.isStopped = true;
                    yield return new WaitForSeconds(_waitTime);

                    // 계속 순찰해야 한다면 다시 같은 위치로 이동 시도
                    if (_isPatrolling)
                    {
                        _navMeshAgent.isStopped = false;
                        NavMeshUtility.TrySetDestination(_navMeshAgent, _targetPosition);
                    }
                }
                else
                {
                    // 다음 웨이포인트 설정
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                    _targetPosition = _waypoints[_currentWaypointIndex].position;

                    // 다음 웨이포인트 설정 후 순찰 중지 여부 확인
                    if (!_isPatrolling) yield break;

                    // 다음 목적지 설정
                    bool success = NavMeshUtility.TrySetDestination(_navMeshAgent, _targetPosition);
                    if (!success && Debug.isDebugBuild)
                    {
                        Debug.LogWarning($"다음 웨이포인트({_currentWaypointIndex})로 경로 설정 실패");
                    }
                }
            }

            // 다음 프레임까지 대기
            yield return null;
        }
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 내부적으로 순찰을 중지하고 상태를 정리하는 메서드.
    /// Coroutine 내부 또는 외부 StopPatrol에서 호출됩니다.
    /// </summary>
    private void StopPatrolInternal()
    {
        _isPatrolling = false;

        if (_patrolCoroutine != null)
        {
            StopCoroutine(_patrolCoroutine);
            _patrolCoroutine = null;
        }

        if (NavMeshUtility.IsAgentValid(_navMeshAgent))
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.ResetPath();
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 순찰을 시작하거나 재개합니다. Enemy 스크립트에서 호출됩니다.
    /// </summary>
    /// <param name="agent">이동에 사용할 NavMeshAgent</param>
    /// <param name="moveSpeed">순찰 시 사용할 이동 속도</param>
    /// <param name="startPositionOverride">웨이포인트가 없을 경우 이동할 목표 지점 (보통 Enemy의 시작 위치)</param>
    public void StartPatrol(NavMeshAgent agent, float moveSpeed, Vector3? startPositionOverride = null)
    {
        if (_isPatrolling) return; // 이미 순찰 중이면 무시

        _navMeshAgent = agent;
        if (!NavMeshUtility.IsAgentValid(_navMeshAgent))
        {
            Debug.LogError("StartPatrol: 유효한 NavMeshAgent가 제공되지 않았습니다!");
            return;
        }

        // NavMeshAgent 설정
        _navMeshAgent.speed = moveSpeed;
        _navMeshAgent.isStopped = false;

        _isSinglePointPatrol = false; // 단일 지점 순찰 플래그 초기화

        // 웨이포인트 설정 또는 단일 목표 지점 설정
        if (!HasWaypoints)
        {
            if (startPositionOverride.HasValue)
            {
                _targetPosition = startPositionOverride.Value;
                _isSinglePointPatrol = true; // 단일 지점 순찰 모드 활성화

                // NavMesh 상의 유효한 위치 확인
                Vector3 validPosition = NavMeshUtility.GetNavMeshPosition(_targetPosition);

                // NavMesh에 유효한 위치가 없다면 현재 위치 사용
                if (validPosition == _targetPosition)
                {
                    validPosition = NavMeshUtility.GetNavMeshPosition(_navMeshAgent.transform.position);
                    if (Debug.isDebugBuild)
                    {
                        Debug.LogWarning($"제공된 위치가 NavMesh 상에 없어서 현재 위치({validPosition})를 사용합니다.");
                    }
                }

                _targetPosition = validPosition;

                // 목적지 설정
                bool success = NavMeshUtility.TrySetDestination(_navMeshAgent, _targetPosition);
                if (!success)
                {
                    Debug.LogWarning($"NavMeshAgent가 목적지({_targetPosition})로 경로를 찾을 수 없습니다.");
                    return; // 순찰 시작 불가
                }
            }
            else
            {
                Debug.LogError("웨이포인트가 없고 startPositionOverride도 제공되지 않아 순찰을 시작할 수 없습니다.");
                return; // 순찰 시작 불가
            }
        }
        else
        {
            // 첫 번째 웨이포인트 설정
            _currentWaypointIndex = 0;
            _targetPosition = _waypoints[_currentWaypointIndex].position;

            // NavMesh 상의 유효한 위치 확인
            Vector3 validPosition = NavMeshUtility.GetNavMeshPosition(_targetPosition);
            if (validPosition == _targetPosition)
            {
                if (Debug.isDebugBuild)
                {
                    Debug.LogWarning("웨이포인트가 NavMesh 상에 없습니다. 다음 웨이포인트로 시도합니다.");
                }

                // 다른 웨이포인트 시도
                bool foundValid = false;
                for (int i = 1; i < _waypoints.Length; i++)
                {
                    _currentWaypointIndex = i;
                    _targetPosition = _waypoints[i].position;
                    validPosition = NavMeshUtility.GetNavMeshPosition(_targetPosition);
                    if (validPosition != _targetPosition)
                    {
                        foundValid = true;
                        _targetPosition = validPosition;
                        break;
                    }
                }

                if (!foundValid)
                {
                    Debug.LogError("유효한 웨이포인트를 찾을 수 없습니다. 순찰을 시작할 수 없습니다.");
                    return; // 순찰 시작 불가
                }
            }
            else
            {
                _targetPosition = validPosition;
            }

            // 목적지 설정
            bool success = NavMeshUtility.TrySetDestination(_navMeshAgent, _targetPosition);
            if (!success)
            {
                Debug.LogWarning("NavMeshAgent가 웨이포인트로 경로를 찾을 수 없습니다.");
                return; // 순찰 시작 불가
            }
        }

        _isPatrolling = true; // 순찰 상태 플래그 활성화
        // 기존 코루틴이 있다면 중지
        if (_patrolCoroutine != null)
        {
            StopCoroutine(_patrolCoroutine);
        }
        // 순찰 코루틴 시작
        _patrolCoroutine = StartCoroutine(PatrolCoroutine());
    }

    /// <summary>
    /// 외부에서 순찰을 중지시킬 때 호출됩니다.
    /// </summary>
    public void StopPatrol()
    {
        StopPatrolInternal(); // 내부 중지 함수 호출
    }

    /// <summary>
    /// 현재 순찰 목표 위치를 반환합니다.
    /// </summary>
    public Vector3 GetCurrentTargetPosition()
    {
        return _targetPosition;
    }
    #endregion

    #region Gizmos 이벤트 함수
    // Gizmos를 사용하여 순찰 경로 시각화 (Editor에서만 보임)
    private void OnDrawGizmosSelected()
    {
        if (!HasWaypoints || _waypoints.Length < 2) return;

        Gizmos.color = Color.green;
        Vector3 previousWaypoint = _waypoints[0].position;

        for (int i = 0; i < _waypoints.Length; i++)
        {
            Gizmos.DrawWireSphere(_waypoints[i].position, 0.3f);
            if (i > 0)
            {
                Gizmos.DrawLine(previousWaypoint, _waypoints[i].position);
            }
            previousWaypoint = _waypoints[i].position;
        }
        Gizmos.DrawLine(previousWaypoint, _waypoints[0].position);
    }
    #endregion
}
