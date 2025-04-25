using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class EnemyPatrol : MonoBehaviour
{
    #region 직렬화 필드
    [Header("순찰 설정")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waitTime = 2f;
    [SerializeField] private float _rotationSpeed = 5f;
    #endregion

    #region 비공개 필드
    private CharacterController _characterController;
    private float _currentMoveSpeed;
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

    #region 비공개 메서드
    private IEnumerator Patrol_Coroutine()
    {
        while (_isPatrolling)
        {
            // CharacterController 유효성 검사
            if (_characterController == null || !_characterController.enabled || !_characterController.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("순찰 중 CharacterController가 유효하지 않아 순찰을 중지합니다.");
                StopPatrolInternal(); // 내부 중지 함수 호출
                yield break;
            }

            Vector3 direction = (_targetPosition - transform.position);
            direction.y = 0; // Y축 이동 무시

            // 목표 지점 도달 체크
            if (direction.magnitude < _characterController.radius)
            {
                // 대기 시간 전에 순찰 중지 여부 확인
                if (!_isPatrolling) yield break;
                yield return new WaitForSeconds(_waitTime);
                // 대기 시간 후에도 순찰 중지 여부 확인
                if (!_isPatrolling) yield break;

                // 목표 지점 도달 후 처리
                if (_isSinglePointPatrol)
                {
                    // 단일 지점 순찰: 목표 도달 후 추가 행동 없음 (계속 대기 상태 유지)
                }
                else // 일반 웨이포인트 순찰
                {
                    // 다음 웨이포인트 설정
                    _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                    _targetPosition = _waypoints[_currentWaypointIndex].position;

                    // 다음 웨이포인트 설정 후 순찰 중지 여부 확인
                    if (!_isPatrolling) yield break;
                }
            }
            // 목표 지점으로 이동 및 회전
            else
            {
                // 이동 전에 순찰 중지 여부 확인
                if (!_isPatrolling) yield break;

                // 이동 계산 및 적용
                Vector3 move = direction.normalized * _currentMoveSpeed * Time.deltaTime;
                // 중력 적용
                _characterController.Move(move + Physics.gravity * Time.deltaTime);

                // 회전 전에 순찰 중지 여부 확인
                if (!_isPatrolling) yield break;

                // 목표 방향으로 회전 (0벡터가 아닐 때만)
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                }
            }

            // 다음 프레임까지 대기
            yield return null;
        }
    }

    /// <summary>
    /// 내부적으로 순찰을 중지하고 상태를 정리하는 메서드.
    /// Coroutine 내부 또는 외부 StopPatrol에서 호출됩니다.
    /// </summary>
    private void StopPatrolInternal()
    {
        if (!_isPatrolling) return; // 이미 중지된 경우 무시

        _isPatrolling = false; // 순찰 상태 플래그 비활성화

        // 코루틴이 실행 중이면 중지
        if (_patrolCoroutine != null)
        {
            StopCoroutine(_patrolCoroutine);
            _patrolCoroutine = null;
        }

        // CharacterController 참조 해제
        _characterController = null;
        // Debug.Log("Patrol Stopped Internally"); // 디버깅 로그 (필요시 활성화)
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 순찰을 시작하거나 재개합니다. Enemy 스크립트에서 호출됩니다.
    /// </summary>
    /// <param name="controller">이동에 사용할 CharacterController</param>
    /// <param name="moveSpeed">순찰 시 사용할 이동 속도</param>
    /// <param name="startPositionOverride">웨이포인트가 없을 경우 이동할 목표 지점 (보통 Enemy의 시작 위치)</param>
    public void StartPatrol(CharacterController controller, float moveSpeed, Vector3? startPositionOverride = null)
    {
        if (_isPatrolling) return; // 이미 순찰 중이면 무시

        _characterController = controller;
        if (_characterController == null)
        {
            Debug.LogError("StartPatrol: CharacterController가 제공되지 않았습니다!");
            return;
        }
        _currentMoveSpeed = moveSpeed;

        _isSinglePointPatrol = false; // 단일 지점 순찰 플래그 초기화

        // 웨이포인트 설정 또는 단일 목표 지점 설정
        if (!HasWaypoints)
        {
            if (startPositionOverride.HasValue)
            {
                _targetPosition = startPositionOverride.Value;
                _isSinglePointPatrol = true; // 단일 지점 순찰 모드 활성화
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
        }

        _isPatrolling = true; // 순찰 상태 플래그 활성화
        // 기존 코루틴이 있다면 중지
        if (_patrolCoroutine != null)
        {
            StopCoroutine(_patrolCoroutine);
        }
        // 순찰 코루틴 시작
        _patrolCoroutine = StartCoroutine(Patrol_Coroutine());
        // Debug.Log("Patrol Started"); // 디버깅 로그 (필요시 활성화)
    }

    /// <summary>
    /// 외부에서 순찰을 중지시킬 때 호출됩니다.
    /// </summary>
    public void StopPatrol()
    {
        // Debug.Log("StopPatrol Called Externally"); // 디버깅 로그 (필요시 활성화)
        StopPatrolInternal(); // 내부 중지 함수 호출
    }
    #endregion

    #region Unity 이벤트 함수
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
