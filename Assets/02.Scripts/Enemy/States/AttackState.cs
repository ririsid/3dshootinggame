using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적의 공격 상태를 구현하는 클래스입니다.
/// </summary>
public class AttackState : IEnemyState
{
    private Enemy _enemy;
    private Coroutine _attackCoroutine;
    private EnemyCombat _enemyCombat;

    // 공격 관련 변수
    private float _positionUpdateTimer = 0f;

    public AttackState(Enemy enemy)
    {
        _enemy = enemy;
        _enemyCombat = enemy.GetComponent<EnemyCombat>();
    }

    public void Enter()
    {
        _positionUpdateTimer = 0f;

        // 공격 시 NavMeshAgent 정지
        if (_enemy.IsAgentValid())
        {
            _enemy.Agent.isStopped = true;
            _enemy.Agent.ResetPath();
        }

        _attackCoroutine = _enemy.StartCoroutine(AttackCoroutine());
    }

    public void Exit()
    {
        if (_attackCoroutine != null)
        {
            _enemy.StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }

        // 공격 상태 종료 시 NavMeshAgent 재활성화
        if (_enemy.IsAgentValid())
        {
            _enemy.Agent.isStopped = false;
        }
    }

    public void Update()
    {
        // 코루틴에서 처리하지만, 상태 전환은 더 자주 확인
        CheckTransitions();
    }

    public void CheckTransitions()
    {
        if (_enemy.Player == null) return;

        // NavMeshUtility를 사용하여 더 정확한 경로 거리 계산
        float distanceToPlayer = _enemy.GetDistanceToDestination(_enemy.Player.transform.position, true);

        if (distanceToPlayer > _enemy.AttackDistance * 1.2f) // 약간의 여유를 두고 추적으로 전환
        {
            _enemy.SetState(Enemy.EnemyState.Trace);
        }
        else if (!_enemy.IsTargetInSight(_enemy.Player.transform.position, 120f, _enemy.AttackDistance * 1.5f))
        {
            // 시야에서 벗어난 경우
            _enemy.SetState(Enemy.EnemyState.Trace);
        }
    }

    private IEnumerator AttackCoroutine()
    {
        while (true)
        {
            _positionUpdateTimer += Time.deltaTime;

            // 플레이어가 없는 경우 처리
            if (_enemy.Player == null)
            {
                _enemy.SetState(Enemy.EnemyState.Idle);
                yield break;
            }

            Vector3 playerPosition = _enemy.Player.transform.position;

            // 타겟 방향으로 회전
            _enemy.RotateToTarget(playerPosition);

            // 공격 시도
            if (_enemyCombat.CanAttack)
            {
                // 공격 전 시야 체크 - NavMeshUtility 사용
                if (_enemy.IsTargetInSight(playerPosition, 120f, _enemy.AttackDistance))
                {
                    // 시각적 디버깅 (개발 빌드에서만)
                    if (Debug.isDebugBuild)
                    {
                        Debug.DrawLine(_enemy.transform.position, playerPosition, Color.red, 0.5f);
                    }

                    _enemyCombat.TryAttack(_enemy.Player);
                }
            }

            // 일정 간격으로 위치 조정 (플레이어에게 더 가까이 접근)
            if (_positionUpdateTimer >= _enemy.AttackPositionUpdateInterval)
            {
                _positionUpdateTimer = 0f;

                // NavMeshUtility를 사용하여 경로 거리 계산
                float distanceToPlayer = _enemy.GetDistanceToDestination(playerPosition, true);
                float optimalAttackDistance = _enemy.AttackDistance * 0.7f; // 최적 공격 거리는 공격 거리의 70%

                // 공격 거리 재조정 로직
                if (distanceToPlayer > optimalAttackDistance && _enemy.IsAgentValid())
                {
                    // 플레이어 주변에서 최적의 공격 위치 찾기
                    FindAndMoveToOptimalAttackPosition(playerPosition, optimalAttackDistance);
                }
                else
                {
                    // 플레이어 주변 위치가 변경되었는지 확인하고 필요 시 전략적 위치 조정
                    CheckAndAdjustAttackPosition(playerPosition);
                }
            }

            yield return null;
        }
    }

    /// <summary>
    /// 플레이어 주변에서 최적의 공격 위치를 찾아 이동합니다.
    /// </summary>
    private void FindAndMoveToOptimalAttackPosition(Vector3 playerPosition, float optimalDistance)
    {
        if (!_enemy.IsAgentValid()) return;

        // 현재 위치에서 플레이어까지의 직선 방향
        Vector3 directDirection = (playerPosition - _enemy.transform.position).normalized;

        // 여러 각도에서 가능한 위치 시도 (전방, 약간 좌측, 약간 우측)
        Vector3[] possibleDirections = new Vector3[]
        {
            directDirection,                                              // 직선 방향
            Quaternion.Euler(0, -30, 0) * directDirection,                // 약간 좌측
            Quaternion.Euler(0, 30, 0) * directDirection,                 // 약간 우측
            Quaternion.Euler(0, -60, 0) * directDirection,                // 더 좌측
            Quaternion.Euler(0, 60, 0) * directDirection                  // 더 우측
        };

        float bestPathLength = float.MaxValue;
        Vector3 bestPosition = _enemy.transform.position;
        bool foundValidPosition = false;

        // 각 방향에서 최적의 위치 찾기
        foreach (Vector3 direction in possibleDirections)
        {
            // 목표 위치 계산
            Vector3 targetPosition = playerPosition - direction * optimalDistance;

            // NavMesh 상의 유효한 위치 확보
            Vector3 navMeshPosition = NavMeshUtility.GetNavMeshPosition(targetPosition, 2f);

            // 이 위치가 도달 가능한지 확인하고 경로 길이 계산
            if (NavMeshUtility.CanReachDestination(_enemy.transform.position, navMeshPosition, out float pathLength))
            {
                // 시야 체크 - 이 위치에서 플레이어를 볼 수 있는지
                Vector3 toPlayerDir = (playerPosition - navMeshPosition).normalized;
                if (Physics.Raycast(navMeshPosition, toPlayerDir, out RaycastHit hit, _enemy.AttackDistance * 1.5f))
                {
                    if (hit.transform.gameObject == _enemy.Player)
                    {
                        // 더 짧은 경로를 선호
                        if (pathLength < bestPathLength)
                        {
                            bestPathLength = pathLength;
                            bestPosition = navMeshPosition;
                            foundValidPosition = true;
                        }
                    }
                }
            }
        }

        // 유효한 위치를 찾았으면 이동
        if (foundValidPosition)
        {
            // 시각적 디버깅 (개발 빌드에서만)
            if (Debug.isDebugBuild)
            {
                Debug.DrawLine(_enemy.transform.position, bestPosition, Color.green, 1f);
                NavMeshUtility.DebugDrawPath(_enemy.transform.position, bestPosition, Color.yellow, 1f);
            }

            // 이동 설정
            _enemy.Agent.isStopped = false;
            _enemy.SetDestination(bestPosition);

            // 짧은 시간 동안 이동 후 정지
            _enemy.StartCoroutine(StopAfterDelay(0.3f));
        }
    }

    /// <summary>
    /// 플레이어의 위치 변화를 감지하고 필요시 공격 위치를 조정합니다.
    /// </summary>
    private void CheckAndAdjustAttackPosition(Vector3 playerPosition)
    {
        if (!_enemy.IsAgentValid()) return;

        // 플레이어가 이동한 방향 감지
        Vector3 playerMoveDirection = Vector3.zero;

        // 공격 전략 조정 - 플레이어 주변을 서서히 이동하며 전략적 위치 선점
        if (Random.value < 0.3f) // 30% 확률로 위치 조정 시도
        {
            // 플레이어 주변을 약간 회전하는 위치 계산
            float angle = Random.Range(-90f, 90f);
            Vector3 currentToPlayer = (playerPosition - _enemy.transform.position).normalized;
            Vector3 rotatedDirection = Quaternion.Euler(0, angle, 0) * currentToPlayer;

            float currentDistance = Vector3.Distance(_enemy.transform.position, playerPosition);
            Vector3 newPosition = playerPosition - rotatedDirection * currentDistance;

            // NavMesh 상의 유효한 위치 확보
            Vector3 navMeshPosition = NavMeshUtility.GetNavMeshPosition(newPosition, 2f);

            // 이 위치가 도달 가능하고 공격 거리 내인지 확인
            if (NavMeshUtility.CanReachDestination(_enemy.transform.position, navMeshPosition, out float pathLength) &&
                pathLength < _enemy.AttackDistance * 1.5f)
            {
                // 이동 설정
                _enemy.Agent.isStopped = false;
                _enemy.SetDestination(navMeshPosition);

                // 짧은 시간 동안 이동 후 정지
                _enemy.StartCoroutine(StopAfterDelay(0.2f));
            }
        }
    }

    /// <summary>
    /// 지정된 시간 후 NavMeshAgent를 정지시킵니다.
    /// </summary>
    private IEnumerator StopAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_enemy.IsAgentValid())
        {
            _enemy.Agent.isStopped = true;
            _enemy.Agent.ResetPath();
        }
    }
}