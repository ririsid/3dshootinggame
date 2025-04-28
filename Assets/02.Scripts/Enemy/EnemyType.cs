/// <summary>
/// 적의 행동 타입을 정의하는 열거형입니다.
/// </summary>
public enum EnemyType
{
    /// <summary>
    /// 정적 타입: 가만히 서있는 타입
    /// </summary>
    Static,

    /// <summary>
    /// 순찰 타입: 지정된 경로를 따라 순찰하는 타입
    /// </summary>
    Patrol,

    /// <summary>
    /// 추적 타입: 항상 플레이어를 추적하는 타입
    /// </summary>
    Chase
}