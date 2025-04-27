using UnityEngine;

/// <summary>
/// 일반적인 패트롤 전략을 구현하는 클래스입니다.
/// </summary>
public class PatrolBehaviorStrategy : IEnemyBehaviorStrategy
{
    private float _patrolTimer = 0f;

    /// <summary>
    /// 초기 상태를 반환합니다.
    /// </summary>
    public EnemyState GetInitialState()
    {
        return EnemyState.Idle;
    }

    /// <summary>
    /// Idle 상태에서의 동작을 처리합니다.
    /// (기존 패트롤 상태의 기능 포함)
    /// </summary>
    public void OnIdle(Enemy enemy)
    {
        // 웨이포인트가 있는 경우 순찰 동작 수행
        if (enemy.EnemyPatrol != null && enemy.EnemyPatrol.HasWaypoints)
        {
            _patrolTimer += Time.deltaTime;

            // 일정 시간 대기 후 다음 웨이포인트로 이동
            if (_patrolTimer >= enemy.IdleDuration)
            {
                _patrolTimer = 0f;

                // 순찰 시작 또는 재개
                enemy.EnemyPatrol.StartPatrol(
                    enemy.Agent,
                    enemy.TraceSpeed * 0.6f, // 느린 속도로 순찰
                    enemy.StartPosition
                );
            }
        }
    }

    /// <summary>
    /// 적이 플레이어를 감지했을 때의 처리를 정의합니다.
    /// </summary>
    public EnemyState OnPlayerDetected(Enemy enemy, Vector3 playerPosition, float distanceToPlayer)
    {
        // 감지 거리 내에 있고 시야에 있는지 확인
        if (distanceToPlayer <= enemy.FindDistance &&
            enemy.IsTargetInSight(playerPosition, enemy.ViewAngle, enemy.FindDistance))
        {
            return EnemyState.Trace;
        }

        return enemy.GetComponent<EnemyStateMachine>().CurrentStateType; // 현재 상태 유지
    }

    /// <summary>
    /// 플레이어를 놓쳤을 때의 처리를 정의합니다.
    /// </summary>
    public EnemyState OnPlayerLost(Enemy enemy)
    {
        // 순찰 중이었다면 순찰 중지
        if (enemy.EnemyPatrol != null && enemy.EnemyPatrol.IsPatrolling)
        {
            enemy.EnemyPatrol.StopPatrol();
        }

        return EnemyState.Return; // 플레이어를 놓치면 원위치로 복귀
    }

    /// <summary>
    /// 공격 완료 후의 처리를 정의합니다.
    /// </summary>
    public EnemyState OnAttackComplete(Enemy enemy)
    {
        // 플레이어가 여전히 공격 범위 내에 있는지 확인
        if (enemy.Player != null)
        {
            float distanceToPlayer = enemy.GetDistanceToDestination(enemy.Player.transform.position, true);
            if (distanceToPlayer <= enemy.AttackDistance &&
                enemy.IsTargetInSight(enemy.Player.transform.position))
            {
                return EnemyState.Attack; // 계속 공격
            }
            else if (distanceToPlayer <= enemy.FindDistance)
            {
                return EnemyState.Trace; // 추적
            }
        }

        return EnemyState.Return; // 복귀
    }
}