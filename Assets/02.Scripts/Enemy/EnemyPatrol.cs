using UnityEngine;
using System.Collections;
using UnityEngine.AI;

/// <summary>
/// 적 캐릭터의 순찰 동작을 관리하는 컴포넌트입니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyPatrol : MonoBehaviour
{
    #region 직렬화 필드
    [Header("순찰 설정")]
    /// <summary>
    /// 순찰 경로 상의 웨이포인트 목록
    /// </summary>
    [SerializeField] private Transform[] _waypoints;

    /// <summary>
    /// 웨이포인트 도착 후 대기 시간
    /// </summary>
    [SerializeField] private float _waitTime = 2f;

    /// <summary>
    /// 웨이포인트 도착 판정 거리
    /// </summary>
    [SerializeField] private float _stoppingDistance = 0.1f;
    #endregion

    #region 비공개 필드
    /// <summary>
    /// NavMesh 에이전트 컴포넌트
    /// </summary>
    private NavMeshAgent _navMeshAgent;

    /// <summary>
    /// 현재 목표 웨이포인트 인덱스
    /// </summary>
    private int _currentWaypointIndex = 0;

    /// <summary>
    /// 순찰 중 여부
    /// </summary>
    private bool _isPatrolling = false;

    /// <summary>
    /// 현재 실행 중인 순찰 코루틴
    /// </summary>
    private Coroutine _patrolCoroutine;

    /// <summary>
    /// 현재 목표 위치
    /// </summary>
    private Vector3 _targetPosition;

    /// <summary>
    /// 단일 지점 순찰 여부 (웨이포인트가 없을 때)
    /// </summary>
    private bool _isSinglePointPatrol = false;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 순찰 중인지 여부
    /// </summary>
    public bool IsPatrolling => _isPatrolling;

    /// <summary>
    /// 웨이포인트가 존재하는지 여부
    /// </summary>
    public bool HasWaypoints => _waypoints != null && _waypoints.Length > 0;
    #endregion

    #region 코루틴
    /// <summary>
    /// 순찰 동작을 처리하는 코루틴
    /// </summary>
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
                    // 단일 지점 순찰인 경우 대기만 하고 다시 목표 지점 확인
                    continue;
                }
                else
                {
                    // 다음 웨이포인트로 인덱스 변경
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
        // 이미 순찰 중이면 중복 시작 방지
        if (_isPatrolling) return;

        _navMeshAgent = agent;
        _navMeshAgent.speed = moveSpeed;
        _isPatrolling = true;

        // 웨이포인트가 없는 경우
        if (!HasWaypoints)
        {
            _isSinglePointPatrol = true;

            // 시작 위치 오버라이드가 제공된 경우 해당 위치를 목표로 사용
            if (startPositionOverride.HasValue)
            {
                _targetPosition = startPositionOverride.Value;

                // NavMesh 상의 유효한 위치 확인
                Vector3 validPosition = NavMeshUtility.GetNavMeshPosition(_targetPosition);
                if (validPosition != _targetPosition)
                {
                    if (Debug.isDebugBuild)
                    {
                        Debug.LogWarning("제공된 시작 위치가 NavMesh 상에 없습니다. 가장 가까운 NavMesh 위치를 사용합니다.");
                    }
                    _targetPosition = validPosition;
                }

                // 목적지 설정
                bool success = NavMeshUtility.TrySetDestination(_navMeshAgent, _targetPosition);
                if (!success && Debug.isDebugBuild)
                {
                    Debug.LogWarning($"NavMeshAgent가 목적지({_targetPosition})로 경로를 찾을 수 없습니다.");
                    return; // 순찰 시작 불가
                }
            }
            else
            {
                if (Debug.isDebugBuild)
                {
                    Debug.LogError("웨이포인트가 없고 startPositionOverride도 제공되지 않아 순찰을 시작할 수 없습니다.");
                }
                _isPatrolling = false;
                return; // 순찰 시작 불가
            }
        }
        else
        {
            // 첫 번째 웨이포인트 설정
            _isSinglePointPatrol = false;
            _currentWaypointIndex = 0;
            _targetPosition = _waypoints[_currentWaypointIndex].position;

            // NavMesh 상의 유효한 위치 확인
            Vector3 validPosition = NavMeshUtility.GetNavMeshPosition(_targetPosition);
            if (validPosition != _targetPosition)
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
                    if (Debug.isDebugBuild)
                    {
                        Debug.LogError("유효한 웨이포인트를 찾을 수 없습니다. 순찰을 시작할 수 없습니다.");
                    }
                    _isPatrolling = false;
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
                if (Debug.isDebugBuild)
                {
                    Debug.LogWarning($"NavMeshAgent가 목적지({_targetPosition})로 경로를 찾을 수 없습니다.");
                }
                _isPatrolling = false;
                return; // 순찰 시작 불가
            }
        }

        // 코루틴 시작
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
    /// <returns>현재 목표 위치</returns>
    public Vector3 GetCurrentTargetPosition()
    {
        return _targetPosition;
    }
    #endregion

    #region Gizmos 이벤트 함수
    /// <summary>
    /// 에디터에서 웨이포인트 경로를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (_waypoints == null || _waypoints.Length < 2) return;

        // 순찰 경로 그리기
        Gizmos.color = Color.yellow;
        for (int i = 0; i < _waypoints.Length; i++)
        {
            if (_waypoints[i] == null) continue;

            // 현재 웨이포인트에 구체 표시
            Gizmos.DrawSphere(_waypoints[i].position, 0.3f);

            // 다음 웨이포인트와 연결
            if (i < _waypoints.Length - 1 && _waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
            }
        }

        // 마지막 웨이포인트와 첫 웨이포인트 연결 (순환 경로)
        if (_waypoints.Length > 1 && _waypoints[0] != null && _waypoints[_waypoints.Length - 1] != null)
        {
            Gizmos.color = new Color(1f, 0.7f, 0f); // 약간 다른 색상으로 구분
            Gizmos.DrawLine(_waypoints[_waypoints.Length - 1].position, _waypoints[0].position);
        }
    }
    #endregion
}
