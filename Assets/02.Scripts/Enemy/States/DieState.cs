using System.Collections;
using UnityEngine;

/// <summary>
/// 적의 사망 상태를 구현하는 클래스입니다.
/// </summary>
public class DieState : IEnemyState
{
    #region 필드
    /// <summary>
    /// Enemy 컴포넌트 참조
    /// </summary>
    private Enemy _enemy;

    /// <summary>
    /// 사망 코루틴 참조
    /// </summary>
    private Coroutine _deathCoroutine;
    #endregion

    #region 생성자
    /// <summary>
    /// DieState 생성자
    /// </summary>
    /// <param name="enemy">Enemy 컴포넌트 참조</param>
    public DieState(Enemy enemy)
    {
        _enemy = enemy;
    }
    #endregion

    #region IEnemyState 구현
    /// <summary>
    /// 사망 상태에 진입할 때 호출됩니다.
    /// </summary>
    public void Enter()
    {
        // NavMeshAgent 비활성화
        if (_enemy.IsAgentValid())
        {
            _enemy.Agent.isStopped = true;
            _enemy.Agent.ResetPath();
            _enemy.Agent.enabled = false;
        }

        _deathCoroutine = _enemy.StartCoroutine(DeathSequence());
    }

    /// <summary>
    /// 사망 상태에서 나갈 때 호출됩니다.
    /// 이 상태는 종료되지 않으므로 사용되지 않습니다.
    /// </summary>
    public void Exit()
    {
        // 사망 상태는 종료되지 않으므로 별도의 로직이 없습니다.
    }

    /// <summary>
    /// 사망 상태의 매 프레임 호출되는 업데이트 함수입니다.
    /// 사망 상태에서는 아무 작업도 하지 않습니다.
    /// </summary>
    public void Update()
    {
        // 사망 상태에서는 아무 작업도 하지 않습니다. 모든 처리는 코루틴으로 수행됩니다.
    }

    /// <summary>
    /// 다른 상태로의 전환 조건을 확인합니다.
    /// 사망 상태에서는 다른 상태로 전환하지 않습니다.
    /// </summary>
    public void CheckTransitions()
    {
        // 사망 상태에서는 다른 상태로 전환하지 않습니다.
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 사망 처리를 수행하는 코루틴입니다.
    /// </summary>
    private IEnumerator DeathSequence()
    {
        // 사망 처리 시작
        _enemy.BeginDeath();

        // 사망 애니메이션 재생 시간 대기
        yield return new WaitForSeconds(_enemy.DeathDuration);

        // 사망 처리 완료
        _enemy.CompleteDeath();
    }
    #endregion
}