using UnityEngine;

/// <summary>
/// 항상 플레이어를 추적하는 행동 전략을 구현하는 클래스입니다.
/// </summary>
public class ChaseBehaviorStrategy : IEnemyBehaviorStrategy
{
    #region 필드
    /// <summary>
    /// 마지막으로 알려진 플레이어 위치
    /// </summary>
    private Vector3 _lastKnownPlayerPosition;

    /// <summary>
    /// 주변 스캔 타이머
    /// </summary>
    private float _scanTimer = 0f;
    #endregion

    #region IEnemyBehaviorStrategy 구현
    /// <summary>
    /// 초기 상태를 반환합니다.
    /// </summary>
    /// <returns>초기 상태</returns>
    public EnemyState GetInitialState()
    {
        return EnemyState.Idle; // 초기 상태는 대기
    }

    /// <summary>
    /// Idle 상태에서의 동작을 처리합니다.
    /// </summary>
    /// <param name="enemy">적 객체</param>
    public void OnIdle(Enemy enemy)
    {
        // 플레이어 존재 여부 확인
        if (enemy.Player != null)
        {
            _scanTimer += Time.deltaTime;

            // 주기적으로 플레이어 방향 확인
            if (_scanTimer >= 0.5f)
            {
                _scanTimer = 0f;

                // 플레이어가 시야 내에 있는지 확인
                float distanceToPlayer = enemy.GetDistanceToDestination(enemy.Player.transform.position, false);

                // 시야 체크
                if (enemy.IsTargetInSight(enemy.Player.transform.position, enemy.ViewAngle, enemy.FindDistance))
                {
                    _lastKnownPlayerPosition = enemy.Player.transform.position;
                    enemy.SetState(EnemyState.Trace);
                    return;
                }

                // 거리가 일정 범위 내에 있으면 플레이어 감지 확률 증가
                if (distanceToPlayer < enemy.FindDistance)
                {
                    float detectionChance = 1 - (distanceToPlayer / enemy.FindDistance);
                    if (Random.value < detectionChance)
                    {
                        _lastKnownPlayerPosition = enemy.Player.transform.position;
                        enemy.SetState(EnemyState.Trace);
                        return;
                    }
                }
            }
        }

        // 대기 상태에서도 주변을 살피는 회전 동작
        enemy.RotateToTarget(
            enemy.transform.position +
            Quaternion.Euler(0, Time.time * 20f, 0) * Vector3.forward * 10f
        );
    }

    /// <summary>
    /// 적이 플레이어를 감지했을 때의 처리를 정의합니다.
    /// </summary>
    /// <param name="enemy">적 객체</param>
    /// <param name="playerPosition">플레이어 위치</param>
    /// <param name="distanceToPlayer">플레이어까지의 거리</param>
    /// <returns>감지 후 전환할 상태</returns>
    public EnemyState OnPlayerDetected(Enemy enemy, Vector3 playerPosition, float distanceToPlayer)
    {
        // 시야 체크만으로 플레이어 감지 (거리 제한 X)
        if (enemy.IsTargetInSight(playerPosition, enemy.ViewAngle))
        {
            _lastKnownPlayerPosition = playerPosition;
            return EnemyState.Trace;
        }

        // 플레이어가 매우 가까이 있는 경우 무조건 감지
        if (distanceToPlayer < enemy.FindDistance)
        {
            _lastKnownPlayerPosition = playerPosition;
            return EnemyState.Trace;
        }

        return enemy.GetComponent<EnemyStateMachine>().CurrentStateType;
    }

    /// <summary>
    /// 플레이어를 놓쳤을 때의 처리를 정의합니다.
    /// </summary>
    /// <param name="enemy">적 객체</param>
    /// <returns>플레이어를 놓친 후 전환할 상태</returns>
    public EnemyState OnPlayerLost(Enemy enemy)
    {
        // 마지막으로 플레이어가 있던 위치로 이동
        if (_lastKnownPlayerPosition != Vector3.zero)
        {
            enemy.SetDestination(_lastKnownPlayerPosition);

            // 이동 중인 상태로 유지 (추적 상태 계속 유지)
            return EnemyState.Trace;
        }

        // 마지막 위치 정보가 없는 경우 Idle 상태로 전환
        return EnemyState.Idle;
    }

    /// <summary>
    /// 공격 완료 후의 처리를 정의합니다.
    /// </summary>
    /// <param name="enemy">적 객체</param>
    /// <returns>공격 종료 후 전환할 상태</returns>
    public EnemyState OnAttackComplete(Enemy enemy)
    {
        // 플레이어가 여전히 공격 범위 내에 있는지 확인
        if (enemy.Player != null)
        {
            float distanceToPlayer = enemy.GetDistanceToDestination(enemy.Player.transform.position);

            if (distanceToPlayer <= enemy.AttackDistance &&
                enemy.IsTargetInSight(enemy.Player.transform.position))
            {
                return EnemyState.Attack; // 계속 공격
            }
            else
            {
                _lastKnownPlayerPosition = enemy.Player.transform.position;
                return EnemyState.Trace; // 항상 추적으로 전환
            }
        }

        // 플레이어 참조가 없는 경우
        if (_lastKnownPlayerPosition != Vector3.zero)
        {
            return EnemyState.Trace; // 마지막 알려진 위치로 추적
        }

        return EnemyState.Idle; // 정보가 없으면 대기 상태로
    }
    #endregion
}