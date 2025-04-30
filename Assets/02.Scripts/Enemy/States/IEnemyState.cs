/// <summary>
/// 적 상태 머신에서 사용되는 상태 인터페이스입니다.
/// 모든 적 상태 클래스는 이 인터페이스를 구현해야 합니다.
/// </summary>
public interface IEnemyState
{
    /// <summary>
    /// 상태에 진입할 때 호출됩니다.
    /// </summary>
    void Enter();

    /// <summary>
    /// 상태에서 나갈 때 호출됩니다.
    /// </summary>
    void Exit();

    /// <summary>
    /// 매 프레임 호출되는 업데이트 함수입니다.
    /// </summary>
    void Update();

    /// <summary>
    /// 다른 상태로의 전환 조건을 확인합니다.
    /// </summary>
    void CheckTransitions();
}