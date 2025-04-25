using UnityEngine;

/// <summary>
/// 적 상태의 기본 인터페이스입니다.
/// </summary>
public interface IEnemyState
{
    /// <summary>
    /// 상태 진입 시 호출됩니다.
    /// </summary>
    void Enter();

    /// <summary>
    /// 상태 종료 시 호출됩니다.
    /// </summary>
    void Exit();

    /// <summary>
    /// 매 프레임 호출되는 업데이트 메서드입니다.
    /// </summary>
    void Update();

    /// <summary>
    /// 다른 상태로의 전환 조건을 확인합니다.
    /// </summary>
    void CheckTransitions();
}