using UnityEngine;

/// <summary>
/// 적의 행동 전략을 정의하는 인터페이스입니다.
/// </summary>
public interface IEnemyBehaviorStrategy
{
    /// <summary>
    /// 적의 초기 상태를 반환합니다.
    /// </summary>
    EnemyState GetInitialState();

    /// <summary>
    /// 대기 상태에서의 행동을 정의합니다.
    /// </summary>
    /// <param name="enemy">적 객체</param>
    void OnIdle(Enemy enemy);

    /// <summary>
    /// 적이 플레이어를 감지했을 때의 처리를 정의합니다.
    /// </summary>
    /// <param name="enemy">적 객체</param>
    /// <param name="playerPosition">플레이어 위치</param>
    /// <param name="distanceToPlayer">플레이어까지의 거리</param>
    /// <returns>감지 후 전환할 상태</returns>
    EnemyState OnPlayerDetected(Enemy enemy, Vector3 playerPosition, float distanceToPlayer);

    /// <summary>
    /// 적이 플레이어를 놓쳤을 때의 처리를 정의합니다.
    /// </summary>
    /// <param name="enemy">적 객체</param>
    /// <returns>플레이어를 놓친 후 전환할 상태</returns>
    EnemyState OnPlayerLost(Enemy enemy);

    /// <summary>
    /// 적이 공격 상태를 종료한 후의 처리를 정의합니다.
    /// </summary>
    /// <param name="enemy">적 객체</param>
    /// <returns>공격 종료 후 전환할 상태</returns>
    EnemyState OnAttackComplete(Enemy enemy);
}